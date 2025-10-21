using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;

namespace TuringMachinesAPI.Services
{
    public class WorkshopItemService
    {
        private readonly TuringMachinesDbContext _db;

        public WorkshopItemService(TuringMachinesDbContext dbContext)
        {
            _db = dbContext;
        }

        public IEnumerable<Dtos.WorkshopItem> GetAll()
        {
            return _db.WorkshopItems
                .AsNoTracking()
                .Select(w => new Dtos.WorkshopItem
                {
                    Id = w.Id,
                    ItemTypeId = w.ItemTypeId,
                    Type = w.Type,
                    Author = _db.Players
                        .Where(p => p.Id == w.AuthorId)
                        .Select(p => new Dtos.Player
                        {
                            Id = p.Id,
                            Username = p.Username,
                            Role = p.Role,
                            Password = null
                        })
                        .FirstOrDefault() ?? new Dtos.Player(),
                    Rating = _db.Reviews
                        .Where(r => r.WorkshopItemId == w.Id)
                        .Select(r => (double?)r.Rating)
                        .Average() ?? 0.0,
                    Subscribers = null
                })
                .ToList();
        }

        public (Dtos.WorkshopItem? item, object? content) GetById(int id)
        {
            var w = _db.WorkshopItems.AsNoTracking().FirstOrDefault(w => w.Id == id);
            if (w == null) return (null, null);

            var author = _db.Players
                .Where(p => p.Id == w.AuthorId)
                .Select(p => new Dtos.Player
                {
                    Id = p.Id,
                    Username = p.Username,
                    Role = p.Role,
                    Password = null
                })
                .FirstOrDefault() ?? new Dtos.Player();

            var itemDto = new Dtos.WorkshopItem
            {
                Id = w.Id,
                ItemTypeId = w.ItemTypeId,
                Type = w.Type,
                Author = author,
                Rating = _db.Reviews
                    .Where(r => r.WorkshopItemId == w.Id)
                    .Select(r => (double?)r.Rating)
                    .Average() ?? 0.0,
                Subscribers = null
            };

            object? content = null;

            if (w.Type == "Level")
            {
                var level = _db.Levels.AsNoTracking().FirstOrDefault(l => l.Id == w.ItemTypeId);
                if (level != null)
                {
                    content = new Dtos.Level
                    {
                        Id = level.Id,
                        Name = level.Name,
                        Description = level.Description,
                        Type = level.Type,
                        LevelData = level.LevelData
                    };
                }
            }
            else if (w.Type == "Machine")
            {
                var machine = _db.Machines.AsNoTracking().FirstOrDefault(m => m.Id == w.ItemTypeId);
                if (machine != null)
                {
                    content = new Dtos.Machine
                    {
                        Id = machine.Id,
                        Name = machine.Name,
                        Alphabet = machine.Alphabet,
                        MachineData = machine.MachineData
                    };
                }
            }

            return (itemDto, content);
        }

        public (Dtos.WorkshopItem item, Dtos.Level level) CreateWorkshopLevel(JsonElement levelJson, int authorId)
        {
            string name = "Untitled";
            string description = "";
            string type = "Workshop";

            try
            {
                if (levelJson.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("name", out var n))
                        name = n.GetString() ?? name;
                    if (data.TryGetProperty("description", out var d))
                        description = d.GetString() ?? description;
                    if (data.TryGetProperty("level_type", out var t))
                        type = t.GetString() ?? type;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to parse level JSON: {ex.Message}");
            }

            var levelEntity = new Entities.Level
            {
                Name = name,
                Description = description,
                Type = type,
                LevelData = levelJson.GetRawText()
            };
            _db.Levels.Add(levelEntity);
            _db.SaveChanges();

            var workshopEntity = new Entities.WorkshopItem
            {
                ItemTypeId = levelEntity.Id,
                AuthorId = authorId,
                Type = "Level",
                Rating = 0.0
            };
            _db.WorkshopItems.Add(workshopEntity);
            _db.SaveChanges();

            var author = _db.Players
                .Where(p => p.Id == authorId)
                .Select(p => new Dtos.Player
                {
                    Id = p.Id,
                    Username = p.Username,
                    Role = p.Role,
                    Password = null
                })
                .FirstOrDefault() ?? new Dtos.Player();

            var itemDto = new Dtos.WorkshopItem
            {
                Id = workshopEntity.Id,
                ItemTypeId = levelEntity.Id,
                Type = "Level",
                Author = author,
                Rating = 0.0
            };

            var levelDto = new Dtos.Level
            {
                Id = levelEntity.Id,
                Name = levelEntity.Name,
                Description = levelEntity.Description,
                Type = levelEntity.Type,
                LevelData = levelEntity.LevelData
            };

            return (itemDto, levelDto);
        }

        public bool AddOrUpdateRating(int workshopId, int userId, int ratingValue)
        {
            var item = _db.WorkshopItems.FirstOrDefault(w => w.Id == workshopId);
            if (item == null) return false;

            var existing = _db.Reviews.FirstOrDefault(r => r.WorkshopItemId == workshopId && r.UserId == userId);
            if (existing != null)
                existing.Rating = ratingValue;
            else
                _db.Reviews.Add(new Entities.Review
                {
                    WorkshopItemId = workshopId,
                    UserId = userId,
                    Rating = ratingValue
                });

            _db.SaveChanges();

            var avg = _db.Reviews
                .Where(r => r.WorkshopItemId == workshopId)
                .Select(r => (double?)r.Rating)
                .Average() ?? 0.0;

            item.Rating = avg;
            _db.SaveChanges();

            return true;
        }
        public bool ToggleSubscription(int workshopId, int playerId)
        {
            var item = _db.WorkshopItems.FirstOrDefault(w => w.Id == workshopId);
            if (item == null) return false;

            if (item.Subscribers.Contains(playerId))
                item.Subscribers.Remove(playerId);
            else
                item.Subscribers.Add(playerId);

            _db.SaveChanges();
            return true;
        }
    }
}
