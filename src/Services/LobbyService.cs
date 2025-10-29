using Microsoft.EntityFrameworkCore;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Utils;

namespace TuringMachinesAPI.Services
{
    public class LobbyService
    {
        private readonly TuringMachinesDbContext db;

        public LobbyService(TuringMachinesDbContext context)
        {
            db = context;
        }

        public IEnumerable<Dtos.Lobby> GetAll(string? codeFilter = null, bool includeStarted = false)
        {
            var query = db.Lobbies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(codeFilter))
                query = query.Where(l => l.Code.ToLower().Contains(codeFilter.ToLower()));

            if (!includeStarted)
                query = query.Where(l => !l.HasStarted);

            var lobbies = query.ToList();

            foreach (var lobby in lobbies)
            {
                var hostName = db.Players
                    .AsNoTracking()
                    .Where(p => p.Id == lobby.HostPlayerId)
                    .Select(p => p.Username)
                    .FirstOrDefault() ?? "Unknown";

                var levelName = db.WorkshopItems
                    .AsNoTracking()
                    .Where(w => w.Id == lobby.SelectedLevelId)
                    .Select(w => w.Name)
                    .FirstOrDefault() ?? "Unknown";

                yield return new Dtos.Lobby
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
                        ? lobby.LobbyPlayers.Select(id =>
                            db.Players.AsNoTracking().Where(p => p.Id == id).Select(p => p.Username).FirstOrDefault() ?? "Unknown").ToList()
                        : new List<string>()
                };
            }
        }

        public Dtos.Lobby? GetByCode(string code)
        {
            var entity = db.Lobbies.AsNoTracking().FirstOrDefault(l => l.Code == code);
            if (entity == null)
                return null;

            var hostName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == entity.HostPlayerId)
                .Select(p => p.Username)
                .FirstOrDefault() ?? "Unknown";

            var levelName = db.WorkshopItems
                .AsNoTracking()
                .Where(w => w.Id == entity.SelectedLevelId)
                .Select(w => w.Name)
                .FirstOrDefault() ?? "Unknown";

            return new Dtos.Lobby
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Password = "",
                PasswordProtected = !string.IsNullOrEmpty(entity.Password),
                HostPlayer = hostName,
                LevelName = levelName,
                MaxPlayers = entity.MaxPlayers,
                HasStarted = entity.HasStarted,
                CreatedAt = entity.CreatedAt,
                LobbyPlayers = entity.LobbyPlayers != null
                    ? entity.LobbyPlayers.Select(id =>
                        db.Players.AsNoTracking().Where(p => p.Id == id).Select(p => p.Username).FirstOrDefault() ?? "Unknown").ToList()
                    : new List<string>()
            };
        }

        public Dtos.Lobby? Create(int hostPlayerId, string name, int selectedLevelId, int max_players, string? password = null)
        {
            var random = new Random();
            string code = random.Next(10000, 99999).ToString();

            name = ValidationUtils.ContainsDisallowedContent(name) ? "Unnamed Lobby" : name;
            if (max_players < 2 || max_players > 10)
                max_players = 4;

            if (password != null && ValidationUtils.ContainsDisallowedContent(password))
                password = null;

            var existingLobby = db.Lobbies.FirstOrDefault(l => l.LobbyPlayers != null && l.LobbyPlayers.Contains(hostPlayerId));
            if (existingLobby != null)
                return null;

            var lobby = new Entities.Lobby
            {
                Code = code,
                Name = name,
                Password = password,
                HostPlayerId = hostPlayerId,
                SelectedLevelId = selectedLevelId,
                MaxPlayers = max_players,
                HasStarted = false,
                CreatedAt = DateTime.UtcNow,
                LobbyPlayers = new List<int> { hostPlayerId }
            };

            db.Lobbies.Add(lobby);
            db.SaveChanges();

            var hostName = db.Players.AsNoTracking().Where(p => p.Id == hostPlayerId)
                .Select(p => p.Username).FirstOrDefault() ?? "Unknown";

            var levelName = db.WorkshopItems.AsNoTracking().Where(w => w.Id == selectedLevelId)
                .Select(w => w.Name).FirstOrDefault() ?? "Unknown";

            return new Dtos.Lobby
            {
                Id = lobby.Id,
                Code = lobby.Code,
                Name = lobby.Name,
                Password = "",
                HostPlayer = hostName,
                LevelName = levelName,
                MaxPlayers = lobby.MaxPlayers,
                HasStarted = false,
                CreatedAt = lobby.CreatedAt,
                LobbyPlayers = new List<string> { hostName }
            };
        }

        public bool JoinLobby(string code, int playerId, string? password = null)
        {
            var lobby = db.Lobbies.FirstOrDefault(l => l.Code == code);
            if (lobby == null)
                return false;

            if (!string.IsNullOrEmpty(lobby.Password) && lobby.Password != password)
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
