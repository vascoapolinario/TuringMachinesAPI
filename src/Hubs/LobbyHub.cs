using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;

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


        public async Task ProposeConnection(object payload)
        {
            try
            {
                Console.WriteLine($"[ProposeConnection] RAW payload type: {payload?.GetType()}");

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
                    Console.WriteLine("[ProposeConnection] Invalid payload format");
                    return;
                }

                string lobbyCode = data["lobbyCode"]?.ToString() ?? "";
                int startId = data["startId"]?.ToObject<int>() ?? -1;
                int endId = data["endId"]?.ToObject<int>() ?? -1;

                var lobby = _lobbyService.GetByCode(lobbyCode);
                if (lobby == null)
                {
                    Console.WriteLine($"[ProposeConnection] Lobby {lobbyCode} not found");
                    return;
                }

                var read = data["read"]?.ToObject<List<string>>() ?? new List<string>();
                var write = data["write"]?.ToObject<List<string>>() ?? new List<string>();
                var move = data["move"]?.ToObject<List<string>>() ?? new List<string>();
                var read2 = data["read2"]?.ToObject<List<string>>() ?? new List<string>();
                var write2 = data["write2"]?.ToObject<List<string>>() ?? new List<string>();
                var move2 = data["move2"]?.ToObject<List<string>>() ?? new List<string>();

                await Clients.Group(lobbyCode).SendAsync("ConnectionProposed", new
                {
                    lobbyCode,
                    startId,
                    endId,
                    read,
                    write,
                    move,
                    read2,
                    write2,
                    move2,
                    proposer = Context.User?.Identity?.Name ?? "Unknown"
                });

                Console.WriteLine($"[Lobby {lobbyCode}] ConnectionProposed {startId}->{endId} from {Context.User?.Identity?.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProposeConnection] ERROR: {ex.Message}");
            }
        }

        public async Task ProposeDelete(object payload)
        {
            try
            {
                Console.WriteLine($"[ProposeDelete] RAW payload type: {payload?.GetType()}");

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
                    Console.WriteLine("[ProposeDelete] Invalid payload format");
                    return;
                }

                string lobbyCode = data["lobbyCode"]?.ToString() ?? "";
                var targetToken = data["target"];

                if (targetToken == null)
                {
                    Console.WriteLine("[ProposeDelete] No target found");
                    return;
                }

                string type = targetToken["type"]?.ToString() ?? "";

                var target = new Dictionary<string, object?>
                {
                    ["type"] = type
                };

                if (type == "node")
                {
                    target["x"] = targetToken["x"]?.ToObject<float>() ?? 0f;
                    target["y"] = targetToken["y"]?.ToObject<float>() ?? 0f;
                }
                else if (type == "connection")
                {
                    var start = targetToken["start"];
                    var end = targetToken["end"];

                    if (start != null && end != null)
                    {
                        target["start"] = new Dictionary<string, float>
                        {
                            ["x"] = start["x"]?.ToObject<float>() ?? 0f,
                            ["y"] = start["y"]?.ToObject<float>() ?? 0f
                        };
                        target["end"] = new Dictionary<string, float>
                        {
                            ["x"] = end["x"]?.ToObject<float>() ?? 0f,
                            ["y"] = end["y"]?.ToObject<float>() ?? 0f
                        };
                    }
                }

                var lobby = _lobbyService.GetByCode(lobbyCode);
                if (lobby == null)
                {
                    Console.WriteLine($"[ProposeDelete] Lobby {lobbyCode} not found");
                    return;
                }

                await Clients.Group(lobbyCode).SendAsync("DeleteProposed", new
                {
                    lobbyCode,
                    target,
                    proposer = Context.User?.Identity?.Name ?? "Unknown"
                });

                Console.WriteLine($"[Lobby {lobbyCode}] DeleteProposed ({type}) from {Context.User?.Identity?.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProposeDelete] ERROR: {ex.Message}");
            }
        }

        public async Task SendChatMessage(object payload)
        {
            try
            {
                var json = payload?.ToString();
                if (string.IsNullOrEmpty(json))
                    return;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string lobbyCode = root.GetProperty("lobbyCode").GetString();
                string sender = root.TryGetProperty("sender", out var sProp) ? sProp.GetString() ?? "Unknown" : "Unknown";
                string message = root.TryGetProperty("message", out var mProp) ? mProp.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(lobbyCode) || string.IsNullOrWhiteSpace(message))
                    return;

                if (ValidationUtils.ContainsDisallowedContent(message))
                {
                    await Clients.Caller.SendAsync("ChatRejected", new { reason = "Message contains disallowed content." });
                    return;
                }

                var formatted = new
                {
                    lobbyCode,
                    sender,
                    message,
                    timestamp = DateTime.UtcNow
                };

                await Clients.Group(lobbyCode).SendAsync("ChatMessageReceived", formatted);

                Console.WriteLine($"[Chat] {sender}@{lobbyCode}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendChatMessage] ERROR: {ex.Message}");
            }
        }

    }
}