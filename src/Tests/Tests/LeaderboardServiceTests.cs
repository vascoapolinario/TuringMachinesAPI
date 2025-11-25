using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TuringMachinesAPI.DataSources;
using Entities = TuringMachinesAPI.Entities;
using Dtos = TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using Xunit;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class LeaderboardServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly LeaderboardService service;

        public LeaderboardServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddSingleton<ICryptoService, AesCryptoService>();
            applicationDomain.Services.AddScoped<LeaderboardService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<LeaderboardService>();

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

        [Fact]
        public void AddLeaderboardLevel_shouldAddSuccessfully()
        {   
            var addedLevel = service.AddLeaderboardLevel("level1", "Starter");
            Assert.NotNull(addedLevel);
            Assert.Equal("level1", addedLevel.Name);
            Assert.Equal("Starter", addedLevel.Category);
        }

        [Fact]
        public void AddLeaderboardLevel_shouldReturnNullWhenLevelExists()
        {
            var addedLevel1 = service.AddLeaderboardLevel("level2", "Starter");
            Assert.NotNull(addedLevel1);
            var addedLevel2 = service.AddLeaderboardLevel("level2", "Starter");
            Assert.Null(addedLevel2);
        }

        [Fact]
        public void AddLevelSubmissions_shouldAddSuccessfully()
        {
            var level = service.AddLeaderboardLevel("Level3", "Starter");
            Assert.NotNull(level);


            var Submission = service.AddSubmission(1, "Level3", 120.5, 10, 6);

            Assert.NotNull(Submission);
            Assert.Equal("Level3", Submission.LevelName);
            Assert.Equal(120.5, Submission.Time);
            Assert.Equal(10, Submission.NodeCount);
            Assert.Equal(6, Submission.ConnectionCount);
        }

        [Fact]
        public void GetLeaderboard_shouldReturnCorrectlyWithOrder()
        {
            var level = service.AddLeaderboardLevel("Level4", "Starter");
            Assert.NotNull(level);
            var submission1 = service.AddSubmission(1, "Level4", 100.0, 8, 5);
            var submission2 = service.AddSubmission(2, "Level4", 95.0, 9, 4);
            var submission3 = service.AddSubmission(3, "Level4", 100.0, 7, 6);
            var leaderboard = service.GetLeaderboard("Level4").ToList();

            Assert.Equal(3, leaderboard.Count);
            Assert.Equal(leaderboard[0].PlayerName, submission3!.PlayerName);
            Assert.Equal(leaderboard[1].PlayerName, submission1!.PlayerName);
            Assert.Equal(leaderboard[2].PlayerName, submission2!.PlayerName);
        }

        [Fact]
        public void GetLeaderboard_shouldFilterByLevelName()
        {
            var levelA = service.AddLeaderboardLevel("AlphaLevel", "Starter");
            var levelB = service.AddLeaderboardLevel("BetaLevel", "Starter");
            Assert.NotNull(levelA);
            Assert.NotNull(levelB);

            var submissionA = service.AddSubmission(1, "AlphaLevel", 110.0, 9, 5);
            var submissionB = service.AddSubmission(2, "BetaLevel", 105.0, 8, 4);
            Assert.NotNull(submissionA);
            Assert.NotNull(submissionB);

            var leaderboard = service.GetLeaderboard("Alpha").ToList();
            Assert.Single(leaderboard);
            Assert.Equal("AlphaLevel", leaderboard[0].LevelName);
            Assert.Equal(submissionA!.PlayerName, leaderboard[0].PlayerName);
        }

        [Fact]
        public void GetPlayerLeaderboard_shouldReturnCorrectlyWithOrder()
        {
            var level = service.AddLeaderboardLevel("Level5", "Starter");
            var level2 =  service.AddLeaderboardLevel("Level6", "Starter");
            var level3 = service.AddLeaderboardLevel("Level7", "Starter");
            Assert.NotNull(level);
            Assert.NotNull(level2);
            Assert.NotNull(level3);

            var submission1 = service.AddSubmission(1, "Level5", 120.0, 10, 5);
            var submission2 = service.AddSubmission(1, "Level6", 115.0, 9, 6);
            var submission3 = service.AddSubmission(1, "Level7", 120.0, 8, 7);
            var playerLeaderboard = service.GetPlayerLeaderboard(1).ToList();
            Assert.Equal(3, playerLeaderboard.Count);

            Assert.Equal(playerLeaderboard[0].NodeCount, submission3!.NodeCount);
            Assert.Equal(playerLeaderboard[1].NodeCount, submission2!.NodeCount);
            Assert.Equal(playerLeaderboard[2].NodeCount, submission1!.NodeCount);
        }

        [Fact]
        public void DeletePlayerSubmissions_shouldDeleteSuccessfully()
        {
            var level = service.AddLeaderboardLevel("Level6", "Starter");
            var level2 = service.AddLeaderboardLevel("Level7", "Starter");
            Assert.NotNull(level);
            Assert.NotNull(level2);

            var submission1 = service.AddSubmission(2, "Level6", 130.0, 11, 5);
            var submission2 = service.AddSubmission(2, "Level7", 125.0, 10, 6);
            var playerLeaderboardBeforeDeletion = service.GetPlayerLeaderboard(2, "Level6").ToList();
            Assert.Single(playerLeaderboardBeforeDeletion);

            service.DeletePlayerSubmission("Bob", "Level6");
            var playerLeaderboardAfterDeletion = service.GetPlayerLeaderboard(2, "Level6").ToList();
            Assert.Empty(playerLeaderboardAfterDeletion);
            Assert.NotEmpty(service.GetPlayerLeaderboard(2, "Level7").ToList());
        }

    }
}
