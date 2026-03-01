using System.ComponentModel.DataAnnotations;

namespace RankArena.Models.Entities;

public class TierRunItem
{
    public int Id { get; set; }

    [Required]
    public int RunId { get; set; }
    public Run? Run { get; set; }

    [Required]
    public int TournamentItemId { get; set; }
    public TournamentItem? TournamentItem { get; set; }

    public int Sequence { get; set; } // pool sırası
}