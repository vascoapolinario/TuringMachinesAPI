using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;
using Microsoft.AspNetCore.SignalR;
using TuringMachinesAPI.Hubs;
using System.Threading.Tasks;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("lobbies")]
    public class LobbyController : ControllerBase
    {
        private readonly LobbyService _service;
        private readonly IHubContext<LobbyHub> _hub;
        private readonly DiscordWebhookService _discordWebService;
        private readonly AdminLogService _adminLogService;

        public LobbyController(LobbyService service, IHubContext<LobbyHub> hub, DiscordWebhookService discordWebService, AdminLogService adminLogService)
        {
            _service = service;
            _hub = hub;
            _discordWebService = discordWebService;
            _adminLogService = adminLogService;
        }

        /// <summary>
        /// Get all available lobbies (optionally filter by code or include started ones).
        /// </summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Lobby>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetAll([FromQuery] string? codeFilter = null, [FromQuery] bool includeStarted = false)
        {
            var lobbies = _service.GetAll(codeFilter, includeStarted);
            return Ok(lobbies);
        }

        /// <summary>
        /// Get lobby details by code.
        /// </summary>
        [Authorize]
        [HttpGet("{code}")]
        [ProducesResponseType(typeof(Lobby), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetByCode(string code)
        {
            var lobby = _service.GetByCode(code);
            if (lobby is null)
                return NotFound(new { message = $"Lobby with code {code} not found." });
            return Ok(lobby);
        }

        /// <summary>
        /// Create a new lobby.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(Lobby), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> CreateAsync([FromQuery] int selectedLevelId, [FromQuery] string name, [FromQuery] int max_players, [FromQuery] string? password = null)
        {
            int hostPlayerId = int.Parse(User.FindFirst("id")!.Value);

            if (selectedLevelId <= 0)
                return BadRequest(new { message = "Invalid level ID." });

            var lobby = _service.Create(hostPlayerId, name, selectedLevelId, max_players, password);

            if (lobby is null)
                return BadRequest(new { message = "Could not create lobby (possible invalid parameters or player is in a lobby)." });

            await _hub.Clients.All.SendAsync("LobbyCreated", new
            {
                code = lobby.Code,
                hostId = hostPlayerId
            });

            await _discordWebService.NotifyNewLobbyAsync(User.Identity!.Name!, lobby.Code, lobby.LevelName);
            await _adminLogService.CreateAdminLog(hostPlayerId, ActionType.Create, TargetEntityType.Lobby, lobby.Id);

            return CreatedAtAction(nameof(GetByCode), new { code = lobby.Code }, lobby);
        }

        /// <summary>
        /// Join a lobby by code (with optional password).
        /// </summary>
        [Authorize]
        [HttpPost("{code}/join")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> JoinAsync(string code, [FromQuery] string? password = null)
        {
            int playerId = int.Parse(User.FindFirst("id")!.Value);
            var lobby = _service.GetByCode(code);
            if (lobby is null)
                return NotFound(new { message = $"Lobby with code {code} not found." });

            bool success = _service.JoinLobby(code, playerId, password);
            if (!success)
                return BadRequest(new { message = "Could not join lobby (invalid password or already joined)." });

            await _hub.Clients.Group(code).SendAsync("PlayerJoined", new
            {
                lobbyCode = code,
                playerId = playerId,
                username = User.Identity!.Name
            });


            return Ok(new { message = "Joined lobby successfully." });
        }

        /// <summary>
        /// Leave a lobby.
        /// </summary>
        [Authorize]
        [HttpPost("{code}/leave")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> LeaveAsync(string code)
        {
            int playerId = int.Parse(User.FindFirst("id")!.Value);
            bool success = _service.LeaveLobby(code, playerId);
            if (!success)
                return NotFound(new { message = $"Lobby with code {code} not found or player not in lobby." });

            await _hub.Clients.Group(code).SendAsync("PlayerLeft", new
            {
                lobbyCode = code,
                playerId = playerId
            });

            return Ok(new { message = "Left lobby successfully." });
        }

        /// <summary>
        /// Start a lobby (host only).
        /// </summary>
        [Authorize]
        [HttpPost("{code}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> StartAsync(string code)
        {
            int playerId = int.Parse(User.FindFirst("id")!.Value);
            bool success = _service.StartLobby(code, playerId);
            if (!success)
                return BadRequest(new { message = "Could not start lobby (only host can start or lobby not found)." });

            await _hub.Clients.Group(code).SendAsync("LobbyStarted", new
            {
                lobbyCode = code,
                startedBy = playerId
            });

            return Ok(new { message = "Lobby started successfully." });
        }

        /// <summary>
        /// Delete a lobby (host or admin).
        /// </summary>
        [Authorize]
        [HttpDelete("{code}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Delete(string code)
        {
            int userId = int.Parse(User.FindFirst("id")!.Value);
            Lobby? lobby = _service.GetByCode(code);
            AdminLog? log = await _adminLogService.CreateAdminLog(userId, ActionType.Delete, TargetEntityType.Lobby, lobby?.Id);

            bool deleted = _service.DeleteLobby(code, userId);

            if (!deleted)
            {
                _adminLogService.DeleteAdminLog(log!.Id);
                return NotFound(new { message = $"Lobby with code {code} not found or insufficient permissions." });
            }

            await _hub.Clients.All.SendAsync("LobbyDeleted", new
            {
                lobbyCode = code
            });


            return Ok(new { message = "Lobby deleted successfully." });
        }

        /// <summary>
        /// Kick a player from a lobby (host only).
        /// </summary>
        [Authorize]
        [HttpPost("{code}/kick/{targetPlayerName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> KickPlayerAsync(string code, string targetPlayerName)
        {
            int requesterId = int.Parse(User.FindFirst("id")!.Value);

            int targetPlayerId = _service.GetPlayerIdFromName(targetPlayerName);

            var lobby = _service.GetByCode(code);
            if (lobby is null)
                return NotFound(new { message = $"Lobby with code {code} not found." });

            if (_service.GetPlayerIdFromName(lobby.HostPlayer) != requesterId)
                return Unauthorized(new { message = "Only the host can kick players." });

            if (targetPlayerId == requesterId)
                return BadRequest(new { message = "You cannot kick yourself." });

            bool removed = _service.LeaveLobby(code, targetPlayerId);
            if (!removed)
                return NotFound(new { message = "Player not found in this lobby." });

            await _hub.Clients.Group(code).SendAsync("PlayerKicked", new
            {
                lobbyCode = code,
                kickedPlayerName = targetPlayerName 
            });

            return Ok(new { message = $"Player {targetPlayerId} was kicked from lobby {code}." });
        }
    }
}
