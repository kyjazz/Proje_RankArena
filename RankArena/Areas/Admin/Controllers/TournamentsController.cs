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
}