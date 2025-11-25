using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using TuringMachinesAPI.Enums;

namespace TuringMachinesAPI.Services
{
    public class AdminLogService
    {
        private readonly TuringMachinesDbContext db;

        public AdminLogService(TuringMachinesDbContext dbContext)
        {
            db = dbContext;
        }

        public async Task<Dtos.AdminLog?> CreateAdminLog(int ActorId, ActionType Action, TargetEntityType TargetType, int TargetEntityId)
        {
            var actor = db.Players.Find(ActorId);
            string TargetEntityName;
            if (actor == null) return null;

            var entity = new Entities.AdminLog
            {
                ActorId = ActorId,
                Action = Action,
                TargetEntityType = TargetType,
                TargetEntityId = TargetEntityId,
                DoneAtUtc = DateTime.UtcNow
            };

            TargetEntityName = GetTargetEntityName(entity.TargetEntityType, entity.TargetEntityId);
            db.AdminLogs.Add(entity);
            db.SaveChanges();
            

            return new Dtos.AdminLog
            {
                Id = entity.Id,
                ActorName = actor.Username,
                ActorRole = actor.Role,
                Action = entity.Action.ToString(),
                TargetEntityType = entity.TargetEntityType.ToString(),
                TargetEntityId = entity.TargetEntityId,
                TargetEntityName = TargetEntityName,
                DoneAt = entity.DoneAtUtc
            };
        }

        public IEnumerable<Dtos.AdminLog> GetAllAdminLogs()
        {
            var logs = db.AdminLogs
            .OrderByDescending(l => l.DoneAtUtc)
            .ToList();

            var actorIds = logs.Select(l => l.ActorId).Distinct().ToList();
            var actors = db.Players
                .Where(p => actorIds.Contains(p.Id))
                .ToDictionary(p => p.Id);

            return logs.Select(log =>
            {
                actors.TryGetValue(log.ActorId, out var actor);
                string TargetEntityName = GetTargetEntityName(log.TargetEntityType, log.TargetEntityId);
                return new Dtos.AdminLog
                {
                    Id = log.Id,
                    ActorName = actor?.Username ?? "Unknown",
                    ActorRole = actor?.Role ?? "User",
                    Action = log.Action.ToString(),
                    TargetEntityType = log.TargetEntityType.ToString(),
                    TargetEntityId = log.TargetEntityId,
                    TargetEntityName = TargetEntityName,
                    DoneAt = log.DoneAtUtc
                };
            }).ToList();
        }

        public IEnumerable<Dtos.AdminLog> GetAdminLogsByActorName(string actorName)
        {
            var actor = db.Players.FirstOrDefault(a => a.Username.Contains(actorName));
            if (actor == null) return Enumerable.Empty<Dtos.AdminLog>();

            var logs = db.AdminLogs.Where(log => log.ActorId == actor.Id);

            var log = logs.First();
            string TargetEntityName = GetTargetEntityName(log.TargetEntityType, log.TargetEntityId);
            return logs.Select(log => new Dtos.AdminLog
            {
                Id = log.Id,
                ActorName = actor.Username,
                ActorRole = actor.Role,
                Action = log.Action.ToString(),
                TargetEntityType = log.TargetEntityType.ToString(),
                TargetEntityId = log.TargetEntityId,
                TargetEntityName = TargetEntityName,
                DoneAt = log.DoneAtUtc
            });
        }

        public bool DeleteAdminLog(int id)
        {
            var log = db.AdminLogs.FirstOrDefault(l => l.Id == id);
            if (log == null) return false;
            db.AdminLogs.Remove(log);
            db.SaveChanges();
            return true;
        }

        public bool DeleteAdminLogs(TimeSpan? olderThan = null)
        {
            var logsToDelete = db.AdminLogs.AsQueryable();
            if (olderThan.HasValue)
            {
                var cutoffDate = DateTime.UtcNow - olderThan.Value;
                logsToDelete = logsToDelete.Where(log => log.DoneAtUtc < cutoffDate);
            }
            db.AdminLogs.RemoveRange(logsToDelete);
            db.SaveChanges();
            return true;
        }

        private string GetTargetEntityName(TargetEntityType type, int id)
        {
            return type switch
            {
                TargetEntityType.WorkshopLevel or TargetEntityType.WorkshopMachine =>
                    db.WorkshopItems.Where(wi => wi.Id == id).Select(wi => wi.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Lobby =>
                    db.Lobbies.Where(l => l.Id == id).Select(l => l.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.LeaderboardSubmission =>
                    $"Leaderboard Submission Id:{id}",
                TargetEntityType.LeaderboardLevel =>
                    db.LeaderboardLevels.Where(lb => lb.Id == id).Select(lb => lb.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Player =>
                    db.Players.Where(p => p.Id == id).Select(p => p.Username).FirstOrDefault() ?? "Unknown",
                _ => "Unknown",
            };
        }
    }
}
