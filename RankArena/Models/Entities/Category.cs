namespace RankArena.Models.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<Tournament> Tournaments { get; set; } = new();
}
