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
        [ProducesResponseType(typeof(IEnumerable<Player>), StatusCodes.Status200OK)]
        public IActionResult GetAllPlayers()
        {
            var players = _playerService.GetAllPlayers();
            return Ok(_playerService.NonSensitivePlayers(players));
        }

        /// <summary>
        /// Get a player by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(Player), StatusCodes.Status200OK)]
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
        public IActionResult Verify()
        {
            var (id, username, role) = _playerService.GetClaimsFromUser(User);
            return Ok(new
            {
                valid = true,
                user = new { id, username, role }
            });
        }
    }
}
