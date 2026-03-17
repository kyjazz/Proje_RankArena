using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models.Entities;
using RankArena.Models.ViewModels;

namespace RankArena.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TournamentsController : Controller
{
    private readonly AppDbContext _db;

    public TournamentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /Admin/Tournaments/Pending
    public async Task<IActionResult> Pending()
    {
        var list = await _db.Tournaments
            .AsNoTracking()
            .Where(x => !x.IsPublished)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // Kullanıcı adlarını çek
        var userIds = list.Where(x => x.CreatedByUserId != null).Select(x => x.CreatedByUserId!).Distinct().ToList();
        var userNames = new Dictionary<string, string>();
        foreach (var uid in userIds)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            if (user != null) userNames[uid] = user.UserName ?? "Bilinmiyor";
        }
        ViewBag.UserNames = userNames;

        return View(list);
    }

    // GET: /Admin/Tournaments/All  (Tüm turnuvaları listele)
    public async Task<IActionResult> All()
    {
        var list = await _db.Tournaments
            .AsNoTracking()
            .Include(x => x.Category)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // Kullanıcı adlarını çek
        var userIds = list.Where(x => x.CreatedByUserId != null).Select(x => x.CreatedByUserId!).Distinct().ToList();
        var userNames = new Dictionary<string, string>();
        foreach (var uid in userIds)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            if (user != null) userNames[uid] = user.UserName ?? "Bilinmiyor";
        }
        ViewBag.UserNames = userNames;

        return View(list);
    }

    // GET: /Admin/Tournaments/Preview/5
    public async Task<IActionResult> Preview(int id)
    {
        var t = await _db.Tournaments
            .Include(x => x.Items)
            .Include(x => x.Category)
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t == null) return NotFound();

        var totalPlayCount = await _db.Runs
            .CountAsync(r => r.TournamentId == t.Id && r.FinishedAt != null);

        var vm = new TournamentDetailsVm
        {
            Tournament = t,
            ActiveItemCount = t.Items.Count(i => i.IsActive),
            TotalItemCount = t.Items.Count,
            Comments = t.Comments.OrderByDescending(c => c.CreatedAt).ToList(),
            CommentCount = t.Comments.Count,
            TotalPlayCount = totalPlayCount
        };

        return View(vm);
    }

    // POST: /Admin/Tournaments/Publish
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var t = await _db.Tournaments.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        if (t.IsPublished)
        {
            TempData["Info"] = "Bu turnuva zaten yayınlı.";
            return RedirectToAction(nameof(Pending));
        }

        t.IsPublished = true;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Turnuva yayınlandı ✅";
        return RedirectToAction(nameof(Pending));
    }

    // POST: /Admin/Tournaments/Unpublish (opsiyonel)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        var t = await _db.Tournaments.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();

        if (!t.IsPublished)
        {
            TempData["Info"] = "Bu turnuva zaten onay bekliyor (yayınlı değil).";
            return RedirectToAction(nameof(Pending));
        }

        t.IsPublished = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Turnuva yayından kaldırıldı.";
        return RedirectToAction(nameof(Pending));
    }

    // =============================================
    // POST: /Admin/Tournaments/Delete/5
    // Admin HERHANGİ bir turnuvayı silebilir
    // =============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Tournaments
            .Include(x => x.Items)
            .Include(x => x.Comments)
            .Include(x => x.Ratings)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t == null)
        {
            TempData["Error"] = "Turnuva bulunamadı.";
            return RedirectToAction(nameof(All));
        }

        var tournamentTitle = t.Title;

        // İlişkili Run'ları ve alt verilerini sil
        var runs = await _db.Runs.Where(r => r.TournamentId == t.Id).ToListAsync();
        var runIds = runs.Select(r => r.Id).ToList();

        if (runIds.Any())
        {
            // Bracket
            var bracketMatches = await _db.BracketMatches.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            var matchIds = bracketMatches.Select(m => m.Id).ToList();
            if (matchIds.Any())
            {
                var bracketVotes = await _db.BracketVotes.Where(x => matchIds.Contains(x.MatchId)).ToListAsync();
                _db.BracketVotes.RemoveRange(bracketVotes);
            }
            _db.BracketMatches.RemoveRange(bracketMatches);

            // Blind
            var blindRunItems = await _db.BlindRunItems.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.BlindRunItems.RemoveRange(blindRunItems);
            var blindSlots = await _db.BlindSlots.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.BlindSlots.RemoveRange(blindSlots);
            var blindPicks = await _db.BlindPicks.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.BlindPicks.RemoveRange(blindPicks);

            // Tier
            var tierRunItems = await _db.TierRunItems.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.TierRunItems.RemoveRange(tierRunItems);
            var tierSlots = await _db.TierSlots.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.TierSlots.RemoveRange(tierSlots);
            var tierPicks = await _db.TierPicks.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.TierPicks.RemoveRange(tierPicks);
            var tierSkips = await _db.TierSkips.Where(x => runIds.Contains(x.RunId)).ToListAsync();
            _db.TierSkips.RemoveRange(tierSkips);

            _db.Runs.RemoveRange(runs);
        }

        // Tournament kendisi
        _db.Tournaments.Remove(t);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{tournamentTitle}' turnuvası silindi. 🗑️";
        return RedirectToAction(nameof(All));
    }
}