namespace RankArena.Models.Entities;

public class Run
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public GameMode Mode { get; set; }

    public string? UserId { get; set; }
    public string SessionKey { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    public int? TotalSlots { get; set; } // Bracket 8/16/32/64
    public int? WinnerItemId { get; set; }
}
