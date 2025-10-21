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

        public IEnumerable<object> GetAll(string? NameFilter)
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
            foreach (Entities.WorkshopItem i in Items)
            {
                if (i.Type == "Level")
                {
                    var level = db.Levels
                        .AsNoTracking()
                        .FirstOrDefault(l => l.WorkshopItemId == i.Id);
                    if (level != null)
                    {
                        yield return new Dtos.Level
                        {
                            Id = i.Id,
                            LevelId = level.Id,
                            Name = i.Name,
                            Description = i.Description,
                            Author = db.Players
                                .AsNoTracking()
                                .Where(p => p.Id == i.AuthorId)
                                .Select(p => p.Username)
                                .FirstOrDefault() ?? "Unknown",
                            Type = i.Type,
                            LevelType = level.LevelType,
                            Rating = i.Rating,
                            LevelData = level.LevelData,
                            Subscribers = i.Subscribers?.Count ?? 0
                        };
                        continue;
                    }
                }
                else if (i.Type == "Machine")
                {
                    var machine = db.Machines
                        .AsNoTracking()
                        .FirstOrDefault(m => m.WorkshopItemId == i.Id);
                    if (machine != null)
                    {
                        yield return new Dtos.Machine
                        {
                            Id = i.Id,
                            MachineId = machine.Id,
                            Name = i.Name,
                            Description = i.Description,
                            Author = db.Players
                                .AsNoTracking()
                                .Where(p => p.Id == i.AuthorId)
                                .Select(p => p.Username)
                                .FirstOrDefault() ?? "Unknown",
                            Type = i.Type,
                            Rating = i.Rating,
                            MachineData = machine.MachineData,
                            Subscribers = i.Subscribers?.Count ?? 0
                        };
                        continue;
                    }
                }
            }
        }

        public object? GetById(int id)
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
            
            if (entity.Type == "Level")
            {
                var level = db.Levels
                    .AsNoTracking()
                    .FirstOrDefault(l => l.WorkshopItemId == entity.Id);

                return new Dtos.Level
                {
                    Id = entity.Id,
                    LevelId = level?.Id ?? 0,
                    Name = entity.Name,
                    Description = entity.Description,
                    Author = authorName,
                    Type = entity.Type,
                    LevelType = level?.LevelType ?? "",
                    Rating = entity.Rating,
                    LevelData = level?.LevelData ?? "",
                    Subscribers = entity.Subscribers?.Count ?? 0
                };
            }
            else if (entity.Type == "Machine")
            {
                var machine = db.Machines
                    .AsNoTracking()
                    .FirstOrDefault(m => m.WorkshopItemId == entity.Id);
                return new Dtos.Machine
                {
                    Id = entity.Id,
                    MachineId = machine?.Id ?? 0,
                    Name = entity.Name,
                    Description = entity.Description,
                    Author = authorName,
                    Type = entity.Type,
                    Rating = entity.Rating,
                    MachineData = machine?.MachineData ?? "",
                    Subscribers = entity.Subscribers?.Count ?? 0
                };
            }
            else
            {
                return new Dtos.WorkshopItem
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    Author = authorName,
                    Type = entity.Type,
                    Rating = entity.Rating,
                    Subscribers = entity.Subscribers?.Count ?? 0
                };
            }
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

            if (type == "Level")
            {
                var LevelItem = new Entities.Level
                {
                    WorkshopItemId = newItem.Id,
                    LevelType = "Workshop",
                    LevelData = jsonElement.GetProperty("levelData").GetRawText()
                };
                db.Levels.Add(LevelItem);
                db.SaveChanges();

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
                    LevelData = LevelItem.LevelData,
                    Subscribers = 0
                };
            }
            else if (type == "Macine")
            {
                var MachineItem = new Entities.Machine
                {
                    WorkshopItemId = newItem.Id,
                    MachineData = jsonElement.GetProperty("machineData").GetRawText()
                };
                db.Machines.Add(MachineItem);
                db.SaveChanges();

                return new Dtos.Machine
                {
                    Id = newItem.Id,
                    MachineId = MachineItem.Id,
                    Name = newItem.Name,
                    Description = newItem.Description,
                    Author = authorName ?? "Unknown",
                    Type = newItem.Type,
                    Rating = newItem.Rating,
                    MachineData = MachineItem.MachineData,
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
                return false;
            var existingReview = db.Reviews.FirstOrDefault(r => r.UserId == userId && r.WorkshopItemId == ItemId);
            if (existingReview != null)
            {
                existingReview.Rating = Rating;
                db.SaveChanges();
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

            db.Reviews.Add(review);
            db.SaveChanges();

            WorkShopItem.Rating = db.Reviews
                .Where(r => r.WorkshopItemId == ItemId)
                .Select(r => r.Rating)
                .Average();

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

        public int? UserRatingForItem(int userId, int workshopItemId)
        {
            var review = db.Reviews.FirstOrDefault(r => r.UserId == userId && r.WorkshopItemId == workshopItemId);
            if (review == null)
            {
                return null;
            }
            return review.Rating;
        }
    }
}
