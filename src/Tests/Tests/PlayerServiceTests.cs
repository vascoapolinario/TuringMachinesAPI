using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;
using Xunit;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class PlayerServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly PlayerService service;
        private readonly TuringMachinesDbContext db;

        public PlayerServiceTests()
        {
            applicationDomain = new TestApplicationDomain();
            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            string? _connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            if (_connectionString == null) throw new FileNotFoundException("Não foi possível obter a connection string de configuração.");

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(_connectionString));
            applicationDomain.Services.AddScoped<PlayerService>();
            applicationDomain.Services.AddScoped<PasswordHashService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<PlayerService>();

            string? BackupPath = applicationDomain.configuration.GetValue<string>("TestsDbBackup:FilePath");
            if (BackupPath == null) throw new Exception("Não foi possível obter o caminho do ficheiro de configuração.");

            string sql = File.ReadAllText(BackupPath);

            using (IServiceScope serviceScope = applicationDomain.ServiceProvider.CreateScope())
            {
                TuringMachinesDbContext databaseContext = serviceScope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
                databaseContext.Database.Migrate();
                databaseContext.Database.ExecuteSqlRaw(sql);
                databaseContext.SaveChanges();
            }
        }

        public void Dispose()
        {
            applicationDomain.Dispose();
        }


        [Fact]
        public void AddPlayer_ShouldAddSuccessfully()
        {
            var player = new Player { Username = "testUser", Password = "1234" };

            var result = service.AddPlayer(player);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("testUser", result.Username);
            Assert.Equal(DateTime.UtcNow, result.CreatedAt, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void AddPlayer_ShouldReturnNull_WhenDuplicateUsername()
        {
            var p1 = new Player { Username = "sameUser", Password = "abc" };
            var p2 = new Player { Username = "sameUser", Password = "def" };
            service.AddPlayer(p1);

            var result = service.AddPlayer(p2);

            Assert.Null(result);
        }

        [Fact]
        public void GetPlayerById_ShouldReturnPlayer_WhenExists()
        {
            var added = service.AddPlayer(new Player { Username = "foundUser", Password = "pass" });

            var found = service.GetPlayerById(added.Id);

            Assert.NotNull(found);
            Assert.Equal("foundUser", found!.Username);
        }

        [Fact]
        public void GetPlayerById_ShouldReturnNull_WhenNotFound()
        {
            var result = service.GetPlayerById(99999);
            Assert.Null(result);
        }

        [Fact]
        public void Authenticate_ShouldReturnPlayer_WhenCredentialsMatch()
        {
            service.AddPlayer(new Player { Username = "loginUser", Password = "secret" });

            var result = service.Authenticate("loginUser", "secret");

            Assert.NotNull(result);
            Assert.Equal("loginUser", result!.Username);
            Assert.Equal(DateTime.UtcNow, result.LastLogin!.Value, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Authenticate_ShouldReturnNull_WhenWrongPassword()
        {
            service.AddPlayer(new Player { Username = "badUser", Password = "rightpass" });

            var result = service.Authenticate("badUser", "wrongpass");

            Assert.Null(result);
        }

        [Fact]
        public void NonSensitivePlayer_ShouldNotExposePassword()
        {
            var player = new Player { Username = "safeUser", Password = "hidden" };
            var nonSensitive = service.NonSensitivePlayer(player);
            Assert.Equal("safeUser", nonSensitive.Username);
            Assert.Equal("", nonSensitive.Password);
        }

        [Fact]
        public void DeletePlayer_ShouldRemovePlayer_WhenExists()
        {
            var player = service.AddPlayer(new Player { Username = "deleteUser", Password = "tobedeleted" });
            Assert.NotNull(player);

            bool deleted = service.DeletePlayer(player.Id);
            Assert.True(deleted);
            var shouldBeNull = service.GetPlayerById(player.Id);
            Assert.Null(shouldBeNull);
        }

        [Fact]
        public void PlayerExistsAsIs_ShouldReturnTrue_WhenExists()
        {
            var player = service.AddPlayer(new Player { Username = "existUser", Password = "pass", Role = "User" });
            string playerId = player.Id.ToString();
            Assert.NotNull(player);
            bool exists = service.PlayerExistsAsIs(playerId, "existUser", "User");
            Assert.True(exists);
        }

        [Fact]
        public void PlayerExistsAsIs_ShouldReturnFalse_WhenNotExists()
        {
            bool exists = service.PlayerExistsAsIs("99999", "nonUser", "User");
            Assert.False(exists);
        }

        [Fact]
        public void TemporaryBanPlayer_ShouldBanSuccessfully()
        {
            var player = service.AddPlayer(new Player { Username = "banUser", Password = "pass" });
            Assert.NotNull(player);
            var bannedPlayer = service.BanPlayer(player.Id, DateTime.UtcNow.AddDays(1), "Cheating");
            Assert.NotNull(bannedPlayer);
            bool isBanned = service.IsPlayerBanned(out string? banReason, out DateTime? bannedUntil, null, player.Id);
            Assert.True(isBanned);
            Assert.Equal(bannedPlayer.BannedUntil, bannedUntil);
            Assert.Equal(bannedPlayer.BanReason, banReason);
        }

        [Fact]

        public void UnbanPlayer_ShouldUnbanSuccessfully()
        {
            var player = service.AddPlayer(new Player { Username = "unbanUser", Password = "pass" });
            Assert.NotNull(player);
            service.BanPlayer(player.Id, DateTime.UtcNow.AddDays(1), "Violation");
            var unbannedPlayer = service.UnbanPlayer(player.Id);
            Assert.NotNull(unbannedPlayer);
            bool isBanned = service.IsPlayerBanned(out string? banReason, out DateTime? bannedUntil, null, player.Id);
            Assert.False(isBanned);
            Assert.Null(banReason);
            Assert.Null(bannedUntil);
        }

        [Fact]
        public void BanShouldExpireAfterDate()
        {
            var player = service.AddPlayer(new Player { Username = "tempBanUser", Password = "pass" });
            Assert.NotNull(player);
            service.BanPlayer(player.Id, DateTime.UtcNow.AddSeconds(1), "Temporary Ban");
            bool isBannedInitially = service.IsPlayerBanned(out _, out _, null, player.Id);
            Assert.True(isBannedInitially);
            System.Threading.Thread.Sleep(2000);
            bool isBannedAfter = service.IsPlayerBanned(out _, out _, null, player.Id);
            Assert.False(isBannedAfter);
        }

        [Fact]
        public void PermanentBanPlayer_ShouldBanSuccessfully()
        {
            var player = service.AddPlayer(new Player { Username = "permBanUser", Password = "pass" });
            Assert.NotNull(player);
            var bannedPlayer = service.BanPlayer(player.Id, null, "Serious Violation");
            Assert.NotNull(bannedPlayer);
            bool isBanned = service.IsPlayerBanned(out string? banReason, out DateTime? bannedUntil, null, player.Id);
            Assert.True(isBanned);
            Assert.Null(bannedUntil);
            Assert.Equal(bannedPlayer.BanReason, banReason);
        }
    }
}
