using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;

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

        return View(list);
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
}