using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

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

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.Username),
                new Claim("id", player.Id.ToString()),
                new Claim(ClaimTypes.Role, player.Role ?? "User")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
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
    }
}
