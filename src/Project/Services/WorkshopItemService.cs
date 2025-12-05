using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Utils;

namespace TuringMachinesAPI.Services
{
    public class WorkshopItemService
    {
        private readonly TuringMachinesDbContext db;
        private readonly IMemoryCache cache;

        public WorkshopItemService(TuringMachinesDbContext context, IMemoryCache memoryCache)
        {
            db = context;
            cache = memoryCache;
        }

        public IEnumerable<object> GetAll(string? NameFilter, int UserId)
        {
            if (cache.TryGetValue("LastPlayerGetId", out int? cachedLastGetId) && cachedLastGetId == UserId && cache.TryGetValue("WorkshopItems", out List<object>? cachedItemsCustom))
            {
                IEnumerable<object> itemsToReturn = cachedItemsCustom!;
                if (!string.IsNullOrWhiteSpace(NameFilter))
                    itemsToReturn = itemsToReturn.Where(x =>
                        x is Dtos.WorkshopItem w && w.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase) ||
                        x is Dtos.LevelWorkshopItem lw && lw.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase) ||
                        x is Dtos.MachineWorkshopItem mw && mw.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase));
                return itemsToReturn;
            }

            var mySubs = db.WorkshopItems
                .Where(w => w.Subscribers != null && w.Subscribers.Contains(UserId))
                .Select(w => w.Id)
                .ToHashSet();

            var myRatings = db.Reviews
                .Where(r => r.UserId == UserId)
                .ToDictionary(r => r.WorkshopItemId, r => r.Rating);

            if (cache.TryGetValue("WorkshopItems", out List<object>? cachedItems))
            {
                IEnumerable<object> itemsToReturn = cachedItems!;

                if (!string.IsNullOrWhiteSpace(NameFilter))
                    itemsToReturn = itemsToReturn.Where(x =>
                        x is Dtos.WorkshopItem w && w.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase) ||
                        x is Dtos.LevelWorkshopItem lw && lw.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase) ||
                        x is Dtos.MachineWorkshopItem mw && mw.Name.Contains(NameFilter, StringComparison.OrdinalIgnoreCase));

                foreach (var item in itemsToReturn)
                {
                    if (item is Dtos.LevelWorkshopItem lw)
                    {
                        lw.UserIsSubscribed = mySubs.Contains(lw.Id);
                        lw.UserRating = myRatings.GetValueOrDefault(lw.Id);
                    }
                    else if (item is Dtos.MachineWorkshopItem mw)
                    {
                        mw.UserIsSubscribed = mySubs.Contains(mw.Id);
                        mw.UserRating = myRatings.GetValueOrDefault(mw.Id);
                    }
                    else if (item is Dtos.WorkshopItem w)
                    {
                        w.UserIsSubscribed = mySubs.Contains(w.Id);
                        w.UserRating = myRatings.GetValueOrDefault(w.Id);
                    }
                }

                cache.Set("LastPlayerGetId", UserId);
                return itemsToReturn;
            }

            var query = db.WorkshopItems.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(NameFilter))
                query = query.Where(wi => wi.Name.ToLower().Contains(NameFilter.ToLower()));

            var items = query.ToList();
            var results = new List<object>(items.Count);

            var levelData = db.Levels.AsNoTracking()
                .ToDictionary(l => l.WorkshopItemId, l => l);

            var machineData = db.Machines.AsNoTracking()
                .ToDictionary(m => m.WorkshopItemId, m => m);

            var authors = db.Players.AsNoTracking()
                .ToDictionary(x => x.Id, x => x.Username);

            foreach (var i in items)
            {
                Dtos.Player? cachedAuthor = CacheHelper.GetPlayerFromCacheById(cache, i.AuthorId);
                string authorName = authors.GetValueOrDefault(i.AuthorId, "Unknown");

                var userRating = myRatings.GetValueOrDefault(i.Id);
                var userSubscribed = mySubs.Contains(i.Id);

                if (i.Type.Equals(WorkshopItemType.Level))
                {
                    if (levelData.TryGetValue(i.Id, out var level))
                    {
                        results.Add(new Dtos.LevelWorkshopItem
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
                            UserRating = userRating,
                            UserIsSubscribed = userSubscribed,
                            TwoTapes = level.TwoTapes
                        });
                        continue;
                    }
                }
                else if (i.Type.Equals(WorkshopItemType.Machine))
                {
                    if (machineData.TryGetValue(i.Id, out var machine))
                    {
                        results.Add(new Dtos.MachineWorkshopItem
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
                            UserRating = userRating,
                            UserIsSubscribed = userSubscribed
                        });
                        continue;
                    }
                }

                results.Add(new Dtos.WorkshopItem
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    Author = authorName,
                    Type = i.Type.ToString(),
                    Rating = i.Rating,
                    Subscribers = i.Subscribers?.Count ?? 0,
                    UserRating = userRating,
                    UserIsSubscribed = userSubscribed
                });
            }

            cache.Set("WorkshopItems", results);
            cache.Set("LastPlayerGetId", UserId);

            return results;
        }


        public object? GetById(int id, int UserId)
        {
            var allItems = GetAll(null, UserId);

            Dtos.Player? cachedAuthor = CacheHelper.GetPlayerFromCacheById(cache, UserId);
            string authorName;
            if (cachedAuthor != null)
            {
                authorName = cachedAuthor.Username;
            }
            else
            {
                authorName = db.Players
                    .AsNoTracking()
                    .Where(p => p.Id == UserId)
                    .Select(p => p.Username)
                    .FirstOrDefault() ?? "Unknown";
            }

            return allItems.FirstOrDefault(item =>
            {
                if (item is Dtos.WorkshopItem w) return w.Author.Equals(authorName);
                if (item is Dtos.LevelWorkshopItem lw) return lw.Author.Equals(authorName);
                if (item is Dtos.MachineWorkshopItem mw) return mw.Author.Equals(authorName);
                return false;
            });

        }

        public Dtos.WorkshopItem? AddWorkshopItem(JsonElement json, int UserId)
        {
            if (json.ValueKind != JsonValueKind.Object)
                return null;

            var name = json.GetProperty("name").GetString() ?? "";
            var description = json.GetProperty("description").GetString() ?? "";

            Dtos.Player? cachedAuthor = CacheHelper.GetPlayerFromCacheById(cache, UserId);
            string authorName;
            if (cachedAuthor != null)
            {
                authorName = cachedAuthor.Username;
            }
            else
            {
                authorName = db.Players
                    .AsNoTracking()
                    .Where(p => p.Id == UserId)
                    .Select(p => p.Username)
                    .FirstOrDefault() ?? "Unknown";
            }

            var type = json.GetProperty("type").GetString() ?? "";

            if (ValidationUtils.ContainsDisallowedContent(name) ||
            ValidationUtils.ContainsDisallowedContent(description) ||
            name.Length > 100 ||
            description.Length > 250)
            {
                Console.WriteLine("[AddWorkshopItem] Invalid name or description content.");
                return null;
            }

            if (Enum.TryParse(type, true, out WorkshopItemType parsedTypeVerify))
            {
                if (cache.TryGetValue("WorkshopItems", out IEnumerable<object>? cachedItems))
                {
                    bool cachedNameExists = cachedItems!.Any(item =>
                    {
                        if (item is Dtos.WorkshopItem w)
                            return w.Name.ToLower() == name.ToLower() && w.Type.Equals(type, StringComparison.OrdinalIgnoreCase);
                        if (item is Dtos.LevelWorkshopItem lw)
                            return lw.Name.ToLower() == name.ToLower() && lw.Type.Equals(type, StringComparison.OrdinalIgnoreCase);
                        if (item is Dtos.MachineWorkshopItem mw)
                            return mw.Name.ToLower() == name.ToLower() && mw.Type.Equals(type, StringComparison.OrdinalIgnoreCase);
                        return false;
                    });
                    if (cachedNameExists)
                    {
                        Console.WriteLine("[AddWorkshopItem] Workshop item with the same name and type already exists in cache.");
                        return null;
                    }
                }
                else
                {
                    bool nameExists = db.WorkshopItems
                        .AsNoTracking()
                        .Any(wi => wi.Name.ToLower() == name.ToLower() &&
                                   wi.Type == parsedTypeVerify);
                    if (nameExists)
                    {
                        Console.WriteLine("[AddWorkshopItem] Workshop item with the same name and type already exists.");
                        return null;
                    }
                }
            }
            else
            {
                Console.WriteLine("[AddWorkshopItem] Invalid type.");
                return null;
            }

            var newItem = new Entities.WorkshopItem
            {
                Name = name,
                Description = description,
                Type = Enum.TryParse(type, true, out WorkshopItemType parsedType) ? parsedType : WorkshopItemType.Undefined,
                AuthorId = UserId,
                Rating = 0.0,
                Subscribers = null
            };


            if (type == WorkshopItemType.Level.ToString())
            {
                var alphabetJson = json.TryGetProperty("alphabetJson", out var a) ? a.GetRawText() : "[_]";
                string? DetailedDescription = json.TryGetProperty("detailedDescription", out var ddstr) ? ddstr.GetString() : null;
                string? Objective = json.TryGetProperty("objective", out var obStr) ? obStr.GetString() : null;
                string? Mode = json.TryGetProperty("mode", out var modePropstr) ? modePropstr.GetString() : null;

                var validAlphabet = ValidationUtils.IsValidJson(alphabetJson) && alphabetJson.Length <= 100;
                var validDetailedDescription = DetailedDescription == null || (DetailedDescription.Length <= 1000 && !ValidationUtils.ContainsDisallowedContent(DetailedDescription));
                var validObjective = Objective == null || (Objective.Length <= 500 && !ValidationUtils.ContainsDisallowedContent(Objective));
                var validMode = Mode == null || Mode.ToLower() == "accept" || Mode.ToLower() == "transform";


                if (!(validAlphabet && validDetailedDescription && validObjective && validMode))
                {
                    Console.WriteLine("[AddWorkshopItem] Invalid level specific properties.");
                    return null;
                }
                db.WorkshopItems.Add(newItem);
                db.SaveChanges();


                var level = new Entities.LevelWorkshopItem
                {
                    WorkshopItemId = newItem.Id,
                    LevelType = "Workshop",
                    DetailedDescription = json.TryGetProperty("detailedDescription", out var dd) ? dd.GetString() ?? "" : "",
                    Objective = json.TryGetProperty("objective", out var ob) ? ob.GetString() ?? "" : "",
                    Mode = Enum.TryParse(json.TryGetProperty("mode", out var modeProp) ? modeProp.GetString() : "accept", true, out LevelMode modeVal) ? modeVal : LevelMode.accept,
                    AlphabetJson = alphabetJson,
                    TransformTestsJson = json.TryGetProperty("transformTestsJson", out var t) ? t.GetRawText() : null,
                    CorrectExamplesJson = json.TryGetProperty("correctExamplesJson", out var c) ? c.GetRawText() : null,
                    WrongExamplesJson = json.TryGetProperty("wrongExamplesJson", out var w) ? w.GetRawText() : null,
                    TwoTapes = json.TryGetProperty("twoTapes", out var tt) ? tt.GetBoolean() : false
                };

                db.Levels.Add(level);
                db.SaveChanges();

                Dtos.LevelWorkshopItem newLevel = new Dtos.LevelWorkshopItem
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
                    Objective = level.Objective,
                    Mode = level.Mode.ToString(),
                    AlphabetJson = level.AlphabetJson,
                    TransformTestsJson = level.TransformTestsJson,
                    CorrectExamplesJson = level.CorrectExamplesJson,
                    WrongExamplesJson = level.WrongExamplesJson,
                    TwoTapes = level.TwoTapes
                };

                if (cache.TryGetValue("WorkshopItems", out IEnumerable<object>? cachedItems))
                {
                    var updatedItems = cachedItems!.ToList();
                    updatedItems.Add(newLevel);
                    cache.Set("WorkshopItems", updatedItems);
                }

                return newLevel;
            }
            else if (type == WorkshopItemType.Machine.ToString())
            {
                var validAlphabet = ValidationUtils.IsValidJson(
                    json.TryGetProperty("alphabetJson", out var aStr) ? aStr.GetRawText() : "[_]");
                var validNodes = ValidationUtils.IsValidJson(
                    json.TryGetProperty("nodesJson", out var nStr) ? nStr.GetRawText() : "[]");
                var validConnections = ValidationUtils.IsValidJson(
                    json.TryGetProperty("connectionsJson", out var cStr) ? cStr.GetRawText() : "[]");
                if (!(validAlphabet && validNodes && validConnections))
                {
                    Console.WriteLine("[AddWorkshopItem] Invalid machine specific properties.");
                    return null;
                }
                db.WorkshopItems.Add(newItem);
                db.SaveChanges();

                var machine = new Entities.MachineWorkshopItem
                {
                    WorkshopItemId = newItem.Id,
                    AlphabetJson = json.TryGetProperty("alphabetJson", out var a) ? a.GetRawText() : "[_]",
                    NodesJson = json.TryGetProperty("nodesJson", out var n) ? n.GetRawText() : "[]",
                    ConnectionsJson = json.TryGetProperty("connectionsJson", out var c) ? c.GetRawText() : "[]"
                };

                db.Machines.Add(machine);
                db.SaveChanges();

                Dtos.MachineWorkshopItem newMachine = new Dtos.MachineWorkshopItem
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

                if (cache.TryGetValue("WorkshopItems", out IEnumerable<object>? cachedItems))
                {
                    var updatedItems = cachedItems!.ToList();
                    updatedItems.Add(newMachine);
                    cache.Set("WorkshopItems", updatedItems);
                }
                return newMachine;
            }

            Console.WriteLine("[AddWorkshopItem] Unsupported workshop item type.");
            return null;
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
            if (cache.TryGetValue("WorkshopItems", out List<object>? cachedItems))
            {
                var updated = cachedItems!
                    .Select(x =>
                    {
                        if (x is Dtos.WorkshopItem w && w.Id == ItemId)
                        {
                            w.Rating = WorkShopItem.Rating;
                            return w;
                        }
                        if (x is Dtos.LevelWorkshopItem lw && lw.Id == ItemId)
                        {
                            lw.Rating = WorkShopItem.Rating;
                            return lw;
                        }
                        if (x is Dtos.MachineWorkshopItem mw && mw.Id == ItemId)
                        {
                            mw.Rating = WorkShopItem.Rating;
                            return mw;
                        }
                        return x;
                    })
                    .ToList();
                cache.Set("WorkshopItems", updated);
                cache.Set("LastPlayerGetId", userId);
            }

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
                if (cache.TryGetValue("WorkshopItems", out List<object>? cachedItems))
                {
                    var updated = cachedItems!
                        .Select(x =>
                        {
                            if (x is Dtos.WorkshopItem w && w.Id == workshopItemId)
                            {
                                w.Subscribers += 1;
                                w.UserIsSubscribed = true;
                                return w;
                            }
                            if (x is Dtos.LevelWorkshopItem lw && lw.Id == workshopItemId)
                            {
                                lw.Subscribers += 1;
                                lw.UserIsSubscribed = true;
                                return lw;
                            }
                            if (x is Dtos.MachineWorkshopItem mw && mw.Id == workshopItemId)
                            {
                                mw.Subscribers += 1;
                                mw.UserIsSubscribed = true;
                                return mw;
                            }
                            return x;
                        })
                        .ToList();
                    cache.Set("WorkshopItems", updated);
                    cache.Set("LastPlayerGetId", userId);
                }
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

                if (cache.TryGetValue("WorkshopItems", out List<object>? cachedItems))
                {
                    var updated = cachedItems!
                        .Select(x =>
                        {
                            if (x is Dtos.WorkshopItem w && w.Id == workshopItemId)
                            {
                                w.Subscribers = Math.Max(0, w.Subscribers - 1);
                                w.UserIsSubscribed = false;
                                return w;
                            }
                            if (x is Dtos.LevelWorkshopItem lw && lw.Id == workshopItemId)
                            {
                                lw.Subscribers = Math.Max(0, lw.Subscribers - 1);
                                lw.UserIsSubscribed = false;
                                return lw;
                            }
                            if (x is Dtos.MachineWorkshopItem mw && mw.Id == workshopItemId)
                            {
                                mw.Subscribers = Math.Max(0, mw.Subscribers - 1);
                                mw.UserIsSubscribed = false;
                                return mw;
                            }
                            return x;
                        })
                        .ToList();
                    cache.Set("WorkshopItems", updated);
                    cache.Set("LastPlayerGetId", userId);
                }
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
            if (workshopItem == null)
            {
                return false;
            }
            if (workshopItem.Type.Equals(WorkshopItemType.Level))
            {
                var level = db.Levels.FirstOrDefault(l => l.WorkshopItemId == workshopItemId);
                if (level != null)
                {
                    db.Levels.Remove(level);
                }
            }
            else if (workshopItem.Type.Equals(WorkshopItemType.Machine))
            {
                var machine = db.Machines.FirstOrDefault(m => m.WorkshopItemId == workshopItemId);
                if (machine != null)
                {
                    db.Machines.Remove(machine);
                }
            }
            var reviews = db.Reviews.Where(r => r.WorkshopItemId == workshopItemId).ToList();
            db.Reviews.RemoveRange(reviews);
            db.WorkshopItems.Remove(workshopItem!);
            db.SaveChanges();

            if (cache.TryGetValue("WorkshopItems", out List<object>? cachedItems))
            {
                var updated = cachedItems!
                    .Where(x =>
                        (x is Dtos.WorkshopItem w && w.Id != workshopItemId) ||
                        (x is Dtos.LevelWorkshopItem lw && lw.Id != workshopItemId) ||
                        (x is Dtos.MachineWorkshopItem mw && mw.Id != workshopItemId)
                    )
                    .ToList();

                cache.Set("WorkshopItems", updated);
                cache.Remove("LastPlayerGetId");
            }
            return true;
        }
    }
}
