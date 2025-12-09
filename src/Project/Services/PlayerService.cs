using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;

namespace TuringMachinesAPI.Services
{
    public class PlayerService
    {
        private readonly TuringMachinesDbContext db;
        private readonly PasswordHashService PasswordService;
        private readonly IConfiguration _config;
        private readonly IMemoryCache cache;

        public PlayerService(TuringMachinesDbContext dbContext, PasswordHashService _PasswordService, IConfiguration config, IMemoryCache memoryCache)
        {
            db = dbContext;
            PasswordService = _PasswordService;
            _config = config;
            cache = memoryCache;
        }

        /// <summary>
        /// Get all players.
        /// </summary>
        public IEnumerable<Dtos.Player> GetAllPlayers()
        {
            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                return cachedPlayers ?? Enumerable.Empty<Dtos.Player>();
            }

            var players = db.Players
                .AsNoTracking()
                .Select(p => new Dtos.Player
                {
                    Id = p.Id,
                    Username = p.Username,
                    Password = p.Password,
                    Role = p.Role,
                    CreatedAt = p.CreatedAt,
                    LastLogin = p.LastLogin,
                    BannedUntil = p.BannedUntil,
                    BanReason = p.BanReason
                })
                .ToList();

            cache.Set("Players", players);
            return players;
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public Dtos.Player? GetPlayerById(int id)
        {
            Dtos.Player? cachedPlayer = CacheHelper.GetPlayerFromCacheById(cache, id);
            if (cachedPlayer != null)
            {
                return cachedPlayer;
            }

            var players = GetAllPlayers();
            return players.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Add a player, unique username enforced.
        /// </summary>
        public Dtos.Player? AddPlayer(Dtos.Player player)
        {
            if (CacheHelper.GetPlayerFromCacheByUsername(cache, player.Username) is not null)
            {
                return null; 
            }

            if (db.Players.Any(p => p.Username == player.Username))
            {
                return null; 
            }

            if (ValidationUtils.ContainsDisallowedContent(player.Username))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(player.Password))
            {
                return null;
            }
            var entity = new Entities.Player
            {
                Username = player.Username,
                Password = PasswordService.Hash(player.Password),
                CreatedAt = DateTime.UtcNow,
                LastLogin = null
            };
            db.Players.Add(entity);
            db.SaveChanges();
            player.Id = entity.Id;

            Dtos.Player newPlayer = new Dtos.Player
            {
                Id = entity.Id,
                Username = entity.Username,
                Password = entity.Password,
                Role = entity.Role,
                CreatedAt = entity.CreatedAt,
                LastLogin = entity.LastLogin
            };

            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                var updatedPlayers = cachedPlayers?.ToList() ?? new List<Dtos.Player>();
                updatedPlayers.Add(newPlayer);
                cache.Set("Players", updatedPlayers);
            }
            else
            {
                cache.Set("Players", new List<Dtos.Player> { newPlayer });
            }

            return newPlayer;

        }

        public Player? Authenticate(string username, string password)
        {
            var Player = GetAllPlayers()
                .FirstOrDefault(p => p.Username == username && PasswordService.Verify(password, p.Password));

            if (Player is not null)
            {
                var entity = db.Players.First(p => p.Id == Player.Id);
                entity.LastLogin = DateTime.UtcNow;
                db.SaveChanges();
                cache.Remove("Players");
                return new Player
                {
                    Id = entity.Id,
                    Username = entity.Username,
                    Role = entity.Role,
                    CreatedAt = entity.CreatedAt,
                    LastLogin = entity.LastLogin,
                };
            }
            return Player;
        }

        public string GenerateJwtToken(Player player)
        {
            byte[] keyBytes = _config.GetRequiredSection("Jwt:Key").Get<byte[]>()!;
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            int expireHours = _config.GetValue<int?>("Jwt:ExpireHours") ?? 4;

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.Username),
                new Claim("id", player.Id.ToString()),
                new Claim(ClaimTypes.Role, player.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public (string? Id, string? Username, string? Role) GetClaimsFromUser(ClaimsPrincipal user)
        {
            var username = user.Identity?.Name
                           ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            var id = user.FindFirst("id")?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            return (id, username, role);
        }

        public Dtos.Player NonSensitivePlayer(Player SensitivePlayer)
        {
            return new Dtos.Player
            {
                Id = SensitivePlayer.Id,
                Username = SensitivePlayer.Username,
                Password = "",
                Role = SensitivePlayer.Role,
                CreatedAt = SensitivePlayer.CreatedAt,
                LastLogin = SensitivePlayer.LastLogin,
                BannedUntil = SensitivePlayer.BannedUntil,
                BanReason = SensitivePlayer.BanReason
            };
        }

        public IEnumerable<Dtos.Player> NonSensitivePlayers(IEnumerable<Player> SensitivePlayers)
        {
            List<Dtos.Player> NonSensitivePlayers = new List<Dtos.Player>();
            foreach (var player in SensitivePlayers)
            {
                NonSensitivePlayers.Add(NonSensitivePlayer(player));
            }
            return NonSensitivePlayers;
        }

        public bool IsAdmin(int playerId)
        {
            var player = GetPlayerById(playerId);
            return player?.Role == "Admin";
        }

        public bool DeletePlayer(int playerId)
        {
            var player = db.Players.FirstOrDefault(p => p.Id == playerId);

            if (player is not null)
            {
                var workshopItems = db.WorkshopItems
                    .Where(wi => wi.Subscribers != null && wi.Subscribers.Any(s => s == playerId))
                    .ToList();

                foreach (var workshopItem in workshopItems)
                {
                    workshopItem.Subscribers!.Remove(playerId);
                }
                var reviews = db.Reviews.Where(r => r.UserId == playerId).ToList();
                foreach (var review in reviews)
                    {
                    var item = db.WorkshopItems.FirstOrDefault(wi => wi.Id == review.WorkshopItemId);
                    if (item is null) continue;
                    var otherRatings = db.Reviews
                        .Where(r => r.WorkshopItemId == item.Id && r.UserId != playerId)
                        .Select(r => r.Rating)
                        .ToList();

                    if (otherRatings.Count == 0)
                    {
                        item.Rating = 0;
                    }
                    else
                    {
                        item.Rating = (int)Math.Round(otherRatings.Average());
                    }
                }
                db.Reviews.RemoveRange(reviews);
            }

            if (player is null) return false;
            db.Players.Remove(player);
            db.SaveChanges();

            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                var updatedPlayers = cachedPlayers?.Where(p => p.Id != playerId).ToList() ?? new List<Dtos.Player>();
                cache.Set("Players", updatedPlayers);
            }

            cache.Remove("Lobbies");
            cache.Remove("WorkshopItems");
            cache.Remove("Leaderboard");

            return true;
        }

        public bool PlayerExistsAsIs(string playerId, string username, string role)
        {
            int id = int.TryParse(playerId, out var parsedId) ? parsedId : -1;
            var player = GetPlayerById(id);
            return player is not null &&
                   player.Username == username &&
                   player.Role == role;
        }

        public Dtos.Player? BanPlayer(int id, DateTime? until, string reason)
        {
            var player = db.Players.FirstOrDefault(p => p.Id == id);
            if (player == null) return null;
            player.BannedUntil = until;
            player.BanReason = reason;
            db.SaveChanges();
            Dtos.Player? playerDto = new Dtos.Player
            {
                Id = player.Id,
                Username = player.Username,
                Role = player.Role,
                CreatedAt = player.CreatedAt,
                LastLogin = player.LastLogin,
                BannedUntil = player.BannedUntil,
                BanReason = player.BanReason
            };

            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                var updatedPlayers = cachedPlayers?.ToList() ?? new List<Dtos.Player>();
                var playerToUpdate = updatedPlayers.FirstOrDefault(p => p.Id == id);
                if (playerToUpdate != null)
                {
                    playerToUpdate.BannedUntil = until;
                    playerToUpdate.BanReason = reason;
                    cache.Set("Players", updatedPlayers);
                }
            }
            return playerDto;
        }

        public Dtos.Player? UnbanPlayer(int id)
        {
            var player = db.Players.FirstOrDefault(p => p.Id == id);
            if (player == null) return null;

            player.BannedUntil = null;
            player.BanReason = null;
            db.SaveChanges();

            Dtos.Player? playerDto = new Dtos.Player
            {
                Id = player.Id,
                Username = player.Username,
                Role = player.Role,
                CreatedAt = player.CreatedAt,
                LastLogin = player.LastLogin,
            };

            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                var updatedPlayers = cachedPlayers?.ToList() ?? new List<Dtos.Player>();
                var playerToUpdate = updatedPlayers.FirstOrDefault(p => p.Id == id);
                if (playerToUpdate != null)
                {
                    playerToUpdate.BannedUntil = null;
                    playerToUpdate.BanReason = null;
                    cache.Set("Players", updatedPlayers);
                }
            }

            return playerDto;

        }

        public bool IsPlayerBanned(out string? banReason, out DateTime? bannedUntil, string? username = null, int? playerId = null)
        {
            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers) && cachedPlayers.Any())
            {
                var cachedPlayer = cachedPlayers.FirstOrDefault(p =>
                    (username != null && p.Username == username) ||
                    (playerId != null && p.Id == playerId));

                if (cachedPlayer != null)
                {
                    banReason = cachedPlayer.BanReason;
                    bannedUntil = cachedPlayer.BannedUntil;
                    bool isBanned = (cachedPlayer.BannedUntil != null && cachedPlayer.BannedUntil > DateTime.UtcNow) ||
                                    (cachedPlayer.BannedUntil == null && !string.IsNullOrEmpty(cachedPlayer.BanReason));
                    if (cachedPlayer.BannedUntil != null && cachedPlayer.BannedUntil <= DateTime.UtcNow)
                    {
                        cachedPlayer.BanReason = null;
                        cachedPlayer.BannedUntil = null;
                        var playerEntity = db.Players.FirstOrDefault(p =>
                            (username != null && p.Username == username) ||
                            (playerId != null && p.Id == playerId));
                        if (playerEntity != null)
                        {
                            playerEntity.BanReason = null;
                            playerEntity.BannedUntil = null;
                            db.SaveChanges();
                        }

                        var updated = cachedPlayers?.ToList();
                        var cachePlayer = updated?.FirstOrDefault(p => p.Id == cachedPlayer.Id);
                        if (cachePlayer != null)
                        {
                            cachePlayer.BanReason = null;
                            cachePlayer.BannedUntil = null;
                            cache.Set("Players", updated);
                        }
                        return false;
                    }

                    return isBanned;
                }
            }

            var player = db.Players.FirstOrDefault(p =>
                (username != null && p.Username == username) ||
                (playerId != null && p.Id == playerId));

            banReason = null;
            bannedUntil = null;

            if (player != null)
            {
                banReason = player.BanReason;
                bannedUntil = player.BannedUntil;
                if (player.BannedUntil != null && player.BannedUntil <= DateTime.UtcNow)
                {
                    player.BanReason = null;
                    player.BannedUntil = null;
                    db.SaveChanges();
                    return false;
                }

                return player.IsBanned;
            }
            return false;
        }
    }
}
