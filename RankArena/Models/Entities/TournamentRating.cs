using System.ComponentModel.DataAnnotations;

namespace RankArena.Models.Entities;

public class TournamentRating
{
    public int Id { get; set; }

    [Required]
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    [Range(1, 10)]
    public int Score { get; set; } // 1-10 arası puan

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}