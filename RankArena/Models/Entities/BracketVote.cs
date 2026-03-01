namespace RankArena.Models.Entities;

public class BracketVote
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run Run { get; set; } = null!;

    public int MatchId { get; set; }
    public BracketMatch Match { get; set; } = null!;

    public int SelectedItemId { get; set; }
    public TournamentItem SelectedItem { get; set; } = null!;

    public string? UserId { get; set; }      // login varsa dolu
    public string SessionKey { get; set; } = null!; // guest için ana kimlik

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}