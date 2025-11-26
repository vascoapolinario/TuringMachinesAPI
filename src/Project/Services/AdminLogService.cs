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

        public async Task<Dtos.AdminLog?> CreateAdminLog(int ActorId, ActionType Action, TargetEntityType TargetType, int? TargetEntityId)
        {
            var actor = db.Players.Find(ActorId);
            if (TargetEntityId == null)
            {
                return null;
            }
            string TargetEntityName = GetTargetEntityName(TargetType, TargetEntityId);
            if (actor == null) return null;

            var entity = new Entities.AdminLog
            {
                ActorId = ActorId,
                ActorName = actor.Username,
                ActorRole = actor.Role,
                Action = Action,
                TargetEntityType = TargetType,
                TargetEntityId = TargetEntityId,
                TargetEntityName = TargetEntityName,
                DoneAtUtc = DateTime.UtcNow
            };

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

            if (actors.Count == 0)
            {
                return logs.Select(log => new Dtos.AdminLog
                {
                    Id = log.Id,
                    ActorName = "Unknown",
                    ActorRole = "Unknown",
                    Action = log.Action.ToString(),
                    TargetEntityType = log.TargetEntityType.ToString(),
                    TargetEntityId = log.TargetEntityId,
                    TargetEntityName = log.TargetEntityName,
                    DoneAt = log.DoneAtUtc
                }).ToList();
            }

            return logs.Select(log =>
            {
                return new Dtos.AdminLog
                {
                    Id = log.Id,
                    ActorName = log.ActorName,
                    ActorRole = log.ActorRole,
                    Action = log.Action.ToString(),
                    TargetEntityType = log.TargetEntityType.ToString(),
                    TargetEntityId = log.TargetEntityId,
                    TargetEntityName = log.TargetEntityName,
                    DoneAt = log.DoneAtUtc
                };
            }).ToList();
        }

        public IEnumerable<Dtos.AdminLog> GetAdminLogsByActorName(string actorName)
        {
            var logs = db.AdminLogs
                .Where(l => l.ActorName.Contains(actorName))
                .OrderByDescending(l => l.DoneAtUtc)
                .ToList();
            return logs.Select(log => new Dtos.AdminLog
            {
                Id = log.Id,
                ActorName = log.ActorName,
                ActorRole = log.ActorRole,
                Action = log.Action.ToString(),
                TargetEntityType = log.TargetEntityType.ToString(),
                TargetEntityId = log.TargetEntityId,
                TargetEntityName = log.TargetEntityName,
                DoneAt = log.DoneAtUtc
            }).ToList();
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

        private string GetTargetEntityName(TargetEntityType type, int? id)
        {
            if (type == TargetEntityType.LeaderboardSubmission)
            {
                Entities.LevelSubmission? levelSubmission = db.LevelSubmissions.Where(ls => ls.Id == id).FirstOrDefault();
                if (levelSubmission != null)
                {
                    var playerName = db.Players.Where(p => p.Id == levelSubmission.PlayerId).Select(p => p.Username).FirstOrDefault() ?? "Unknown Player";
                    var leaderboardLevelName = db.LeaderboardLevels.Where(lb => lb.Id == levelSubmission.LeaderboardLevelId).Select(lb => lb.Name).FirstOrDefault() ?? "Unknown Level";
                    return $"Submission Id:{id} | Player: {playerName} | Level: {leaderboardLevelName}";
                }
            }

            return type switch
            {
                TargetEntityType.WorkshopLevel or TargetEntityType.WorkshopMachine =>
                    db.WorkshopItems.Where(wi => wi.Id == id).Select(wi => wi.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Lobby =>
                    db.Lobbies.Where(l => l.Id == id).Select(l => l.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.LeaderboardLevel =>
                    db.LeaderboardLevels.Where(lb => lb.Id == id).Select(lb => lb.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Player =>
                    db.Players.Where(p => p.Id == id).Select(p => p.Username).FirstOrDefault() ?? "Unknown",
                _ => "Unknown",
            };
        }
    }
}
