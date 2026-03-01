using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Helpers;
using RankArena.Models.Entities;

namespace RankArena.Controllers;

[Authorize]
public class MyTournamentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MyTournamentsController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var list = await _db.Tournaments
            .Where(x => x.CreatedByUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string? description, string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("", "Başlık zorunlu.");
            return View();
        }

        var userId = _userManager.GetUserId(User);

        var slugBase = SlugHelper.ToSlug(title);
        var slug = slugBase;

        // slug çakışmasın diye sonuna -2 -3 ekle
        var i = 2;
        while (await _db.Tournaments.AnyAsync(x => x.Slug == slug))
        {
            slug = $"{slugBase}-{i}";
            i++;
        }

        var t = new Tournament
        {
            Title = title.Trim(),
            Description = description,
            CoverImageUrl = coverImageUrl,
            Slug = slug,
            CreatedByUserId = userId,
            IsPublished = false
        };

        _db.Tournaments.Add(t);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /MyTournaments/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);

        var t = await _db.Tournaments
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId);

        if (t == null) return NotFound();

        return View(t);
    }

    // POST: /MyTournaments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string? description, string? coverImageUrl)
    {
        var userId = _userManager.GetUserId(User);

        var t = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId);

        if (t == null) return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Başlık zorunlu.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        t.Title = title.Trim();
        t.Description = description;
        t.CoverImageUrl = coverImageUrl;

        // ✅ herhangi bir değişiklik sonrası yeniden onaya düşsün
        t.IsPublished = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Turnuva güncellendi. Yeniden admin onayına gönderildi.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST: /MyTournaments/AddItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int tournamentId, string name, string? imageUrl, string? youTubeVideoId)
    {
        var userId = _userManager.GetUserId(User);

        var t = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId && x.CreatedByUserId == userId);

        if (t == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Item adı zorunlu.";
            return RedirectToAction(nameof(Edit), new { id = tournamentId });
        }

        var item = new TournamentItem
        {
            TournamentId = t.Id,
            Name = name.Trim(),
            ImageUrl = imageUrl,
            YouTubeVideoId = youTubeVideoId,
            IsActive = true
        };

        _db.TournamentItems.Add(item);

        // ✅ item değişimi => unpublish
        t.IsPublished = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Item eklendi. Turnuva yeniden onaya gönderildi.";
        return RedirectToAction(nameof(Edit), new { id = tournamentId });
    }

    // POST: /MyTournaments/UpdateItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(int itemId, string name, string? imageUrl, string? youTubeVideoId)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _db.TournamentItems
            .Include(x => x.Tournament)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.Tournament.CreatedByUserId == userId);

        if (item == null) return NotFound();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Item adı zorunlu.";
            return RedirectToAction(nameof(Edit), new { id = item.TournamentId });
        }

        item.Name = name.Trim();
        item.ImageUrl = imageUrl;
        item.YouTubeVideoId = youTubeVideoId;

        // ✅ item güncellemesi => unpublish
        item.Tournament.IsPublished = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Item güncellendi. Turnuva yeniden onaya gönderildi.";
        return RedirectToAction(nameof(Edit), new { id = item.TournamentId });
    }

    // POST: /MyTournaments/RemoveItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var userId = _userManager.GetUserId(User);

        var item = await _db.TournamentItems
            .Include(x => x.Tournament)
            .FirstOrDefaultAsync(x => x.Id == itemId && x.Tournament.CreatedByUserId == userId);

        if (item == null) return NotFound();

        var tournamentId = item.TournamentId;

        _db.TournamentItems.Remove(item);

        // ✅ item silinmesi => unpublish
        item.Tournament.IsPublished = false;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Item silindi. Turnuva yeniden onaya gönderildi.";
        return RedirectToAction(nameof(Edit), new { id = tournamentId });
    }
}