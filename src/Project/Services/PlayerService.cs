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
        private readonly ICryptoService CryptoService;
        private readonly IConfiguration _config;

        public PlayerService(TuringMachinesDbContext dbContext, ICryptoService _CryptoService, IConfiguration config)
        {
            db = dbContext;
            CryptoService = _CryptoService;
            _config = config;
        }

        /// <summary>
        /// Get all players.
        /// </summary>
        public IEnumerable<Dtos.Player> GetAllPlayers()
        {
            return db.Players
                .Select(p => new Dtos.Player
                {
                    Id = p.Id,
                    Username = p.Username,
                    Password = CryptoService.Decrypt(p.Password),
                    Role = p.Role
                })
                .ToList();
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        public Dtos.Player? GetPlayerById(int id)
        {
            var entity = db.Players.FirstOrDefault(p => p.Id == id);
            if (entity is null) return null;
            return new Dtos.Player
            {
                Id = entity.Id,
                Username = entity.Username,
                Password = CryptoService.Decrypt(entity.Password),
                Role = entity.Role
            };
        }

        /// <summary>
        /// Add a player, unique username enforced.
        /// </summary>
        public Dtos.Player? AddPlayer(Dtos.Player player)
        {
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
                Password = CryptoService.Encrypt(player.Password)
            };
            db.Players.Add(entity);
            db.SaveChanges();
            player.Id = entity.Id;
            return player;
        }

        public Player? Authenticate(string username, string password)
        {
            return GetAllPlayers()
                .FirstOrDefault(p => p.Username == username && p.Password == password);
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
                Role = SensitivePlayer.Role

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
            return true;
        }
    }
}
