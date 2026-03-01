using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models.Entities;

namespace RankArena.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Kategori adı boş olamaz.";
            return View();
        }

        var exists = await _db.Categories.AnyAsync(c => c.Name == name.Trim());
        if (exists)
        {
            TempData["Error"] = "Bu isimde kategori zaten var.";
            return View();
        }

        _db.Categories.Add(new Category { Name = name.Trim() });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{name.Trim()}' kategorisi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        // Kategoriyi kullanan turnuva var mı?
        var usedCount = await _db.Tournaments.CountAsync(t => t.CategoryId == id);
        if (usedCount > 0)
        {
            TempData["Error"] = $"Bu kategori {usedCount} turnuvada kullanılıyor, silinemez.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{cat.Name}' kategorisi silindi.";
        return RedirectToAction(nameof(Index));
    }
}