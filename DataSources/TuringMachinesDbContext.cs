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

        public virtual DbSet<Level> Levels => Set<Level>();
        public virtual DbSet<Machine> Machines => Set<Machine>();
        // public virtual DbSet<UserEntity> Users => Set<UserEntity>();
    }
}