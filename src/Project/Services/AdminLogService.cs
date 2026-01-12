using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Entities;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using TuringMachinesAPI.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace TuringMachinesAPI.Services
{
    public class AdminLogService
    {
        private readonly TuringMachinesDbContext db;
        private readonly IMemoryCache cache;

        public AdminLogService(TuringMachinesDbContext dbContext, IMemoryCache memoryCache)
        {
            db = dbContext;
            this.cache = memoryCache;
        }

        public async Task<Dtos.AdminLog?> CreateAdminLog(int ActorId, ActionType Action, TargetEntityType TargetType, int? TargetEntityId)
        {
            string actorName;
            string actorRole;
            Dtos.Player? actor = CacheHelper.GetPlayerFromCacheById(cache, ActorId);
            if (actor == null)
            {
                Entities.Player? dbActor = db.Players.FirstOrDefault(p => p.Id == ActorId);
                if (dbActor != null)
                {
                    actorName = dbActor.Username;
                    actorRole = dbActor.Role;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                actorName = actor.Username;
                actorRole = actor.Role;
            }

            if (TargetEntityId == null)
            {
                return null;
            }
            string TargetEntityName = GetTargetEntityName(TargetType, TargetEntityId);

            var entity = new Entities.AdminLog
            {
                ActorId = ActorId,
                ActorName = actorName,
                ActorRole = actorRole,
                Action = Action,
                TargetEntityType = TargetType,
                TargetEntityId = TargetEntityId,
                TargetEntityName = TargetEntityName,
                DoneAtUtc = DateTime.UtcNow
            };

            db.AdminLogs.Add(entity);
            db.SaveChanges();
            

            Dtos.AdminLog newLog = new Dtos.AdminLog
            {
                Id = entity.Id,
                ActorName = entity.ActorName,
                ActorRole = entity.ActorRole,
                Action = entity.Action.ToString(),
                TargetEntityType = entity.TargetEntityType.ToString(),
                TargetEntityId = entity.TargetEntityId,
                TargetEntityName = TargetEntityName,
                DoneAt = entity.DoneAtUtc
            };
            if (cache.TryGetValue("AdminLogs", out IEnumerable<Dtos.AdminLog>? AdminLogs))
            {
                var updatedLogs = AdminLogs!.ToList();
                updatedLogs.Add(newLog);
                cache.Set("AdminLogs", updatedLogs);
            }

            return newLog;
        }

        public IEnumerable<Dtos.AdminLog> GetAllAdminLogs()
        {
            if (cache.TryGetValue("AdminLogs", out IEnumerable<Dtos.AdminLog>? AdminLogs))
            {
                return AdminLogs!;
            }

            var logs = db.AdminLogs
            .OrderByDescending(l => l.DoneAtUtc)
            .ToList();

            if (logs.Count == 0)
            {
                cache.Set("AdminLogs", Enumerable.Empty<Dtos.AdminLog>());
                return Enumerable.Empty<Dtos.AdminLog>();
            }

            List<Dtos.AdminLog> result = logs.Select(log =>
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

            cache.Set("AdminLogs", result);
            return result;
        }

        public IEnumerable<Dtos.AdminLog> GetAdminLogsByActorName(string actorName)
        {
            if (cache.TryGetValue("AdminLogs", out IEnumerable<Dtos.AdminLog>? AdminLogs))
            {
                return AdminLogs!.Where(log => log.ActorName.Contains(actorName)).ToList();
            }
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
            if (cache.TryGetValue("AdminLogs", out IEnumerable<Dtos.AdminLog>? AdminLogs))
            {
                var updatedLogs = AdminLogs!.Where(l => l.Id != id).ToList();
                cache.Set("AdminLogs", updatedLogs);
            }
            return true;
        }

        public bool DeleteAdminLogs(TimeSpan? olderThan = null)
        {
            var logsQuery = db.AdminLogs.AsQueryable();

            if (olderThan.HasValue)
            {
                var cutoffDate = DateTime.UtcNow - olderThan.Value;
                logsQuery = logsQuery.Where(log => log.DoneAtUtc < cutoffDate);
            }

            var deletedIds = logsQuery.Select(l => l.Id).ToList();

            db.AdminLogs.RemoveRange(logsQuery);
            db.SaveChanges();

            if (cache.TryGetValue("AdminLogs", out IEnumerable<Dtos.AdminLog>? cachedLogs))
            {
                var updated = cachedLogs!
                    .Where(log => !deletedIds.Contains(log.Id))
                    .ToList();

                cache.Set("AdminLogs", updated);
            }

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
                TargetEntityType.Discussion =>
                    db.Discussions.Where(d => d.Id == id).Select(d => d.Title).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Post =>
                    db.Posts.Where(p => p.Id == id)
                        .Select(p => p.Discussion.Title)
                        .FirstOrDefault() is string discussionTitle && !string.IsNullOrEmpty(discussionTitle)
                        ? $"Post in Discussion: {discussionTitle}"
                        : "Unknown",
                TargetEntityType.LeaderboardLevel =>
                    db.LeaderboardLevels.Where(lb => lb.Id == id).Select(lb => lb.Name).FirstOrDefault() ?? "Unknown",
                TargetEntityType.Player =>
                    db.Players.Where(p => p.Id == id).Select(p => p.Username).FirstOrDefault() ?? "Unknown",
                _ => "Unknown",
            };
        }
    }
}
