using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace TuringMachinesAPI.Services
{
    public class LobbyService
    {
        private readonly TuringMachinesDbContext db;
        private readonly PasswordHashService PasswordService;
        private readonly IMemoryCache cache;

        public LobbyService(TuringMachinesDbContext context, PasswordHashService _PasswordService, IMemoryCache memoryCache)
        {
            db = context;
            PasswordService = _PasswordService;
            cache = memoryCache;
        }

        public IEnumerable<Dtos.Lobby> GetAll(string? codeFilter = null, bool includeStarted = false)
        {
            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                if (!string.IsNullOrWhiteSpace(codeFilter))
                {
                    cachedLobbies = cachedLobbies!.Where(l => l.Code.ToLower().Contains(codeFilter.ToLower()));
                }
                if (!includeStarted)
                {
                    cachedLobbies = cachedLobbies!.Where(l => !l.HasStarted);
                }
                return cachedLobbies!;
            }

            var query = db.Lobbies.AsNoTracking();
            var lobbyDtos = new List<Dtos.Lobby>();
            var lobbies = query.ToList();

            foreach (var lobby in lobbies)
            {
                var lobbyPlayerNames = new List<string>();
                string hostName;
                if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
                {
                    var hostPlayer = cachedPlayers!.FirstOrDefault(p => p.Id == lobby.HostPlayerId);
                    hostName = hostPlayer != null ? hostPlayer.Username : "Unknown";

                    if (lobby.LobbyPlayers != null)
                    {
                        foreach (var playerId in lobby.LobbyPlayers)
                        {
                            var player = cachedPlayers!.FirstOrDefault(p => p.Id == playerId);
                            lobbyPlayerNames.Add(player != null ? player.Username : "Unknown");
                        }
                    }
                }
                else
                {
                    hostName = db.Players
                        .AsNoTracking()
                        .Where(p => p.Id == lobby.HostPlayerId)
                        .Select(p => p.Username)
                        .FirstOrDefault() ?? "Unknown";

                    if (lobby.LobbyPlayers != null)
                    {
                        foreach (var playerId in lobby.LobbyPlayers)
                        {
                            var playerName = db.Players
                                .AsNoTracking()
                                .Where(p => p.Id == playerId)
                                .Select(p => p.Username)
                                .FirstOrDefault() ?? "Unknown";
                            lobbyPlayerNames.Add(playerName);
                        }
                    }
                }

                string levelName;
                if (cache.TryGetValue("WorkshopItems", out IEnumerable<Dtos.WorkshopItem>? cachedItems))
                {
                    var levelItem = cachedItems!.FirstOrDefault(w => w.Id == lobby.SelectedLevelId);
                    levelName = levelItem != null ? levelItem.Name : "Unknown";
                }
                else
                {
                    levelName = db.WorkshopItems
                        .AsNoTracking()
                        .Where(w => w.Id == lobby.SelectedLevelId)
                        .Select(w => w.Name)
                        .FirstOrDefault() ?? "Unknown";
                }


                Dtos.Lobby newLobby = new Dtos.Lobby
                {
                    Id = lobby.Id,
                    Code = lobby.Code,
                    Name = lobby.Name,
                    Password = "",
                    PasswordProtected = !string.IsNullOrEmpty(lobby.Password),
                    HostPlayer = hostName,
                    LevelName = levelName,
                    MaxPlayers = lobby.MaxPlayers,
                    HasStarted = lobby.HasStarted,
                    CreatedAt = lobby.CreatedAt,
                    LobbyPlayers = lobby.LobbyPlayers != null
                        ? lobbyPlayerNames
                        : new List<string>()
                };
                lobbyDtos.Add(newLobby);
            }
            cache.Set("Lobbies", lobbyDtos);
            if (!string.IsNullOrWhiteSpace(codeFilter))
            {
                lobbyDtos = lobbyDtos.Where(l => l.Code.ToLower().Contains(codeFilter.ToLower())).ToList();
            }
            if (!includeStarted)
            {
                lobbyDtos = lobbyDtos.Where(l => !l.HasStarted).ToList();
            }
            return lobbyDtos;

        }

        public Dtos.Lobby? GetByCode(string code)
        {
            var lobbies = GetAll();
            return lobbies.FirstOrDefault(l => l.Code == code);
        }

        public Dtos.Lobby? Create(int hostPlayerId, string name, int selectedLevelId, int max_players, string? password = null)
        {
            var random = new Random();
            string code = random.Next(10000, 99999).ToString();

            name = ValidationUtils.ContainsDisallowedContent(name) ? "Unnamed Lobby" : name;
            if (max_players < 2 || max_players > 10)
                max_players = 4;

            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                if (cachedLobbies!.Any(l => l.HostPlayer == db.Players.AsNoTracking().Where(p => p.Id == hostPlayerId)
                    .Select(p => p.Username).FirstOrDefault()))
                {
                    return null;
                }
            }
            else
            {
                var existingLobby = db.Lobbies.FirstOrDefault(l => l.LobbyPlayers != null && l.LobbyPlayers.Contains(hostPlayerId));
                if (existingLobby != null)
                    return null;
            }

            string? passwordHash = null;
            if (password != null) {
                passwordHash = PasswordService.Hash(password);
            }

            var lobby = new Entities.Lobby
            {
                Code = code,
                Name = name,
                Password = passwordHash,
                HostPlayerId = hostPlayerId,
                SelectedLevelId = selectedLevelId,
                MaxPlayers = max_players,
                HasStarted = false,
                CreatedAt = DateTime.UtcNow,
                LobbyPlayers = new List<int> { hostPlayerId }
            };

            db.Lobbies.Add(lobby);
            db.SaveChanges();

            string hostName;
            string levelName;

            if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
            {
                var hostPlayer = cachedPlayers!.FirstOrDefault(p => p.Id == hostPlayerId);
                hostName = hostPlayer != null ? hostPlayer.Username : "Unknown";
            }
            else
            {
                hostName = db.Players.AsNoTracking().Where(p => p.Id == hostPlayerId)
                .Select(p => p.Username).FirstOrDefault() ?? "Unknown";
            }

            if (cache.TryGetValue("WorkshopItems", out IEnumerable<Dtos.WorkshopItem>? cachedItems))
            {
                var levelItem = cachedItems!.FirstOrDefault(w => w.Id == selectedLevelId);
                levelName = levelItem != null ? levelItem.Name : "Unknown";
            }
            else
            {
                levelName = db.WorkshopItems.AsNoTracking().Where(w => w.Id == selectedLevelId)
                .Select(w => w.Name).FirstOrDefault() ?? "Unknown";
            }

            Dtos.Lobby newLobby = new Dtos.Lobby
            {
                Id = lobby.Id,
                Code = lobby.Code,
                Name = lobby.Name,
                Password = "",
                PasswordProtected = !string.IsNullOrEmpty(password),
                HostPlayer = hostName,
                LevelName = levelName,
                MaxPlayers = lobby.MaxPlayers,
                HasStarted = false,
                CreatedAt = lobby.CreatedAt,
                LobbyPlayers = new List<string> { hostName }
            };

            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? existingCachedLobbies))
            {
                var lobbyDtoList = existingCachedLobbies!.ToList();
                lobbyDtoList.Add(newLobby);
                cache.Set("Lobbies", lobbyDtoList);
            }
            else
            {
                cache.Set("Lobbies", new List<Dtos.Lobby> { newLobby });
            }

            return newLobby;
        }

        public bool JoinLobby(string code, int playerId, string? password = null)
        {
            var lobby = db.Lobbies.FirstOrDefault(l => l.Code == code);
            if (lobby == null)
                return false;

            if (!string.IsNullOrEmpty(lobby.Password) && !PasswordService.Verify(password, lobby.Password))
                return false;

            if (lobby.LobbyPlayers == null)
                lobby.LobbyPlayers = new List<int>();

            if (lobby.LobbyPlayers.Contains(playerId))
                return false;

            if (!lobby.LobbyPlayers.Contains(playerId))
            {
                lobby.LobbyPlayers.Add(playerId);
                db.SaveChanges();
            }

            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                var lobbyDtoList = cachedLobbies!.ToList();
                var lobbyDto = lobbyDtoList.FirstOrDefault(l => l.Code == code);
                if (lobbyDto != null)
                {
                    string playerName;
                    if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
                    {
                        var player = cachedPlayers!.FirstOrDefault(p => p.Id == playerId);
                        playerName = player != null ? player.Username : "Unknown";
                    }
                    else
                    {
                        playerName = db.Players
                            .AsNoTracking()
                            .Where(p => p.Id == playerId)
                            .Select(p => p.Username)
                            .FirstOrDefault() ?? "Unknown";
                    }
                    var updatedPlayerList = lobbyDto.LobbyPlayers!.ToList();
                    updatedPlayerList.Add(playerName);
                    lobbyDto.LobbyPlayers = updatedPlayerList;
                }
                cache.Set("Lobbies", lobbyDtoList);
            }
            return true;
        }

        public bool LeaveLobby(string code, int playerId)
        {
            var lobby = db.Lobbies.FirstOrDefault(l => l.Code == code);
            if (lobby == null || lobby.LobbyPlayers == null)
                return false;

            lobby.LobbyPlayers.Remove(playerId);

            if (playerId == lobby.HostPlayerId)
            {
                db.Lobbies.Remove(lobby);
            }

            db.SaveChanges();
            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                var lobbyDtoList = cachedLobbies!.ToList();
                var lobbyDto = lobbyDtoList.FirstOrDefault(l => l.Code == code);
                if (lobbyDto != null)
                {
                    string playerName;
                    if (cache.TryGetValue("Players", out IEnumerable<Dtos.Player>? cachedPlayers))
                    {
                        var player = cachedPlayers!.FirstOrDefault(p => p.Id == playerId);
                        playerName = player != null ? player.Username : "Unknown";
                    }
                    else
                    {
                        playerName = db.Players
                            .AsNoTracking()
                            .Where(p => p.Id == playerId)
                            .Select(p => p.Username)
                            .FirstOrDefault() ?? "Unknown";
                    }
                    var updatedPlayerList = lobbyDto.LobbyPlayers!.ToList();
                    if (playerId == lobby.HostPlayerId)
                    {
                        lobbyDtoList.Remove(lobbyDto);
                        cache.Set("Lobbies", lobbyDtoList);
                        return true;
                    }
                    updatedPlayerList.Remove(playerName);
                    lobbyDto.LobbyPlayers = updatedPlayerList;
                }
                cache.Set("Lobbies", lobbyDtoList);
            }
            return true;
        }

        public bool StartLobby(string code, int playerId)
        {
            var lobby = db.Lobbies.FirstOrDefault(l => l.Code == code);
            if (lobby == null || lobby.LobbyPlayers == null)
                return false;

            if (lobby.HostPlayerId != playerId)
                return false;

            if (lobby.LobbyPlayers.Count < 2 || lobby.LobbyPlayers.Count > lobby.MaxPlayers)
                return false;

            lobby.HasStarted = true;
            db.SaveChanges();
            if (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                var lobbyDtoList = cachedLobbies!.ToList();
                var lobbyDto = lobbyDtoList.FirstOrDefault(l => l.Code == code);
                if (lobbyDto != null)
                {
                    lobbyDto.HasStarted = true;
                }
                cache.Set("Lobbies", lobbyDtoList);
            }
            return true;
        }

        public bool DeleteLobby(string code, int userId)
        {
            var lobby = db.Lobbies.FirstOrDefault(l => l.Code == code);
            if (lobby == null)
                return false;

            if (lobby.HostPlayerId != userId &&
                !db.Players.Any(p => p.Id == userId && p.Role == "Admin"))
                return false;

            db.Lobbies.Remove(lobby);
            db.SaveChanges();
            if  (cache.TryGetValue("Lobbies", out IEnumerable<Dtos.Lobby>? cachedLobbies))
            {
                var lobbyDtoList = cachedLobbies!.ToList();
                var lobbyDto = lobbyDtoList.FirstOrDefault(l => l.Code == code);
                if (lobbyDto != null)
                {
                    lobbyDtoList.Remove(lobbyDto);
                }
                cache.Set("Lobbies", lobbyDtoList);
            }
            return true;
        }
        public Entities.Lobby? GetEntityByPlayerId(int playerId)
        {
            return db.Lobbies
                .FirstOrDefault(l => l.LobbyPlayers != null && l.LobbyPlayers.Contains(playerId));
        }

        public int GetPlayerIdFromName(string playerName)
        {
            return db.Players
                .AsNoTracking()
                .Where(p => p.Username.ToLower() == playerName.ToLower())
                .Select(p => p.Id)
                .FirstOrDefault();
        }
    }
}
