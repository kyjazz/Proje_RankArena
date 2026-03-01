using System.ComponentModel.DataAnnotations.Schema;

namespace RankArena.Models.Entities;

public class BlindRunItem
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run Run { get; set; } = null!;

    public int TournamentItemId { get; set; }
    public TournamentItem TournamentItem { get; set; } = null!;

    public int Sequence { get; set; } // item'lar hangi sırayla gösterilecek (1..N)
}