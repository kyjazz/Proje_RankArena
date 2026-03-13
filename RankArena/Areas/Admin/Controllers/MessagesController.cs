using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RankArena.Data;
using RankArena.Models.Entities;

namespace RankArena.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MessagesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MessagesController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // =============================================
    // GET: /Admin/Messages
    // Admin'in gönderdiği tüm mesajları listele
    // =============================================
    public async Task<IActionResult> Index()
    {
        var messages = await _db.AdminMessages
            .AsNoTracking()
            .Include(x => x.Tournament)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        // Kullanıcı adlarını çekmek için
        var userIds = messages.Select(m => m.ReceiverUserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "Bilinmiyor");

        ViewBag.UserNames = users;

        return View(messages);
    }

    // =============================================
    // GET: /Admin/Messages/Send?tournamentId=5
    // Turnuva bazlı mesaj gönderme formu
    // =============================================
    [HttpGet]
    public async Task<IActionResult> Send(int? tournamentId)
    {
        if (tournamentId.HasValue)
        {
            var tournament = await _db.Tournaments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == tournamentId.Value);

            if (tournament == null) return NotFound();

            ViewBag.Tournament = tournament;

            // Turnuva sahibinin bilgilerini getir
            if (!string.IsNullOrEmpty(tournament.CreatedByUserId))
            {
                var user = await _userManager.FindByIdAsync(tournament.CreatedByUserId);
                ViewBag.ReceiverUserName = user?.UserName ?? user?.Email ?? "Bilinmiyor";
                ViewBag.ReceiverUserId = tournament.CreatedByUserId;
            }
        }

        return View();
    }

    // =============================================
    // POST: /Admin/Messages/Send
    // Mesajı kaydet
    // =============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(
        string receiverUserId,
        int? tournamentId,
        AdminMessageType messageType,
        string subject,
        string content)
    {
        if (string.IsNullOrWhiteSpace(receiverUserId))
        {
            TempData["Error"] = "Alıcı kullanıcı belirtilmedi.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            TempData["Error"] = "Mesaj başlığı zorunlu.";
            return RedirectToAction(nameof(Send), new { tournamentId });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Mesaj içeriği zorunlu.";
            return RedirectToAction(nameof(Send), new { tournamentId });
        }

        var message = new AdminMessage
        {
            ReceiverUserId = receiverUserId,
            TournamentId = tournamentId,
            MessageType = messageType,
            Subject = subject.Trim(),
            Content = content.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.AdminMessages.Add(message);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Mesaj başarıyla gönderildi ✅";

        // Turnuva sayfasından geldiyse oraya geri dön
        if (tournamentId.HasValue)
            return RedirectToAction("All", "Tournaments", new { area = "Admin" });

        return RedirectToAction(nameof(Index));
    }

    // =============================================
    // POST: /Admin/Messages/SendQuick
    // Hızlı mesaj gönder (Turnuva listesindeki modaldan)
    // =============================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendQuick(
        int tournamentId,
        AdminMessageType messageType,
        string content,
        string? returnUrl)
    {
        var tournament = await _db.Tournaments
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (tournament == null)
        {
            TempData["Error"] = "Turnuva bulunamadı.";
            return RedirectToAction("All", "Tournaments", new { area = "Admin" });
        }

        if (string.IsNullOrWhiteSpace(tournament.CreatedByUserId))
        {
            TempData["Error"] = "Turnuva sahibi bulunamadı (anonim turnuva).";
            return RedirectToAction("All", "Tournaments", new { area = "Admin" });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Mesaj içeriği boş olamaz.";
            return Redirect(returnUrl ?? "/Admin/Tournaments/All");
        }

        // Mesaj türüne göre otomatik başlık oluştur
        var subject = messageType switch
        {
            AdminMessageType.TurnuvaReddedildi => $"Turnuvanız reddedildi: {tournament.Title}",
            AdminMessageType.TurnuvaYayindanKaldirildi => $"Turnuvanız yayından kaldırıldı: {tournament.Title}",
            AdminMessageType.TurnuvaSilindi => $"Turnuvanız silindi: {tournament.Title}",
            AdminMessageType.GenelBilgilendirme => $"Bilgilendirme: {tournament.Title}",
            _ => $"Admin Mesajı: {tournament.Title}"
        };

        var message = new AdminMessage
        {
            ReceiverUserId = tournament.CreatedByUserId,
            TournamentId = tournamentId,
            MessageType = messageType,
            Subject = subject,
            Content = content.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.AdminMessages.Add(message);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"'{tournament.Title}' turnuvasının sahibine mesaj gönderildi ✅";
        return Redirect(returnUrl ?? "/Admin/Tournaments/All");
    }
}