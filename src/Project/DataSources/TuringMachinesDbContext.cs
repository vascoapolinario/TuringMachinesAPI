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
        }

        public virtual DbSet<LevelWorkshopItem> Levels => Set<LevelWorkshopItem>();
        public virtual DbSet<MachineWorkshopItem> Machines => Set<MachineWorkshopItem>();
        public virtual DbSet<Entities.Player> Players => Set<Entities.Player>();
        public virtual DbSet<Entities.WorkshopItem> WorkshopItems => Set<Entities.WorkshopItem>();
        public virtual DbSet<Entities.Review> Reviews => Set<Entities.Review>();

        public virtual DbSet<Entities.Lobby> Lobbies => Set<Entities.Lobby>();

    }
}