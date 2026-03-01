using System.ComponentModel.DataAnnotations;

namespace RankArena.Models.Entities;

public class TierSlot
{
    public int Id { get; set; }

    [Required]
    public int RunId { get; set; }
    public Run? Run { get; set; }

    [Required]
    public int TournamentItemId { get; set; }
    public TournamentItem? TournamentItem { get; set; }

    [Required]
    public Tier Tier { get; set; } // S/A/B/C/D

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}