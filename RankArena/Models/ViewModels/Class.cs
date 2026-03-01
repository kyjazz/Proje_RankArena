namespace RankArena.Models.ViewModels;

public class TournamentStatsVm
{
    // Bracket istatistikleri
    public List<ItemStatRow> BracketTopWinners { get; set; } = new();
    public int TotalBracketRuns { get; set; }

    // Blind istatistikleri
    public List<BlindStatRow> BlindTopItems { get; set; } = new();
    public int TotalBlindRuns { get; set; }

    // Tier istatistikleri
    public List<TierStatRow> TierTopItems { get; set; } = new();
    public int TotalTierRuns { get; set; }
    public int TotalTierSkips { get; set; }
}

public class ItemStatRow
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int WinCount { get; set; }
    public double WinRate { get; set; }
}

public class BlindStatRow
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public double AveragePosition { get; set; }
    public int FirstPlaceCount { get; set; }
}

public class TierStatRow
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int STierCount { get; set; }
    public int ATierCount { get; set; }
    public int BTierCount { get; set; }
    public int CTierCount { get; set; }
    public int DTierCount { get; set; }
    public int TotalPlacements { get; set; }
}