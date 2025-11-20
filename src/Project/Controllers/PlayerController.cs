using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("players")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayerService _playerService;
        private readonly DiscordWebhookService discordWebhookService;

        public PlayersController(PlayerService playerService, DiscordWebhookService discordWebhookService)
        {
            this._playerService = playerService;
            this.discordWebhookService = discordWebhookService;
        }

        /// <summary>
        /// Get all registered players.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<Player>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetAllPlayers()
        {
            var players = _playerService.GetAllPlayers();
            return Ok(_playerService.NonSensitivePlayers(players));
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Player), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetPlayerById(int id)
        {
            var player = _playerService.GetPlayerById(id);
            if (player is null)
                return NotFound($"Player with ID {id} not found.");
            return Ok(_playerService.NonSensitivePlayer(player));
        }

        /// <summary>
        /// Add a new player.
        /// Username must be unique.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Player), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPlayer([FromBody] Player player)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = _playerService.AddPlayer(player);
            if (created is null)
            {
                if (_playerService.GetAllPlayers().Any(p => p.Username == player.Username))
                    return Conflict(new { message = "Username already exists." });

                return BadRequest(new { message = "Invalid player data or missing password." });
            }

            await discordWebhookService.NotifyNewPlayerAsync(created.Username);

            return CreatedAtAction(nameof(GetPlayerById), new { id = created.Id }, _playerService.NonSensitivePlayer(created));
        }

        /// <summary>
        /// Login a player and receive a JWT token.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] Player credentials)
        {
            if (credentials == null ||
                string.IsNullOrEmpty(credentials.Username) ||
                string.IsNullOrEmpty(credentials.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            var player = _playerService.Authenticate(credentials.Username, credentials.Password);
            if (player == null)
                return Unauthorized(new { message = "Invalid username or password." });

            var token = _playerService.GenerateJwtToken(player);
            return Ok(new
            {
                token,
                user = _playerService.NonSensitivePlayer(player)
            });
        }

        /// <summary>
        ///  Verify the JWT token and userinfo, return user info.
        /// </summary>
        /// <returns> User Info </returns>
        [Authorize]
        [HttpGet("verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Verify()
        {
            var (id, username, role) = _playerService.GetClaimsFromUser(User);
            if (_playerService.PlayerExistsAsIs(id!, username!, role!) is false)
            {
                return Unauthorized(new { valid = false, message = "Claims do not match the database" });
            }
            return Ok(new
            {
                valid = true,
                user = new { id, username, role }
            });
        }

        /// <summary>
        /// Delete a player by ID.
        /// </summary>
        /// <param name="id">The ID of the player to delete.</param>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult DeletePlayer(int id)
        {
            int playerId = int.Parse(_playerService.GetClaimsFromUser(User).Id ?? "-1");
            if (!_playerService.IsAdmin(playerId) && playerId != id)
                return Forbid("You do not have permission to delete this player.");

            var success = _playerService.DeletePlayer(id);
            if (!success)
                return NotFound($"Player with ID {id} not found.");

            return Ok(new { message = $"Player with ID {id} has been deleted." });
        }
    }
}
