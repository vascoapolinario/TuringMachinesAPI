using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.Entities;

namespace TuringMachinesAPI.DataSources
{
    public class TuringMachinesDbContext : DbContext
    {
        public TuringMachinesDbContext(DbContextOptions<TuringMachinesDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LevelWorkshopItem> Levels => Set<LevelWorkshopItem>();
        public virtual DbSet<MachineWorkshopItem> Machines => Set<MachineWorkshopItem>();
        public virtual DbSet<Entities.Player> Players => Set<Entities.Player>();
        public virtual DbSet<Entities.WorkshopItem> WorkshopItems => Set<Entities.WorkshopItem>();
        public virtual DbSet<Entities.Review> Reviews => Set<Entities.Review>();

    }
}