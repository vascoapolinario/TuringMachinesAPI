using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
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

            builder.Entity<AdminLog>()
                .Property(p => p.TargetEntityType)
                .HasConversion(v => v.ToString(), v => Enum.Parse<TargetEntityType>(v));

            builder.Entity<AdminLog>()
                .Property(p => p.Action)
                .HasConversion(v => v.ToString(), v => Enum.Parse<ActionType>(v));

            builder.Entity<Discussion>()
                .Property(p => p.Category)
                .HasConversion(v => v.ToString(), v => Enum.Parse<DiscussionCategory>(v));

            builder.Entity<AdminLog>()
                .HasOne<Player>()
                .WithMany()
                .HasForeignKey(l => l.ActorId)
                .IsRequired(false)
                .OnDelete(deleteBehavior: DeleteBehavior.SetNull);

            builder.Entity<Discussion>()
                .HasOne(d => d.InitialPost)
                .WithOne()
                .HasForeignKey<Discussion>(d => d.InitialPostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Discussion>()
                .HasOne(d => d.AnswerPost)
                .WithOne()
                .HasForeignKey<Discussion>(d => d.AnswerPostId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany()
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Post>()
                .HasOne(p => p.Discussion)
                .WithMany(d => d.Posts)
                .HasForeignKey(p => p.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Discussion>()
                .HasOne(d => d.Author)
                .WithMany()
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PostVote>()
                .HasOne(p => p.Player)
                .WithMany()
                .HasForeignKey(pv => pv.PlayerId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostVote>()
                .HasOne(p => p.Post)
                .WithMany()
                .HasForeignKey(pv => pv.PostId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Report>()
                .Property(r => r.ReportedItemType)
                .HasConversion(v => v.ToString(), v => Enum.Parse<ReportType>(v));

            builder.Entity<Report>()
                .Property(r => r.Status)
                .HasConversion(v => v.ToString(), v => Enum.Parse<ReportStatus>(v));



        }

        public virtual DbSet<LevelWorkshopItem> Levels => Set<LevelWorkshopItem>();
        public virtual DbSet<MachineWorkshopItem> Machines => Set<MachineWorkshopItem>();
        public virtual DbSet<Entities.Player> Players => Set<Entities.Player>();
        public virtual DbSet<Entities.WorkshopItem> WorkshopItems => Set<Entities.WorkshopItem>();
        public virtual DbSet<Entities.Review> Reviews => Set<Entities.Review>();
        public virtual DbSet<Entities.Lobby> Lobbies => Set<Entities.Lobby>();
        public virtual DbSet<Entities.LeaderboardLevel> LeaderboardLevels => Set<Entities.LeaderboardLevel>();
        public virtual DbSet<Entities.LevelSubmission> LevelSubmissions => Set<Entities.LevelSubmission>();
        public virtual DbSet<Entities.AdminLog> AdminLogs => Set<Entities.AdminLog>();
        public virtual DbSet<Entities.Discussion> Discussions => Set<Entities.Discussion>();
        public virtual DbSet<Entities.Post> Posts => Set<Entities.Post>();

        public virtual DbSet<Entities.Report> Reports => Set<Entities.Report>();

        public virtual DbSet<Entities.PostVote> PostVotes => Set<Entities.PostVote>();

    }
}