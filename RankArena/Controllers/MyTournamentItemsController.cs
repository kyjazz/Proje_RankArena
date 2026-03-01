using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Helpers;              // ✅ eklendi
using RankArena.Models.Entities;

namespace RankArena.Controllers;

[Authorize]
public class MyTournamentItemsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MyTournamentItemsController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // /MyTournamentItems/Index?tournamentId=5
    public async Task<IActionResult> Index(int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId && x.CreatedByUserId == userId);

        if (tournament == null) return NotFound();

        var items = await _db.TournamentItems
            .Where(x => x.TournamentId == tournamentId)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        ViewBag.Tournament = tournament;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int tournamentId, string name, string? imageUrl, string? youTubeVideoId)
    {
        var userId = _userManager.GetUserId(User);

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId && x.CreatedByUserId == userId);

        if (tournament == null) return NotFound();

        // ✅ Yayınlı turnuvayı kilitle
        if (tournament.IsPublished)
        {
            TempData["Error"] = "Bu turnuva yayınlandı. Yayınlı turnuvaya item ekleyemezsin.";
            return RedirectToAction(nameof(Index), new { tournamentId });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Item adı zorunlu.";
            return RedirectToAction(nameof(Index), new { tournamentId });
        }

        // ✅ YouTube: Kullanıcı link yapıştırsa bile sadece VideoId kaydedilecek
        var videoId = YouTubeHelper.ExtractVideoId(youTubeVideoId);

        // kullanıcı bir şey girdiyse ama videoId çıkarılamadıysa hata
        if (!string.IsNullOrWhiteSpace(youTubeVideoId) && videoId == null)
        {
            TempData["Error"] = "YouTube linki/ID geçersiz. Örn: dQw4w9WgXcQ veya https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            return RedirectToAction(nameof(Index), new { tournamentId });
        }

        var item = new TournamentItem
        {
            TournamentId = tournamentId,
            Name = name.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            YouTubeVideoId = videoId, // ✅ artık sadece ID
            IsActive = true
        };

        _db.TournamentItems.Add(item);

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Item eklendi.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Aynı turnuvada aynı isimde item zaten var (unique).";
        }

        return RedirectToAction(nameof(Index), new { tournamentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId && x.CreatedByUserId == userId);

        if (tournament == null) return NotFound();

        if (tournament.IsPublished)
        {
            TempData["Error"] = "Bu turnuva yayınlandı. Item değiştiremezsin.";
            return RedirectToAction(nameof(Index), new { tournamentId });
        }

        var item = await _db.TournamentItems
            .FirstOrDefaultAsync(x => x.Id == id && x.TournamentId == tournamentId);

        if (item == null) return NotFound();

        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { tournamentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int tournamentId)
    {
        var userId = _userManager.GetUserId(User);

        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId && x.CreatedByUserId == userId);

        if (tournament == null) return NotFound();

        if (tournament.IsPublished)
        {
            TempData["Error"] = "Bu turnuva yayınlandı. Item silemezsin.";
            return RedirectToAction(nameof(Index), new { tournamentId });
        }

        var item = await _db.TournamentItems
            .FirstOrDefaultAsync(x => x.Id == id && x.TournamentId == tournamentId);

        if (item == null) return NotFound();

        _db.TournamentItems.Remove(item);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Item silindi.";
        return RedirectToAction(nameof(Index), new { tournamentId });
    }
}
