using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;

namespace RankArena.Controllers;

[Authorize]
public class MyMessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MyMessagesController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // =============================================
    // GET: /MyMessages
    // Kullanıcının gelen kutusu
    // =============================================
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var messages = await _db.AdminMessages
            .AsNoTracking()
            .Include(x => x.Tournament)
            .Where(x => x.ReceiverUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(messages);
    }

    // =============================================
    // GET: /MyMessages/Detail/5
    // Mesaj detayı + okundu işaretle
    // =============================================
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _userManager.GetUserId(User);

        var message = await _db.AdminMessages
            .Include(x => x.Tournament)
            .FirstOrDefaultAsync(x => x.Id == id && x.ReceiverUserId == userId);

        if (message == null) return NotFound();

        // Okundu olarak işaretle
        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return View(message);
    }

    // =============================================
    // POST: /MyMessages/MarkAllRead
    // Tüm mesajları okundu olarak işaretle
    // =============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User);

        var unreadMessages = await _db.AdminMessages
            .Where(x => x.ReceiverUserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Tüm mesajlar okundu olarak işaretlendi.";
        return RedirectToAction(nameof(Index));
    }
}