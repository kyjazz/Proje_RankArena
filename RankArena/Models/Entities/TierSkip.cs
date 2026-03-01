namespace RankArena.Models.Entities;

public class TierSkip
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run? Run { get; set; }

    public int TournamentItemId { get; set; }
    public TournamentItem? TournamentItem { get; set; }

    public string? UserId { get; set; }
    public string SessionKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}