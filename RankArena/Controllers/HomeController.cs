using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models;

namespace RankArena.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;

    public HomeController(ILogger<HomeController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var tournaments = await _db.Tournaments
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .ToListAsync();

        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Tournaments = tournaments;
        ViewBag.Categories = categories;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}