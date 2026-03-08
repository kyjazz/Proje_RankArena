using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models.Entities;
using RankArena.Models.ViewModels;

namespace RankArena.Controllers;

public class TournamentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public TournamentsController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Tournaments
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = _db.Tournaments
            .Include(t => t.Category)
            .Where(t => t.IsPublished);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        var tournaments = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Search = search;
        ViewBag.SelectedCategory = categoryId;

        return View(tournaments);
    }

    // GET: /t/{slug}
    [Route("t/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var t = await _db.Tournaments
            .Include(x => x.Items)
            .Include(x => x.Category)
            .Include(x => x.Comments)
            .Include(x => x.Ratings)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);

        if (t == null) return NotFound();

        // Bracket top kazananlar
        var bracketWinners = await _db.Runs
            .Where(r => r.TournamentId == t.Id && r.Mode == GameMode.Bracket && r.WinnerItemId != null)
            .GroupBy(r => r.WinnerItemId)
            .Select(g => new { ItemId = g.Key!.Value, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var totalBracketRuns = await _db.Runs
            .CountAsync(r => r.TournamentId == t.Id && r.Mode == GameMode.Bracket && r.FinishedAt != null);

        var bracketTopWinners = new List<ItemStatRow>();
        foreach (var bw in bracketWinners)
        {
            var item = t.Items.FirstOrDefault(i => i.Id == bw.ItemId);
            if (item != null)
            {
                bracketTopWinners.Add(new ItemStatRow
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    ImageUrl = item.ImageUrl,
                    WinCount = bw.Count,
                    WinRate = totalBracketRuns == 0 ? 0 : Math.Round(bw.Count * 100.0 / totalBracketRuns, 1)
                });
            }
        }

        // Blind: ortalama sıra + 1. seçilme
        var blindStats = await _db.BlindSlots
            .Where(s => s.Run!.TournamentId == t.Id && s.TournamentItemId != null)
            .GroupBy(s => s.TournamentItemId)
            .Select(g => new
            {
                ItemId = g.Key!.Value,
                AvgPos = g.Average(s => s.Position),
                FirstCount = g.Count(s => s.Position == 1)
            })
            .OrderBy(x => x.AvgPos)
            .Take(5)
            .ToListAsync();

        var totalBlindRuns = await _db.Runs
            .CountAsync(r => r.TournamentId == t.Id && r.Mode == GameMode.BlindRank && r.FinishedAt != null);

        var blindTopItems = new List<BlindStatRow>();
        foreach (var bs in blindStats)
        {
            var item = t.Items.FirstOrDefault(i => i.Id == bs.ItemId);
            if (item != null)
            {
                blindTopItems.Add(new BlindStatRow
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    ImageUrl = item.ImageUrl,
                    AveragePosition = Math.Round(bs.AvgPos, 2),
                    FirstPlaceCount = bs.FirstCount
                });
            }
        }

        // Tier: S/A/B/C/D dağılımı
        var tierStats = await _db.TierSlots
            .Where(s => s.Run!.TournamentId == t.Id)
            .GroupBy(s => s.TournamentItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                SCount = g.Count(s => s.Tier == Tier.S),
                ACount = g.Count(s => s.Tier == Tier.A),
                BCount = g.Count(s => s.Tier == Tier.B),
                CCount = g.Count(s => s.Tier == Tier.C),
                DCount = g.Count(s => s.Tier == Tier.D),
                Total = g.Count()
            })
            .OrderByDescending(x => x.SCount)
            .ThenByDescending(x => x.ACount)
            .Take(5)
            .ToListAsync();

        var totalTierRuns = await _db.Runs
            .CountAsync(r => r.TournamentId == t.Id && r.Mode == GameMode.TierList && r.FinishedAt != null);

        var totalTierSkips = await _db.TierSkips
            .CountAsync(s => s.Run!.TournamentId == t.Id);

        var tierTopItems = new List<TierStatRow>();
        foreach (var ts in tierStats)
        {
            var item = t.Items.FirstOrDefault(i => i.Id == ts.ItemId);
            if (item != null)
            {
                tierTopItems.Add(new TierStatRow
                {
                    ItemId = item.Id,
                    ItemName = item.Name,
                    ImageUrl = item.ImageUrl,
                    STierCount = ts.SCount,
                    ATierCount = ts.ACount,
                    BTierCount = ts.BCount,
                    CTierCount = ts.CCount,
                    DTierCount = ts.DCount,
                    TotalPlacements = ts.Total
                });
            }
        }

        var stats = new TournamentStatsVm
        {
            BracketTopWinners = bracketTopWinners,
            TotalBracketRuns = totalBracketRuns,
            BlindTopItems = blindTopItems,
            TotalBlindRuns = totalBlindRuns,
            TierTopItems = tierTopItems,
            TotalTierRuns = totalTierRuns,
            TotalTierSkips = totalTierSkips
        };

        // Toplam oynanma sayısı
        var totalPlayCount = await _db.Runs
            .CountAsync(r => r.TournamentId == t.Id && r.FinishedAt != null);

        // Yorumları tarihe göre sırala
        var comments = t.Comments.OrderByDescending(c => c.CreatedAt).ToList();

        // ===== PUAN (RATING) HESAPLAMA =====
        var ratings = t.Ratings;
        var totalRatingCount = ratings.Count;
        var averageRating = totalRatingCount > 0
            ? Math.Round(ratings.Average(r => r.Score), 1)
            : 0;

        // Giriş yapmış kullanıcının mevcut puanı
        int? currentUserRating = null;
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null)
        {
            var existingRating = ratings.FirstOrDefault(r => r.UserId == currentUser.Id);
            if (existingRating != null)
                currentUserRating = existingRating.Score;
        }

        var vm = new TournamentDetailsVm
        {
            Tournament = t,
            TotalItemCount = t.Items.Count,
            ActiveItemCount = t.Items.Count(i => i.IsActive),
            Comments = comments,
            CommentCount = comments.Count,
            TotalPlayCount = totalPlayCount,
            AverageRating = averageRating,
            TotalRatingCount = totalRatingCount,
            CurrentUserRating = currentUserRating
        };

        ViewBag.Stats = stats;

        // ===== OLUŞTURAN KULLANICI ADINI BUL =====
        string creatorName = "Anonim";
        if (!string.IsNullOrEmpty(t.CreatedByUserId))
        {
            var creatorUser = await _userManager.FindByIdAsync(t.CreatedByUserId);
            if (creatorUser != null)
                creatorName = creatorUser.UserName ?? "Anonim";
        }
        ViewBag.CreatorName = creatorName;

        // ===== GİRİŞ YAPAN KULLANICININ ID'Sİ (Yorum sil/düzenle için) =====
        ViewBag.CurrentUserId = currentUser?.Id;
        ViewBag.IsAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

        return View(vm);
    }

    // POST: /Tournaments/AddComment
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int tournamentId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
        {
            TempData["Error"] = "Yorum 1-1000 karakter arasında olmalıdır.";
            var tournament = await _db.Tournaments.FindAsync(tournamentId);
            if (tournament != null)
                return Redirect($"/t/{tournament.Slug}");
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var t = await _db.Tournaments.FindAsync(tournamentId);
        if (t == null || !t.IsPublished) return NotFound();

        var comment = new TournamentComment
        {
            TournamentId = tournamentId,
            UserId = user.Id,
            UserName = user.UserName ?? "Anonim",
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.TournamentComments.Add(comment);
        await _db.SaveChangesAsync();

        return Redirect($"/t/{t.Slug}#comments");
    }

    // POST: /Tournaments/DeleteComment
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var comment = await _db.TournamentComments
            .Include(c => c.Tournament)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        // Sadece kendi yorumunu silebilir VEYA admin herkesin yorumunu silebilir
        if (comment.UserId != user.Id && !isAdmin)
        {
            TempData["Error"] = "Bu yorumu silme yetkiniz yok.";
            return Redirect($"/t/{comment.Tournament.Slug}#comments");
        }

        var slug = comment.Tournament.Slug;

        _db.TournamentComments.Remove(comment);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Yorum silindi.";
        return Redirect($"/t/{slug}#comments");
    }

    // POST: /Tournaments/EditComment
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditComment(int commentId, string content)
    {
        var comment = await _db.TournamentComments
            .Include(c => c.Tournament)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Sadece kendi yorumunu düzenleyebilir
        if (comment.UserId != user.Id)
        {
            TempData["Error"] = "Bu yorumu düzenleme yetkiniz yok.";
            return Redirect($"/t/{comment.Tournament.Slug}#comments");
        }

        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
        {
            TempData["Error"] = "Yorum 1-1000 karakter arasında olmalıdır.";
            return Redirect($"/t/{comment.Tournament.Slug}#comments");
        }

        comment.Content = content.Trim();
        _db.TournamentComments.Update(comment);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Yorum güncellendi.";
        return Redirect($"/t/{comment.Tournament.Slug}#comments");
    }

    // POST: /Tournaments/RateTournament
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RateTournament(int tournamentId, int score)
    {
        // Puan 1-10 arası olmalı
        if (score < 1 || score > 10)
        {
            TempData["Error"] = "Puan 1 ile 10 arasında olmalıdır.";
            var tournament = await _db.Tournaments.FindAsync(tournamentId);
            if (tournament != null)
                return Redirect($"/t/{tournament.Slug}");
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var t = await _db.Tournaments.FindAsync(tournamentId);
        if (t == null || !t.IsPublished) return NotFound();

        // Kullanıcının daha önce puan verip vermediğini kontrol et
        var existingRating = await _db.TournamentRatings
            .FirstOrDefaultAsync(r => r.TournamentId == tournamentId && r.UserId == user.Id);

        if (existingRating != null)
        {
            // Güncelle
            existingRating.Score = score;
            existingRating.UpdatedAt = DateTime.UtcNow;
            _db.TournamentRatings.Update(existingRating);
        }
        else
        {
            // Yeni puan ekle
            var rating = new TournamentRating
            {
                TournamentId = tournamentId,
                UserId = user.Id,
                Score = score,
                CreatedAt = DateTime.UtcNow
            };
            _db.TournamentRatings.Add(rating);
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Puanınız kaydedildi!";
        return Redirect($"/t/{t.Slug}");
    }
}