using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.DataSources
{
    public class TuringMachinesDbContext : DbContext
    {
        public TuringMachinesDbContext(DbContextOptions<TuringMachinesDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<LevelWorkshopItem>()
                .Property(p => p.Mode)
                .HasConversion(v => v.ToString(), v => Enum.Parse<LevelMode>(v));

            builder.Entity<WorkshopItem>()
                .Property(p => p.Type)
                .HasConversion(v => v.ToString(), v => Enum.Parse<WorkshopItemType>(v));

            builder.Entity<LeaderboardLevel>()
                .HasOne<WorkshopItem>()
                .WithMany()
                .HasForeignKey(l => l.WorkshopItemId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<LevelSubmission>()
                .HasOne<Player>()
                .WithMany()
                .HasForeignKey(l => l.PlayerId)
                .IsRequired(true)
                .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

            builder.Entity<LevelSubmission>()
                .HasOne<LeaderboardLevel>()
                .WithMany()
                .HasForeignKey(l => l.LeaderboardLevelId)
                .IsRequired(true)
                .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        }

        public virtual DbSet<LevelWorkshopItem> Levels => Set<LevelWorkshopItem>();
        public virtual DbSet<MachineWorkshopItem> Machines => Set<MachineWorkshopItem>();
        public virtual DbSet<Entities.Player> Players => Set<Entities.Player>();
        public virtual DbSet<Entities.WorkshopItem> WorkshopItems => Set<Entities.WorkshopItem>();
        public virtual DbSet<Entities.Review> Reviews => Set<Entities.Review>();
        public virtual DbSet<Entities.Lobby> Lobbies => Set<Entities.Lobby>();
        public virtual DbSet<Entities.LeaderboardLevel> LeaderboardLevels => Set<Entities.LeaderboardLevel>();
        public virtual DbSet<Entities.LevelSubmission> LevelSubmissions => Set<Entities.LevelSubmission>();

    }
}