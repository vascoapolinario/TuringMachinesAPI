using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using Xunit;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class LobbyServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly LobbyService service;

        public LobbyServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddScoped<PasswordHashService>();
            applicationDomain.Services.AddScoped<LobbyService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<LobbyService>();

            string? backupPath = applicationDomain.configuration.GetValue<string>("TestsDbBackup:FilePath");
            if (backupPath == null)
                throw new Exception("Não foi possível obter o caminho do ficheiro de configuração.");

            string sql = File.ReadAllText(backupPath);

            using (IServiceScope scope = applicationDomain.ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw(sql);
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            applicationDomain.Dispose();
        }

        private int CreateLevelWorkshopItem()
        {
            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();

            var item = new WorkshopItem
            {
                Name = "Test Level",
                Description = "Test Level Description",
                Type = WorkshopItemType.Level,
                AuthorId = 1,
                Rating = 0.0,
                Subscribers = null
            };

            db.WorkshopItems.Add(item);
            db.SaveChanges();
            return item.Id;
        }

        [Fact]
        public void GetAll_ShouldReturnEmpty_WhenNoLobbiesExist()
        {
            var result = service.GetAll().ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void GetAll_ShouldReturnExistingLobbies()
        {
            int levelId = CreateLevelWorkshopItem();
            var lobby1 = service.Create(1, "Lobby One", levelId, 4, null);
            var lobby2 = service.Create(2, "Lobby Two", levelId, 4, "pwd");

            var result = service.GetAll().ToList();
            Assert.Equal(2, result.Count);
            Assert.Contains(result, l => l.Name == "Lobby One" && l.HostPlayer == "Alice");
            Assert.Contains(result, l => l.Name == "Lobby Two" && l.HostPlayer == "Bob");
        }

        [Fact]
        public void GetAll_ShouldReturnFiltered_WhenFilterApplied()
        {
            int levelId = CreateLevelWorkshopItem();
            var lobby1 = service.Create(1, "Lobby One", levelId, 4, null);
            var lobby2 = service.Create(2, "Lobby Two", levelId, 4, "pwd");

            service.JoinLobby(lobby1!.Code, playerId: 3, password: null);
            service.StartLobby(lobby1!.Code, playerId: 1);
            var started = service.GetAll(includeStarted: true).ToList();

            Assert.Equal(2, started.Count);
            Assert.Single(service.GetAll(includeStarted: false).ToList());
        }

        [Fact]
        public void Create_ShouldCreateLobby_WithValidData()
        {
            int levelId = CreateLevelWorkshopItem();

            var lobby = service.Create(
                hostPlayerId: 1,
                name: "My Lobby",
                selectedLevelId: levelId,
                max_players: 4,
                password: "secret"
            );

            Assert.NotNull(lobby);
            Assert.Equal("My Lobby", lobby!.Name);
            Assert.Equal("Alice", lobby.HostPlayer);
            Assert.Equal("Test Level", lobby.LevelName);
            Assert.Equal(4, lobby.MaxPlayers);
            Assert.NotNull(lobby.Code);
            Assert.Equal(5, lobby.Code.Length);
            Assert.Equal("", lobby.Password);
            Assert.True(lobby.CreatedAt <= DateTime.UtcNow);
            Assert.Single(lobby.LobbyPlayers);
            Assert.Equal("Alice", lobby.LobbyPlayers.First());

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var entity = db.Lobbies.First(l => l.Id == lobby.Id);
            Assert.NotNull(entity.Password);
        }

        [Fact]
        public void Create_ShouldReturnNull_WhenHostAlreadyHasLobby()
        {
            int levelId = CreateLevelWorkshopItem();

            var first = service.Create(1, "First Lobby", levelId, 4, null);
            Assert.NotNull(first);

            var second = service.Create(1, "Second Lobby", levelId, 4, null);
            Assert.Null(second);
        }

        [Fact]
        public void GetByCode_ShouldReturnLobby_WhenExists()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Lobby For Code", levelId, 4, "pwd");

            Assert.NotNull(created);
            var fetched = service.GetByCode(created!.Code);

            Assert.NotNull(fetched);
            Assert.Equal(created.Id, fetched!.Id);
            Assert.Equal("Lobby For Code", fetched.Name);
            Assert.Equal("Alice", fetched.HostPlayer);
            Assert.Equal("Test Level", fetched.LevelName);
            Assert.Equal("", fetched.Password);
            Assert.True(fetched.PasswordProtected);
        }

        [Fact]
        public void GetByCode_ShouldReturnNull_WhenNotFound()
        {
            var result = service.GetByCode("99999");
            Assert.Null(result);
        }

        [Fact]
        public void JoinLobby_ShouldSucceed_ForOpenLobby()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Joinable Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool joined = service.JoinLobby(created!.Code, playerId: 2, password: null);

            Assert.True(joined);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var lobby = db.Lobbies.First(l => l.Id == created.Id);

            Assert.Contains(1, lobby.LobbyPlayers!);
            Assert.Contains(2, lobby.LobbyPlayers!);
        }

        [Fact]

        public void JoinLobby_ShouldSucceed_ForProtectedLobby_WithCorrectPassword()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Protected Join Lobby", levelId, 4, "secret");
            Assert.NotNull(created);
            bool joined = service.JoinLobby(created!.Code, playerId: 2, password: "secret");
            Assert.True(joined);
            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var lobby = db.Lobbies.First(l => l.Id == created.Id);
            Assert.Contains(1, lobby.LobbyPlayers!);
            Assert.Contains(2, lobby.LobbyPlayers!);
        }

        [Fact]
        public void JoinLobby_ShouldFail_WhenWrongPassword()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Protected Lobby", levelId, 4, "secret");
            Assert.NotNull(created);

            bool joined = service.JoinLobby(created!.Code, playerId: 2, password: "wrong");

            Assert.False(joined);
        }

        [Fact]
        public void JoinLobby_ShouldFail_WhenAlreadyInLobby()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Double Join Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool first = service.JoinLobby(created!.Code, playerId: 2, password: null);
            bool second = service.JoinLobby(created.Code, playerId: 2, password: null);

            Assert.True(first);
            Assert.False(second);
        }

        [Fact]
        public void LeaveLobby_ShouldRemovePlayer()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Leave Lobby", levelId, 4, null);
            Assert.NotNull(created);

            service.JoinLobby(created!.Code, playerId: 2, password: null);

            bool result = service.LeaveLobby(created.Code, playerId: 2);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var lobby = db.Lobbies.First(l => l.Id == created.Id);

            Assert.DoesNotContain(2, lobby.LobbyPlayers!);
            Assert.Contains(1, lobby.LobbyPlayers!);
        }

        [Fact]
        public void LeaveLobby_HostLeaving_ShouldDeleteLobby()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Host Leaves Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool result = service.LeaveLobby(created!.Code, playerId: 1);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            Assert.False(db.Lobbies.Any(l => l.Id == created.Id));
        }

        [Fact]
        public void StartLobby_ShouldFail_IfNotHost()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Start Fail Lobby", levelId, 4, null);
            Assert.NotNull(created);

            service.JoinLobby(created!.Code, playerId: 2, password: null);

            bool result = service.StartLobby(created.Code, playerId: 2);

            Assert.False(result);
        }

        [Fact]
        public void StartLobby_ShouldFail_IfPlayerCountInvalid()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Not Enough Players", levelId, 4, null);
            Assert.NotNull(created);

            bool result = service.StartLobby(created!.Code, playerId: 1);

            Assert.False(result);
        }

        [Fact]
        public void StartLobby_ShouldSucceed_WithValidPlayers()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Ready Lobby", levelId, 4, null);
            Assert.NotNull(created);

            service.JoinLobby(created!.Code, playerId: 2, password: null);

            bool result = service.StartLobby(created.Code, playerId: 1);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var lobby = db.Lobbies.First(l => l.Id == created.Id);

            Assert.True(lobby.HasStarted);
        }

        [Fact]
        public void DeleteLobby_ShouldFail_WhenNotHostOrAdmin()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Delete Fail Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool result = service.DeleteLobby(created!.Code, userId: 5);

            Assert.False(result);
        }

        [Fact]
        public void DeleteLobby_ShouldSucceed_WhenHost()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Host Delete Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool result = service.DeleteLobby(created!.Code, userId: 1);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            Assert.False(db.Lobbies.Any(l => l.Id == created.Id));
        }

        [Fact]
        public void DeleteLobby_ShouldSucceed_WhenAdmin()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Admin Delete Lobby", levelId, 4, null);
            Assert.NotNull(created);

            bool result = service.DeleteLobby(created!.Code, userId: 2);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            Assert.False(db.Lobbies.Any(l => l.Id == created.Id));
        }

        [Fact]
        public void GetAll_ShouldFilterByStartedAndCode()
        {
            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();

            var lobby1 = new Lobby
            {
                Code = "12345",
                Name = "Lobby One",
                Password = null,
                HostPlayerId = 1,
                SelectedLevelId = 0,
                MaxPlayers = 4,
                HasStarted = false,
                CreatedAt = DateTime.UtcNow,
                LobbyPlayers = new System.Collections.Generic.List<int> { 1 }
            };

            var lobby2 = new Lobby
            {
                Code = "99999",
                Name = "Lobby Two",
                Password = null,
                HostPlayerId = 2,
                SelectedLevelId = 0,
                MaxPlayers = 4,
                HasStarted = true,
                CreatedAt = DateTime.UtcNow,
                LobbyPlayers = new System.Collections.Generic.List<int> { 2, 3 }
            };

            db.Lobbies.Add(lobby1);
            db.Lobbies.Add(lobby2);
            db.SaveChanges();

            var onlyNotStarted = service.GetAll(includeStarted: false).ToList();
            Assert.Single(onlyNotStarted);
            Assert.Equal("12345", onlyNotStarted[0].Code);

            var all = service.GetAll(includeStarted: true).ToList();
            Assert.Equal(2, all.Count);

            var filtered = service.GetAll(codeFilter: "123", includeStarted: true).ToList();
            Assert.Single(filtered);
            Assert.Equal("12345", filtered[0].Code);
        }

        [Fact]
        public void GetEntityByPlayerId_ShouldReturnLobby_WhenPlayerIsInLobby()
        {
            int levelId = CreateLevelWorkshopItem();
            var created = service.Create(1, "Entity Lobby", levelId, 4, null);
            Assert.NotNull(created);

            service.JoinLobby(created!.Code, playerId: 2, password: null);

            var entity = service.GetEntityByPlayerId(2);

            Assert.NotNull(entity);
            Assert.Equal(created.Id, entity!.Id);
        }

        [Fact]
        public void GetPlayerIdFromName_ShouldReturnCorrectId()
        {
            int aliceId = service.GetPlayerIdFromName("Alice");
            int aliceLowerId = service.GetPlayerIdFromName("alice");
            int unknown = service.GetPlayerIdFromName("NonExistentUser");

            Assert.Equal(1, aliceId);
            Assert.Equal(1, aliceLowerId);
            Assert.Equal(0, unknown);
        }
    }
}
