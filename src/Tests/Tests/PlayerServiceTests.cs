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
            Assert.Equal("1234", result.Password);
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
    }
}
