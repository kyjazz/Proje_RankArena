namespace RankArena.Models.Entities;

public class TournamentItem
{
    public int Id { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? YouTubeVideoId { get; set; }

    public bool IsActive { get; set; } = true;
}
