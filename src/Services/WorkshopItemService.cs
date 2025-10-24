using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Services
{
    public class WorkshopItemService
    {
        private readonly TuringMachinesDbContext db;

        public WorkshopItemService(TuringMachinesDbContext context)
        {
            db = context;
        }

        public IEnumerable<object> GetAll(string? NameFilter, int UserId)
        {
            var query = db.WorkshopItems.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(NameFilter))
                query = query.Where(wi => wi.Name.ToLower().Contains(NameFilter.ToLower()));

            var items = query.ToList();

            foreach (var i in items)
            {
                var authorName = db.Players
                    .AsNoTracking()
                    .Where(p => p.Id == i.AuthorId)
                    .Select(p => p.Username)
                    .FirstOrDefault() ?? "Unknown";

                int UserRating = UserRatingForItem(UserId, i.Id) ?? 0;

                if (i.Type.Equals(WorkshopItemType.Level))
                {
                    var level = db.Levels.AsNoTracking().FirstOrDefault(l => l.WorkshopItemId == i.Id);
                    if (level != null)
                    {
                        yield return new Dtos.LevelWorkshopItem
                        {
                            Id = i.Id,
                            LevelId = level.Id,
                            Name = i.Name,
                            Description = i.Description,
                            Author = authorName,
                            Type = i.Type.ToString(),
                            Rating = i.Rating,
                            Subscribers = i.Subscribers?.Count ?? 0,
                            LevelType = level.LevelType,
                            DetailedDescription = level.DetailedDescription,
                            Mode = level.Mode.ToString(),
                            AlphabetJson = level.AlphabetJson,
                            TransformTestsJson = level.TransformTestsJson,
                            CorrectExamplesJson = level.CorrectExamplesJson,
                            WrongExamplesJson = level.WrongExamplesJson,
                            UserRating = UserRating,
                            UserIsSubscribed = IsUserSubscribed(UserId, i.Id)
                        };
                        continue;
                    }
                }
                else if (i.Type.Equals(WorkshopItemType.Machine))
                {
                    var machine = db.Machines.AsNoTracking().FirstOrDefault(m => m.WorkshopItemId == i.Id);
                    if (machine != null)
                    {
                        yield return new Dtos.MachineWorkshopItem
                        {
                            Id = i.Id,
                            MachineId = machine.Id,
                            Name = i.Name,
                            Description = i.Description,
                            Author = authorName,
                            Type = i.Type.ToString(),
                            Rating = i.Rating,
                            Subscribers = i.Subscribers?.Count ?? 0,
                            AlphabetJson = machine.AlphabetJson,
                            NodesJson = machine.NodesJson,
                            ConnectionsJson = machine.ConnectionsJson,
                            UserRating = UserRating,
                            UserIsSubscribed = IsUserSubscribed(UserId, i.Id)
                        };
                        continue;
                    }
                }

                yield return new Dtos.WorkshopItem
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Author = authorName,
                    Type = i.Type.ToString(),
                    Rating = i.Rating,
                    Subscribers = i.Subscribers?.Count ?? 0,
                    UserRating = UserRating,
                    UserIsSubscribed = IsUserSubscribed(UserId, i.Id)
                };
            }
        }

        public object? GetById(int id, int UserId)
        {
            var entity = db.WorkshopItems.AsNoTracking().FirstOrDefault(wi => wi.Id == id);
            if (entity == null)
                return null;

            var authorName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == entity.AuthorId)
                .Select(p => p.Username)
                .FirstOrDefault() ?? "Unknown";

            int UserRating = UserRatingForItem(UserId, entity.Id) ?? 0;


            if (entity.Type.Equals(WorkshopItemType.Level))
            {
                var level = db.Levels.AsNoTracking().FirstOrDefault(l => l.WorkshopItemId == entity.Id);
                if (level == null) return null;

                return new Dtos.LevelWorkshopItem
                {
                    Id = entity.Id,
                    LevelId = level.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    Author = authorName,
                    Type = entity.Type.ToString(),
                    Rating = entity.Rating,
                    Subscribers = entity.Subscribers?.Count ?? 0,
                    LevelType = level.LevelType,
                    DetailedDescription = level.DetailedDescription,
                    Mode = level.Mode.ToString(),
                    AlphabetJson = level.AlphabetJson,
                    TransformTestsJson = level.TransformTestsJson,
                    CorrectExamplesJson = level.CorrectExamplesJson,
                    WrongExamplesJson = level.WrongExamplesJson,
                    UserRating = UserRating,
                    UserIsSubscribed = IsUserSubscribed(UserId, entity.Id)
                };
            }


            else if (entity.Type.Equals(WorkshopItemType.Machine))
            {
                var machine = db.Machines.AsNoTracking().FirstOrDefault(m => m.WorkshopItemId == entity.Id);
                if (machine == null) return null;

                return new Dtos.MachineWorkshopItem
                {
                    Id = entity.Id,
                    MachineId = machine.Id,
                    Name = entity.Name,
                    Description = entity.Description,
                    Author = authorName,
                    Type = entity.Type.ToString(),
                    Rating = entity.Rating,
                    Subscribers = entity.Subscribers?.Count ?? 0,
                    AlphabetJson = machine.AlphabetJson,
                    NodesJson = machine.NodesJson,
                    ConnectionsJson = machine.ConnectionsJson,
                    UserRating = UserRating,
                    UserIsSubscribed = IsUserSubscribed(UserId, entity.Id)
                };
            }

            return new Dtos.WorkshopItem
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Author = authorName,
                Type = entity.Type.ToString(),
                Rating = entity.Rating,
                Subscribers = entity.Subscribers?.Count ?? 0,
                UserRating = UserRating,
                UserIsSubscribed = IsUserSubscribed(UserId, entity.Id)
            };
        }

        public Dtos.WorkshopItem? AddWorkshopItem(JsonElement json, int UserId)
        {
            if (json.ValueKind != JsonValueKind.Object)
                return null;

            var name = json.GetProperty("name").GetString() ?? "";
            var description = json.GetProperty("description").GetString() ?? "";
            var authorName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == UserId)
                .Select(p => p.Username)
                .FirstOrDefault() ?? "Unknown";
            var type = json.GetProperty("type").GetString() ?? "";

            var authorId = db.Players
                .AsNoTracking()
                .Where(p => p.Username == authorName)
                .Select(p => p.Id)
                .FirstOrDefault();

            var newItem = new Entities.WorkshopItem
            {
                Name = name,
                Description = description,
                Type = Enum.TryParse(type, true, out WorkshopItemType parsedType) ? parsedType : WorkshopItemType.Undefined,
                AuthorId = authorId,
                Rating = 0.0,
                Subscribers = null
            };

            db.WorkshopItems.Add(newItem);
            db.SaveChanges();

            if (type == WorkshopItemType.Level.ToString())
            {
                var level = new Entities.LevelWorkshopItem
                {
                    WorkshopItemId = newItem.Id,
                    LevelType = "Workshop",
                    DetailedDescription = json.TryGetProperty("detailedDescription", out var dd) ? dd.GetString() ?? "" : "",
                    Objective = json.TryGetProperty("objective", out var ob) ? ob.GetString() ?? "" : "",
                    Mode = Enum.TryParse(json.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "accept", true, out LevelMode modeVal) ? modeVal : LevelMode.accept,
                    AlphabetJson = json.TryGetProperty("alphabetJson", out var a) ? a.GetRawText() : "[_]",
                    TransformTestsJson = json.TryGetProperty("transformTestsJson", out var t) ? t.GetRawText() : null,
                    CorrectExamplesJson = json.TryGetProperty("correctExamplesJson", out var c) ? c.GetRawText() : null,
                    WrongExamplesJson = json.TryGetProperty("wrongExamplesJson", out var w) ? w.GetRawText() : null
                };

                db.Levels.Add(level);
                db.SaveChanges();

                return new Dtos.LevelWorkshopItem
                {
                    Id = newItem.Id,
                    LevelId = level.Id,
                    Name = name,
                    Description = description,
                    Author = authorName ?? "Unknown",
                    Type = type,
                    Rating = 0.0,
                    Subscribers = 0,
                    LevelType = level.LevelType,
                    DetailedDescription = level.DetailedDescription,
                    Mode = level.Mode.ToString(),
                    AlphabetJson = level.AlphabetJson,
                    TransformTestsJson = level.TransformTestsJson,
                    CorrectExamplesJson = level.CorrectExamplesJson,
                    WrongExamplesJson = level.WrongExamplesJson
                };
            }
            else if (type == WorkshopItemType.Machine.ToString())
            {
                var machine = new Entities.MachineWorkshopItem
                {
                    WorkshopItemId = newItem.Id,
                    AlphabetJson = json.TryGetProperty("alphabetJson", out var a) ? a.GetRawText() : "[_]",
                    NodesJson = json.TryGetProperty("nodesJson", out var n) ? n.GetRawText() : "[]",
                    ConnectionsJson = json.TryGetProperty("connectionsJson", out var c) ? c.GetRawText() : "[]"
                };

                db.Machines.Add(machine);
                db.SaveChanges();

                return new Dtos.MachineWorkshopItem
                {
                    Id = newItem.Id,
                    MachineId = machine.Id,
                    Name = name,
                    Description = description,
                    Author = authorName ?? "Unknown",
                    Type = type,
                    Rating = 0.0,
                    Subscribers = 0,
                    AlphabetJson = machine.AlphabetJson,
                    NodesJson = machine.NodesJson,
                    ConnectionsJson = machine.ConnectionsJson
                };
            }

            return new Dtos.WorkshopItem
            {
                Id = newItem.Id,
                Name = name,
                Description = description,
                Author = authorName ?? "Unknown",
                Type = type,
                Rating = 0.0,
                Subscribers = 0
            };
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

        public bool DeleteWorkshopItem(int workshopItemId, int userId)
        {
            if (!db.Players.Any(p => p.Id == userId && p.Role == "Admin") &&
                !db.WorkshopItems.Any(wi => wi.Id == workshopItemId && wi.AuthorId == userId))
            {
                return false;
            }

            var workshopItem = db.WorkshopItems.FirstOrDefault(wi => wi.Id == workshopItemId);
            db.WorkshopItems.Remove(workshopItem!);
            db.SaveChanges();
            return true;
        }
    }
}
