using System.ComponentModel.DataAnnotations;

namespace RankArena.Models.Entities;

public class TournamentComment
{
    public int Id { get; set; }

    [Required]
    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}