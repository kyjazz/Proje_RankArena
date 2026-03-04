using RankArena.Models.Entities;

namespace RankArena.Models.ViewModels;

public class TournamentDetailsVm
{
    public Tournament Tournament { get; set; } = null!;
    public int ActiveItemCount { get; set; }
    public int TotalItemCount { get; set; }

    public List<WinnerStatVm> TopWinners { get; set; } = new();

    // Yorumlar
    public List<TournamentComment> Comments { get; set; } = new();
    public int CommentCount { get; set; }

    // Toplam oynanma
    public int TotalPlayCount { get; set; }

    // Puan (Rating) Sistemi
    public double AverageRating { get; set; }
    public int TotalRatingCount { get; set; }
    public int? CurrentUserRating { get; set; } // Giriş yapmış kullanıcının verdiği puan (null = henüz vermemiş)
}

public class WinnerStatVm
{
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int WinCount { get; set; }
    public double WinRate { get; set; }
}