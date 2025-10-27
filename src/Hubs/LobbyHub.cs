using Microsoft.AspNetCore.SignalR;
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
    }
}