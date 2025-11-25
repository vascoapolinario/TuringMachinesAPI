using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using Xunit;
using Dtos = TuringMachinesAPI.Dtos;
using Entities = TuringMachinesAPI.Entities;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class AdminLogServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly AdminLogService service;

        public AdminLogServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddScoped<AdminLogService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<AdminLogService>();

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
        public async Task CreateAdminLog_ShouldCreateSuccessfullyAsync()
        {
            int actorId = 2;
            ActionType action = ActionType.Create;
            TargetEntityType targetType = TargetEntityType.Player;
            int target = 1;

            Dtos.AdminLog? log = await service.CreateAdminLog(actorId, action, targetType, target);

            Assert.NotNull(log);

            Assert.Equal("Bob", log.ActorName);
            Assert.Equal("Admin", log.ActorRole);
            Assert.Equal("Create", log.Action);
            Assert.Equal("Player", log.TargetEntityType);
            Assert.Equal(1, log.TargetEntityId);
            Assert.Equal("Alice", log.TargetEntityName);
            Assert.True((DateTime.UtcNow - log.DoneAt).TotalSeconds < 10);
        }

        [Fact]
        public void GetAllAdminLogs_ShouldReturnLogsSuccessfully()
        {
            var log1 = service.CreateAdminLog(2, ActionType.Create, TargetEntityType.Player, 1);
            var log2 = service.CreateAdminLog(2, ActionType.Create, TargetEntityType.WorkshopLevel, 2);

            var logs = service.GetAllAdminLogs();

            Assert.NotNull(logs);
            Assert.Equal(2, logs.Count());
        }

        [Fact]
        public async Task CreateAdminLog_WithInvalidActorId_ShouldReturnNull()
        {
            int invalidActorId = 999;
            ActionType action = ActionType.Delete;
            TargetEntityType targetType = TargetEntityType.Lobby;
            int target = 1;
            Dtos.AdminLog? log = await service.CreateAdminLog(invalidActorId, action, targetType, target);
            Assert.Null(log);
        }

        [Fact]
        public void GetAllAdminLogs_WhenNoLogsExist_ShouldReturnEmptyCollection()
        {
            var logs = service.GetAllAdminLogs();
            Assert.NotNull(logs);
            Assert.Empty(logs);
        }

        [Fact]
        public void GetByActorName_ShouldReturnCorrectLogs()
        {
            var log1 = service.CreateAdminLog(2, ActionType.Create, TargetEntityType.Player, 1);
            var log2 = service.CreateAdminLog(2, ActionType.Delete, TargetEntityType.Lobby, 1);
            var logs = service.GetAdminLogsByActorName("Bob");
            Assert.NotNull(logs);
            Assert.Equal(2, logs.Count());
        }

        [Fact]
        public void GetByActorName_WithNoMatchingLogs_ShouldReturnEmptyCollection()
        {
            var logs = service.GetAdminLogsByActorName("NonExistentActor");
            Assert.NotNull(logs);
            Assert.Empty(logs);
        }

        [Fact]
        public async Task DeleteAdminLog_ShouldDeleteSuccessfullyAsync()
        {
            var log = await service.CreateAdminLog(2, ActionType.Create, TargetEntityType.Player, 1);
            Assert.NotNull(log);
            bool isDeleted = service.DeleteAdminLog(log.Id);
            Assert.True(isDeleted);
            var logs = service.GetAllAdminLogs();
            Assert.Empty(logs);
        }

        [Fact]
        public void DeleteAdminLog_WithInvalidId_ShouldBeFalse()
        {
            int invalidLogId = 999;
            bool isDeleted = service.DeleteAdminLog(invalidLogId);
            Assert.False(isDeleted);
        }
    }
}
