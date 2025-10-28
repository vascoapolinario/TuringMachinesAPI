using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly LobbyService _lobbyService;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyHub(LobbyService lobbyService, IHubContext<LobbyHub> hubContext)
        {
            _lobbyService = lobbyService;
            _hubContext = hubContext;
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Connection established: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Connection lost: {Context.ConnectionId}");

            try
            {
                string? userIdClaim = Context.User?.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    await base.OnDisconnectedAsync(exception);
                    return;
                }

                int playerId = int.Parse(userIdClaim);

                var lobbyEntity = _lobbyService.GetEntityByPlayerId(playerId);
                if (lobbyEntity != null)
                {
                    Console.WriteLine($"Player {playerId} disconnected from lobby {lobbyEntity.Code}");

                    bool isHost = lobbyEntity.HostPlayerId == playerId;

                    _lobbyService.LeaveLobby(lobbyEntity.Code, playerId);

                    if (isHost)
                    {
                        await _hubContext.Clients.All.SendAsync("LobbyDeleted", new
                        {
                            lobbyCode = lobbyEntity.Code
                        });
                    }
                    else
                    {
                        await _hubContext.Clients.Group(lobbyEntity.Code).SendAsync("PlayerLeft", new
                        {
                            lobbyCode = lobbyEntity.Code,
                            playerId = playerId
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling disconnect: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinLobbyGroup(string code)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
        }

        public async Task LeaveLobbyGroup(string code)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
        }


        /// <summary>
        /// Host broadcasts the entire Turing machine state (nodes, connections, etc.)
        /// </summary>
        public async Task SyncEnvironment(object payload)
        {
            Console.WriteLine($"[Hub] SyncEnvironment RAW payload: {payload?.GetType()}");

            var json = payload?.ToString();
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("[Hub] No payload data.");
                return;
            }
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string lobbyCode = root.GetProperty("lobbyCode").GetString();
            var state = root.GetProperty("state");

            await Clients.Group(lobbyCode).SendAsync("EnvironmentSynced", new
            {
                lobbyCode,
                state
            });

            Console.WriteLine($"[Lobby {lobbyCode}] Environment synced (broadcasted).");
        }

        /// <summary>
        /// Client proposes to add a node (only host should handle this).
        /// </summary>
        public async Task ProposeNode(object payload)
        {
            try
            {
                Console.WriteLine($"[ProposeNode] RAW payload type: {payload?.GetType()}");

                JObject data = null;

                switch (payload)
                {
                    case JObject jObj:
                        data = jObj;
                        break;
                    case JArray jArr when jArr.Count > 0:
                        data = jArr[0] as JObject;
                        break;
                    default:
                        var json = payload?.ToString();
                        if (!string.IsNullOrEmpty(json))
                        {
                            if (json.TrimStart().StartsWith("["))
                                data = JArray.Parse(json).FirstOrDefault() as JObject;
                            else
                                data = JObject.Parse(json);
                        }
                        break;
                }

                if (data == null)
                {
                    Console.WriteLine("[ProposeNode] Invalid payload format");
                    return;
                }

                string lobbyCode = data["lobbyCode"]?.ToString() ?? "";
                var posToken = data["pos"];
                float x = posToken?["x"]?.ToObject<float>() ?? 0f;
                float y = posToken?["y"]?.ToObject<float>() ?? 0f;
                bool isEnd = data["isEnd"]?.ToObject<bool>() ?? false;

                var lobbyDto = _lobbyService.GetByCode(lobbyCode);
                if (lobbyDto == null)
                {
                    Console.WriteLine($"[ProposeNode] Lobby {lobbyCode} not found");
                    return;
                }

                await Clients.Group(lobbyCode).SendAsync("NodeProposed", new
                {
                    lobbyCode,
                    x,
                    y,
                    isEnd,
                    proposer = Context.User?.Identity?.Name ?? "Unknown"
                });

                Console.WriteLine($"[Lobby {lobbyCode}] NodeProposed from {Context.User?.Identity?.Name} at ({x}, {y})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProposeNode] ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Client proposes to delete a node or connection (host should handle).
        /// </summary>
        public async Task ProposeDelete(object payload)
        {
            var data = payload as JObject;
            if (data == null) return;

            string lobbyCode = data["lobbyCode"]?.ToString() ?? "";
            var target = data["target"];

            var lobbyDto = _lobbyService.GetByCode(lobbyCode);
            if (lobbyDto == null)
            {
                Console.WriteLine($"[ProposeDelete] Lobby {lobbyCode} not found");
                return;
            }

            // Notify the host of this lobby
            await Clients.Group(lobbyCode).SendAsync("DeleteProposed", new
            {
                lobbyCode,
                target,
                proposer = Context.User?.Identity?.Name
            });

            Console.WriteLine($"[Lobby {lobbyCode}] DeleteProposed from {Context.User?.Identity?.Name}");
        }
    }
}