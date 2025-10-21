using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;

namespace TuringMachinesAPI.Services
{
    
    public class LevelService
    {
        private readonly TuringMachinesDbContext _db;

        public LevelService(TuringMachinesDbContext dbContext)
        {
            _db = dbContext;
        }

        /// <summary>
        /// Obtém todos os níveis disponíveis, com filtro opcional por nome.
        /// </summary>
        public IEnumerable<Dtos.Level> GetAllLevels(string? nameFilter = null)
        {
            IQueryable<Entities.Level> query = _db.Levels.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                string lowered = nameFilter.Trim().ToLower();
                query = query.Where(l => l.Name.ToLower().Contains(lowered));
            }

            return query
                .OrderByDescending(l => l.Id)
                .Select(l => new Dtos.Level
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    Type = l.Type,
                    LevelData = l.LevelData
                })
                .ToList();
        }

        /// <summary>
        /// Obtém um nível específico pelo ID.
        /// </summary>
        public Dtos.Level? GetLevelById(int id)
        {
            var entity = _db.Levels.AsNoTracking().FirstOrDefault(l => l.Id == id);
            if (entity is null) return null;

            return new Dtos.Level
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Type = entity.Type,
                LevelData = entity.LevelData
            };
        }

        /// <summary>
        /// Adiciona um novo nível à base de dados.
        /// Espera uma string JSON representando o nível.
        /// O método analisa o JSON para extrair metadados como name, description e type.
        /// </summary>
        public Dtos.Level AddLevel(string LevelData)
        {
            if (string.IsNullOrWhiteSpace(LevelData))
                throw new ArgumentException("LevelData cannot be empty.");

            string name = "Untitled";
            string description = "";
            string type = "Workshop";

            try
            {
                using var doc = JsonDocument.Parse(LevelData);
                var root = doc.RootElement;

                // Get the nested "data" object (Python wrapper)
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? name;

                    if (data.TryGetProperty("description", out var descProp))
                        description = descProp.GetString() ?? description;

                    if (data.TryGetProperty("level_type", out var typeProp))
                        type = typeProp.GetString() ?? type;
                    else if (data.TryGetProperty("type", out var typeAlt))
                        type = typeAlt.GetString() ?? type;
                }
                else
                {
                    // fallback if "data" key not present (e.g., raw level JSON)
                    if (root.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString() ?? name;

                    if (root.TryGetProperty("description", out var descProp))
                        description = descProp.GetString() ?? description;

                    if (root.TryGetProperty("level_type", out var typeProp))
                        type = typeProp.GetString() ?? type;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[WARN] Failed to parse LevelData JSON: {ex.Message}");
            }

            var entity = new Entities.Level
            {
                Name = name,
                Description = description,
                Type = type,
                LevelData = LevelData
            };

            _db.Levels.Add(entity);
            _db.SaveChanges();

            return new Dtos.Level
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Type = entity.Type,
                LevelData = entity.LevelData
            };
        }
    }
}
