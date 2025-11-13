using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;
using Xunit;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class WorkshopItemServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly WorkshopItemService service;

        public WorkshopItemServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddScoped<WorkshopItemService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<WorkshopItemService>();

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

        private static JsonElement ParseJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        [Fact]
        public void GetAll_ShouldReturnItems_WhenItemsExist()
        {
            var jsonLevel = ParseJson("""
            {
              "name": "Existing Level",
              "description": "A level that exists",
              "type": "Level",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "detailedDescription": "Details",
              "objective": "Objective",
              "mode": "accept",
              "twoTapes": false
            }
            """);
            var jsonMachine = ParseJson("""
            {
              "name": "Existing Machine",
              "description": "A machine that exists",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);
            service.AddWorkshopItem(jsonLevel, UserId: 1);
            service.AddWorkshopItem(jsonMachine, UserId: 1);
            var result = service.GetAll(null, UserId: 1).ToList();
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetAll_ShouldReturnEmpty_WhenNoItemsExist()
        {
            var result = service.GetAll(null, UserId: 1).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void AddWorkshopItem_Level_ShouldCreateLevelAndWorkshopItem()
        {
            var json = ParseJson("""
            {
              "name": "Test Level",
              "description": "Simple test level",
              "type": "Level",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "detailedDescription": "Some details",
              "objective": "Accept some language",
              "mode": "accept",
              "twoTapes": false
            }
            """);

            var dto = service.AddWorkshopItem(json, UserId: 1);

            Assert.NotNull(dto);
            var levelDto = Assert.IsType<LevelWorkshopItem>(dto);

            Assert.True(levelDto.Id > 0);
            Assert.True(levelDto.LevelId > 0);
            Assert.Equal("Test Level", levelDto.Name);
            Assert.Equal("Simple test level", levelDto.Description);
            Assert.Equal("accept", levelDto.Mode.ToLower());
        }

        [Fact]
        public void AddWorkshopItem_Machine_ShouldCreateMachineAndWorkshopItem()
        {
            var json = ParseJson("""
            {
              "name": "Test Machine",
              "description": "Simple machine",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[{\"id\":0,\"isStart\":true}]",
              "connectionsJson": "[]"
            }
            """);

            var dto = service.AddWorkshopItem(json, UserId: 1);

            Assert.NotNull(dto);
            var machineDto = Assert.IsType<MachineWorkshopItem>(dto);

            Assert.True(machineDto.Id > 0);
            Assert.True(machineDto.MachineId > 0);
            Assert.Equal("Test Machine", machineDto.Name);
        }

        [Fact]
        public void AddWorkshopItem_ShouldReturnNull_WhenTypeIsInvalid()
        {
            var json = ParseJson("""
            {
              "name": "Invalid Type",
              "description": "Should fail",
              "type": "SomethingWeird"
            }
            """);

            var dto = service.AddWorkshopItem(json, UserId: 1);

            Assert.Null(dto);
        }

        [Fact]
        public void GetById_ShouldReturnLevelWorkshopItem_WhenLevelExists()
        {
            var json = ParseJson("""
            {
              "name": "Level For GetById",
              "description": "Level to fetch",
              "type": "Level",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "detailedDescription": "Some details",
              "objective": "Objective",
              "mode": "accept",
              "twoTapes": true
            }
            """);

            var created = (LevelWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            var fetched = service.GetById(created.Id, UserId: 1);

            var levelDto = Assert.IsType<LevelWorkshopItem>(fetched);
            Assert.Equal(created.Id, levelDto.Id);
            Assert.Equal("Level For GetById", levelDto.Name);
            Assert.True(levelDto.TwoTapes);
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenWorkshopItemDoesNotExist()
        {
            var result = service.GetById(id: 999999, UserId: 1);

            Assert.Null(result);
        }

        [Fact]
        public void RateWorkshopItem_FirstRating_ShouldSetRating()
        {

            var json = ParseJson("""
            {
              "name": "Rate Me",
              "description": "Machine to rate",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            var success = service.RateWorkshopItem(userId: 1, ItemId: created.Id, Rating: 5);

            Assert.True(success);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var entity = db.WorkshopItems.First(w => w.Id == created.Id);

            Assert.Equal(5.0, entity.Rating, 3);
        }

        [Fact]
        public void RateWorkshopItem_UpdateExistingReview_ShouldUpdateAverage()
        {
            var json = ParseJson("""
            {
              "name": "Rate Me Twice",
              "description": "Machine to rate twice",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;


            service.RateWorkshopItem(userId: 1, ItemId: created.Id, Rating: 3);

            service.RateWorkshopItem(userId: 1, ItemId: created.Id, Rating: 5);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            var entity = db.WorkshopItems.First(w => w.Id == created.Id);

            Assert.Equal(5.0, entity.Rating, 3);
        }

        [Fact]
        public void SubscribeToWorkshopItem_ShouldSubscribeAndNotDuplicate()
        {
            var json = ParseJson("""
            {
              "name": "Subscribable",
              "description": "Item to subscribe",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;
            int userId = 1;

            var first = service.SubscribeToWorkshopItem(userId, created.Id);
            var second = service.SubscribeToWorkshopItem(userId, created.Id);

            Assert.True(first);
            Assert.False(second);
            Assert.True(service.IsUserSubscribed(userId, created.Id));
        }

        [Fact]
        public void UnsubscribeFromWorkshopItem_ShouldUnsubscribe()
        {
            var json = ParseJson("""
            {
              "name": "Unsubscribable",
              "description": "Item to unsubscribe",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;
            int userId = 1;

            service.SubscribeToWorkshopItem(userId, created.Id);
            Assert.True(service.IsUserSubscribed(userId, created.Id));

            var result = service.UnsubscribeFromWorkshopItem(userId, created.Id);

            Assert.True(result);
            Assert.False(service.IsUserSubscribed(userId, created.Id));
        }

        [Fact]
        public void UserRatingForItem_ShouldReturnNull_WhenNoRatingExists()
        {
            var json = ParseJson("""
            {
              "name": "No Rating Yet",
              "description": "No ratings",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            var rating = service.UserRatingForItem(userId: 1, workshopItemId: created.Id);

            Assert.Null(rating);
        }

        [Fact]
        public void UserRatingForItem_ShouldReturnRating_WhenExists()
        {
            var json = ParseJson("""
            {
              "name": "Rated Item",
              "description": "Has a rating",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;
            service.RateWorkshopItem(userId: 1, ItemId: created.Id, Rating: 4);

            var rating = service.UserRatingForItem(userId: 1, workshopItemId: created.Id);

            Assert.NotNull(rating);
            Assert.Equal(4, rating.Value);
        }

        [Fact]
        public void DeleteWorkshopItem_ShouldFail_WhenUserIsNotAuthorOrAdmin()
        {
            var json = ParseJson("""
            {
              "name": "Delete Me",
              "description": "Item to test delete",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            bool result = service.DeleteWorkshopItem(workshopItemId: created.Id, userId: 5);

            Assert.False(result);
        }

        [Fact]
        public void DeleteWorkshopItem_ShouldSucceed_WhenCalledByAuthor()
        {
            var json = ParseJson("""
            {
              "name": "Author Delete",
              "description": "Author should delete",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            bool result = service.DeleteWorkshopItem(workshopItemId: created.Id, userId: 1);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            Assert.False(db.WorkshopItems.Any(w => w.Id == created.Id));
        }

        [Fact]
        public void DeleteWorkshopItem_ShouldSucceed_WhenCalledByAdmin()
        {
            var json = ParseJson("""
            {
              "name": "Admin Delete",
              "description": "Admin should delete",
              "type": "Machine",
              "alphabetJson": "[\"0\",\"1\",\"_\"]",
              "nodesJson": "[]",
              "connectionsJson": "[]"
            }
            """);

            var created = (MachineWorkshopItem)service.AddWorkshopItem(json, UserId: 1)!;

            bool result = service.DeleteWorkshopItem(workshopItemId: created.Id, userId: 2);

            Assert.True(result);

            using var scope = applicationDomain.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
            Assert.False(db.WorkshopItems.Any(w => w.Id == created.Id));
        }
    }
}
