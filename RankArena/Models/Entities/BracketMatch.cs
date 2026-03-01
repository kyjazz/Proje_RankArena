using System.ComponentModel.DataAnnotations.Schema;

namespace RankArena.Models.Entities;

public class BracketMatch
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run Run { get; set; } = null!;

    // 1..N (1 = ilk tur, N = final)
    public int Round { get; set; }

    // 1..M (tur içindeki maç sırası)
    public int MatchNumber { get; set; }

    // İki taraf (ilerleyen turlarda başta null olabilir)
    public int? LeftItemId { get; set; }
    public TournamentItem? LeftItem { get; set; }

    public int? RightItemId { get; set; }
    public TournamentItem? RightItem { get; set; }

    // Vote sonrası kazanan set edilir
    public int? WinnerItemId { get; set; }
    public TournamentItem? WinnerItem { get; set; }

    public DateTime? CompletedAt { get; set; }
}