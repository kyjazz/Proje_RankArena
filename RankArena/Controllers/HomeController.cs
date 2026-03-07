using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models;

namespace RankArena.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public HomeController(ILogger<HomeController> logger, AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Popüler turnuvalar (en çok oynanan / en çok yorum alan)
        var popularTournaments = await _db.Tournaments
            .Include(t => t.Category)
            .Include(t => t.Ratings)
            .Include(t => t.Items)
            .Include(t => t.Comments)
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.Ratings.Count)
            .ThenByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Son eklenen turnuvalar
        var recentTournaments = await _db.Tournaments
            .Include(t => t.Category)
            .Include(t => t.Ratings)
            .Include(t => t.Items)
            .Include(t => t.Comments)
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        // Tüm turnuvalarýn oluþturan kullanýcý adlarýný bul
        var allTournaments = popularTournaments.Union(recentTournaments).ToList();
        var creatorUserIds = allTournaments
            .Where(t => !string.IsNullOrEmpty(t.CreatedByUserId))
            .Select(t => t.CreatedByUserId!)
            .Distinct()
            .ToList();

        var creatorNames = new Dictionary<string, string>();
        foreach (var userId in creatorUserIds)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
                creatorNames[userId] = user.UserName ?? "Anonim";
            else
                creatorNames[userId] = "Anonim";
        }

        ViewBag.PopularTournaments = popularTournaments;
        ViewBag.RecentTournaments = recentTournaments;
        ViewBag.Categories = categories;
        ViewBag.CreatorNames = creatorNames;

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