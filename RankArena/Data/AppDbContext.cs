using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RankArena.Models.Entities;

namespace RankArena.Data;

public class AppDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentItem> TournamentItems => Set<TournamentItem>();
    public DbSet<Run> Runs => Set<Run>();

    // BRACKET
    public DbSet<BracketMatch> BracketMatches => Set<BracketMatch>();
    public DbSet<BracketVote> BracketVotes => Set<BracketVote>();

    // BLIND RANK
    public DbSet<BlindRunItem> BlindRunItems => Set<BlindRunItem>();
    public DbSet<BlindSlot> BlindSlots => Set<BlindSlot>();
    public DbSet<BlindPick> BlindPicks => Set<BlindPick>();

    // TIER LIST
    public DbSet<TierRunItem> TierRunItems => Set<TierRunItem>();
    public DbSet<TierSlot> TierSlots => Set<TierSlot>();
    public DbSet<TierPick> TierPicks => Set<TierPick>();
    public DbSet<TierSkip> TierSkips => Set<TierSkip>();

    // YORUM
    public DbSet<TournamentComment> TournamentComments => Set<TournamentComment>();

    // PUAN (RATING)
    public DbSet<TournamentRating> TournamentRatings => Set<TournamentRating>();

    // ✅ ADMİN MESAJLARI
    public DbSet<AdminMessage> AdminMessages => Set<AdminMessage>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Tournament -> Slug unique
        b.Entity<Tournament>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        // Tournament -> IsPublished default false
        b.Entity<Tournament>()
            .Property(x => x.IsPublished)
            .HasDefaultValue(false);

        // Tournament (1) - (N) TournamentItem
        b.Entity<TournamentItem>()
            .HasOne(x => x.Tournament)
            .WithMany(t => t.Items)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Aynı turnuvada aynı isim olmasın
        b.Entity<TournamentItem>()
            .HasIndex(x => new { x.TournamentId, x.Name })
            .IsUnique();

        // Run -> SessionKey zorunlu
        b.Entity<Run>()
            .Property(x => x.SessionKey)
            .IsRequired();

        // -------------------------------------------------
        // BRACKET MATCHES
        // -------------------------------------------------
        b.Entity<BracketMatch>()
            .HasIndex(x => new { x.RunId, x.Round, x.MatchNumber })
            .IsUnique();

        b.Entity<BracketMatch>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BracketMatch>()
            .HasOne(x => x.LeftItem)
            .WithMany()
            .HasForeignKey(x => x.LeftItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BracketMatch>()
            .HasOne(x => x.RightItem)
            .WithMany()
            .HasForeignKey(x => x.RightItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BracketMatch>()
            .HasOne(x => x.WinnerItem)
            .WithMany()
            .HasForeignKey(x => x.WinnerItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // -------------------------------------------------
        // BRACKET VOTES
        // -------------------------------------------------
        b.Entity<BracketVote>()
            .Property(x => x.SessionKey)
            .IsRequired();

        b.Entity<BracketVote>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BracketVote>()
            .HasOne(x => x.Match)
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BracketVote>()
            .HasOne(x => x.SelectedItem)
            .WithMany()
            .HasForeignKey(x => x.SelectedItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // -------------------------------------------------
        // BLIND RANK (POOL + SLOTS + PICKS)
        // -------------------------------------------------
        b.Entity<BlindRunItem>()
            .HasIndex(x => new { x.RunId, x.Sequence })
            .IsUnique();

        b.Entity<BlindRunItem>()
            .HasIndex(x => new { x.RunId, x.TournamentItemId })
            .IsUnique();

        b.Entity<BlindRunItem>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BlindRunItem>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BlindSlot>()
            .HasIndex(x => new { x.RunId, x.Position })
            .IsUnique();

        b.Entity<BlindSlot>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<BlindSlot>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BlindPick>()
            .Property(x => x.SessionKey)
            .IsRequired();

        b.Entity<BlindPick>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<BlindPick>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // -------------------------------------------------
        // TIER LIST (POOL + SLOTS + PICKS)
        // -------------------------------------------------
        b.Entity<TierRunItem>()
            .HasIndex(x => new { x.RunId, x.Sequence })
            .IsUnique();

        b.Entity<TierRunItem>()
            .HasIndex(x => new { x.RunId, x.TournamentItemId })
            .IsUnique();

        b.Entity<TierRunItem>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TierRunItem>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<TierSlot>()
            .HasIndex(x => new { x.RunId, x.TournamentItemId })
            .IsUnique();

        b.Entity<TierSlot>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TierSlot>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<TierPick>()
            .Property(x => x.SessionKey)
            .IsRequired();

        b.Entity<TierPick>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<TierPick>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<TierSkip>()
            .HasIndex(x => new { x.RunId, x.TournamentItemId })
            .IsUnique();

        b.Entity<TierSkip>()
            .Property(x => x.SessionKey)
            .IsRequired();

        b.Entity<TierSkip>()
            .HasOne(x => x.Run)
            .WithMany()
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.NoAction);

        b.Entity<TierSkip>()
            .HasOne(x => x.TournamentItem)
            .WithMany()
            .HasForeignKey(x => x.TournamentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        // -------------------------------------------------
        // TOURNAMENT COMMENTS
        // -------------------------------------------------
        b.Entity<TournamentComment>()
            .HasOne(x => x.Tournament)
            .WithMany(t => t.Comments)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TournamentComment>()
            .Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(1000);

        b.Entity<TournamentComment>()
            .Property(x => x.UserId)
            .IsRequired();

        b.Entity<TournamentComment>()
            .Property(x => x.UserName)
            .IsRequired();

        // -------------------------------------------------
        // TOURNAMENT RATINGS (PUAN)
        // -------------------------------------------------
        b.Entity<TournamentRating>()
            .HasOne(x => x.Tournament)
            .WithMany(t => t.Ratings)
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<TournamentRating>()
            .Property(x => x.UserId)
            .IsRequired();

        b.Entity<TournamentRating>()
            .Property(x => x.Score)
            .IsRequired();

        // Aynı kullanıcı aynı turnuvaya sadece 1 kez puan verebilir
        b.Entity<TournamentRating>()
            .HasIndex(x => new { x.TournamentId, x.UserId })
            .IsUnique();

        // -------------------------------------------------
        // ✅ ADMİN MESAJLARI
        // -------------------------------------------------
        b.Entity<AdminMessage>()
            .Property(x => x.ReceiverUserId)
            .IsRequired();

        b.Entity<AdminMessage>()
            .Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);

        b.Entity<AdminMessage>()
            .Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(2000);

        b.Entity<AdminMessage>()
            .HasOne(x => x.Tournament)
            .WithMany()
            .HasForeignKey(x => x.TournamentId)
            .OnDelete(DeleteBehavior.SetNull);

        b.Entity<AdminMessage>()
            .Property(x => x.IsRead)
            .HasDefaultValue(false);
    }
}