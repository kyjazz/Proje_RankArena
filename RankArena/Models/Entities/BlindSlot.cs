namespace RankArena.Models.Entities;

public class BlindSlot
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run Run { get; set; } = null!;

    public int Position { get; set; } // 1..N

    public int? TournamentItemId { get; set; } // slot dolunca set olur
    public TournamentItem? TournamentItem { get; set; }

    public DateTime? FilledAt { get; set; }
}