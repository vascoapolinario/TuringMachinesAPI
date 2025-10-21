using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;

namespace TuringMachinesAPI.Services
{
    public class WorkshopItemService
    {
        private readonly TuringMachinesDbContext db;

        public WorkshopItemService(TuringMachinesDbContext context)
        {
            db = context;
        }

        public IEnumerable<Dtos.WorkshopItem> GetAll(string? NameFilter)
        {
            var Items = db.WorkshopItems
                .AsNoTracking()
                .Select(wi => new Entities.WorkshopItem
                {
                    Id = wi.Id,
                    Name = wi.Name,
                    Type = wi.Type,
                    Description = wi.Description,
                    AuthorId = wi.AuthorId,
                    Rating = wi.Rating,
                    Subscribers = null
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(NameFilter))
            {
                Items = Items
                    .Where(wi => wi.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return Items.Select(wi => new Dtos.WorkshopItem
            {
                Id = wi.Id,
                Name = wi.Name,
                Type = wi.Type,
                Description = wi.Description,
                Author = db.Players
                    .AsNoTracking()
                    .Where(p => p.Id == wi.AuthorId)
                    .Select(p => p.Username)
                    .FirstOrDefault() ?? "Unknown",
                Rating = wi.Rating,
                Subscribers = db.WorkshopItems
                    .AsNoTracking()
                    .Where(w => w.Id == wi.Id && w.Subscribers != null)
                    .Select(w => w.Subscribers!.Count)
                    .FirstOrDefault()
            });
        }

        public Dtos.WorkshopItem? GetById(int id)
        {
            var entity = db.WorkshopItems
                .AsNoTracking()
                .FirstOrDefault(wi => wi.Id == id);
            if (entity is null) return null;
            var authorName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == entity.AuthorId)
                .Select(p => p.Username)
                .FirstOrDefault() ?? "Unknown";
            return new Dtos.WorkshopItem
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                Description = entity.Description,
                Author = authorName,
                Rating = entity.Rating,
                Subscribers = entity.Subscribers?.Count ?? 0
            };
        }

        public Dtos.WorkshopItem? AddWorkshopItem(JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Object)
                return null;
            var name = jsonElement.GetProperty("name").GetString() ?? "";
            var description = jsonElement.GetProperty("description").GetString() ?? "";
            var authorName = jsonElement.GetProperty("authorName").GetString();
            var type = jsonElement.GetProperty("type").GetString() ?? "";
            var newItem = new Entities.WorkshopItem
            {
                Name = name,
                Description = description,
                AuthorId = db.Players
                    .AsNoTracking()
                    .Where(p => p.Username == authorName)
                    .Select(p => p.Id)
                    .FirstOrDefault(),
                Type = type,
                Rating = 0.0,
                Subscribers = null
            };
            db.WorkshopItems.Add(newItem);
            db.SaveChanges();

            if (newItem.Type == "Level")
            {
                var LevelItem = new Entities.Level
                {
                    WorkshopItemId = newItem.Id,
                    LevelType = "Workshop",
                    LevelData = jsonElement.GetProperty("levelData").GetRawText()
                };

                return new Dtos.Level
                {
                    Id = newItem.Id,
                    LevelId = LevelItem.Id,
                    Name = newItem.Name,
                    Description = newItem.Description,
                    Author = authorName ?? "Unknown",
                    Type = newItem.Type,
                    LevelType = LevelItem.LevelType,
                    Rating = newItem.Rating,
                    Subscribers = 0
                };
            }
            else if (newItem.Type == "Macine")
            {
                var MachineItem = new Entities.Machine
                {
                    WorkshopItemId = newItem.Id,
                    MachineData = jsonElement.GetProperty("machineData").GetRawText()
                };
                return new Dtos.Machine
                {
                    Id = newItem.Id,
                    MachineId = MachineItem.Id,
                    Name = newItem.Name,
                    Description = newItem.Description,
                    Author = authorName ?? "Unknown",
                    Type = newItem.Type,
                    Rating = newItem.Rating,
                    Subscribers = 0
                };
            }
            else
            {
                return new Dtos.WorkshopItem
                {
                    Id = newItem.Id,
                    Name = newItem.Name,
                    Description = newItem.Description,
                    Author = authorName ?? "Unknown",
                    Type = newItem.Type,
                    Rating = newItem.Rating,
                    Subscribers = 0
                };
            }
        }

        public bool RateWorkshopItem(int userId, int ItemId, int Rating)
        {
            var WorkShopItem = db.WorkshopItems.FirstOrDefault(wi => wi.Id == ItemId);
            if (WorkShopItem == null)
            {
                return false;
            }
            if (db.Reviews.Any(r => r.UserId == userId && r.WorkshopItemId == ItemId))
            {
                var existingReview = db.Reviews.First(r => r.UserId == userId && r.WorkshopItemId == ItemId);
                existingReview.Rating = Rating;
                WorkShopItem.Rating = db.Reviews
                    .Where(r => r.WorkshopItemId == ItemId)
                    .Select(r => r.Rating)
                    .Average();
                db.SaveChanges();
                return true;
            }
            var review = new Entities.Review
            {
                UserId = userId,
                WorkshopItemId = ItemId,
                Rating = Rating
            };
            WorkShopItem!.Rating = db.Reviews
                .Where(r => r.WorkshopItemId == ItemId)
                .Select(r => r.Rating)
                .Append(Rating)
                .Average();
            db.Reviews.Add(review);
            db.SaveChanges();
            return true;
        }

        public bool SubscribeToWorkshopItem(int userId, int workshopItemId)
        {
            var workshopItem = db.WorkshopItems.FirstOrDefault(wi => wi.Id == workshopItemId);
            if (workshopItem == null)
            {
                return false;
            }
            if (workshopItem.Subscribers == null)
            {
                workshopItem.Subscribers = new List<int>();
            }
            if (!IsUserSubscribed(userId, workshopItemId))
            {
                workshopItem.Subscribers.Add(userId);
                db.SaveChanges();
                return true;
            }
            return false;
        }

        public bool UnsubscribeFromWorkshopItem(int userId, int workshopItemId)
        {
            if (IsUserSubscribed(userId, workshopItemId))
            {
                var workshopItem = db.WorkshopItems.FirstOrDefault(wi => wi.Id == workshopItemId);
                workshopItem!.Subscribers!.Remove(userId);
                db.SaveChanges();
                return true;
            }

            return false;
        }

        public bool IsUserSubscribed(int userId, int workshopItemId)
        {
            var workshopItem = db.WorkshopItems.FirstOrDefault(wi => wi.Id == workshopItemId);
            if (workshopItem == null || workshopItem.Subscribers == null)
            {
                return false;
            }
            return workshopItem.Subscribers.Contains(userId);
        }
    }
}
