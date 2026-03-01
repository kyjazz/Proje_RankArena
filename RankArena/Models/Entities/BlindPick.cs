namespace RankArena.Models.Entities;

public class BlindPick
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run Run { get; set; } = null!;

    public int TournamentItemId { get; set; }
    public TournamentItem TournamentItem { get; set; } = null!;

    public int Position { get; set; } // seçilen slot

    public string? UserId { get; set; }
    public string SessionKey { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}