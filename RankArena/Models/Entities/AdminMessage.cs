using System.ComponentModel.DataAnnotations;

namespace RankArena.Models.Entities;

public class AdminMessage
{
    public int Id { get; set; }

    /// <summary>
    /// Mesajın gönderildiği kullanıcı (turnuva sahibi)
    /// </summary>
    [Required]
    public string ReceiverUserId { get; set; } = null!;

    /// <summary>
    /// İlgili turnuva (opsiyonel – genel mesaj da gönderilebilir)
    /// </summary>
    public int? TournamentId { get; set; }
    public Tournament? Tournament { get; set; }

    /// <summary>
    /// Mesaj türü
    /// </summary>
    [Required]
    public AdminMessageType MessageType { get; set; }

    /// <summary>
    /// Mesaj başlığı
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = null!;

    /// <summary>
    /// Mesaj içeriği (Admin'in yazdığı açıklama)
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = null!;

    /// <summary>
    /// Okundu mu?
    /// </summary>
    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public enum AdminMessageType
{
    TurnuvaReddedildi = 1,
    TurnuvaYayindanKaldirildi = 2,
    TurnuvaSilindi = 3,
    GenelBilgilendirme = 4
}