using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Utils;


namespace TuringMachinesAPI.Services
{
    public class LeaderboardService
    {
        private readonly TuringMachinesDbContext db;
        private readonly IMemoryCache cache;

        public LeaderboardService(TuringMachinesDbContext context, IMemoryCache memoryCache)
        {
            this.db = context;
            this.cache = memoryCache;
        }

        public IEnumerable<LevelSubmission> GetLeaderboard(string? levelName = null)
        {
            if (!cache.TryGetValue("Leaderboard", out List<LevelSubmission>? leaderboard))
            {
                leaderboard = (
                    from s in db.LevelSubmissions.AsNoTracking()
                    join p in db.Players.AsNoTracking() on s.PlayerId equals p.Id
                    join lvl in db.LeaderboardLevels.AsNoTracking() on s.LeaderboardLevelId equals lvl.Id
                    orderby s.NodeCount, s.ConnectionCount, s.Time
                    select new LevelSubmission
                    {
                        Id = s.Id,
                        LevelName = lvl.Name,
                        PlayerName = p.Username,
                        Time = s.Time,
                        NodeCount = s.NodeCount,
                        ConnectionCount = s.ConnectionCount
                    }
                ).ToList();

                cache.Set("Leaderboard", leaderboard);
            }

            if (string.IsNullOrEmpty(levelName))
                return leaderboard!;

            return leaderboard!.Where(ls =>
                ls.LevelName.Contains(levelName, StringComparison.OrdinalIgnoreCase)
            );
        }



        public IEnumerable<LevelSubmission> GetPlayerLeaderboard(int playerId, string? LevelName = null)
        {
            if (cache.TryGetValue("Leaderboard", out IEnumerable<LevelSubmission>? cachedLeaderboard))
            {
                string? cachedPlayerName = CacheHelper.GetPlayerFromCacheById(cache, playerId)?.Username;

                if (cachedPlayerName == null)
                {
                    {
                        string? Name = db.Players
                            .AsNoTracking()
                            .Where(p => p.Id == playerId)
                            .Select(p => p.Username)
                            .FirstOrDefault();
                        cachedPlayerName = Name;
                    }
                }

                var FilteredCachedLeaderboard = cachedLeaderboard!.Where(l => l.PlayerName == cachedPlayerName);
                if (string.IsNullOrEmpty(LevelName))
                {
                    return FilteredCachedLeaderboard;
                }
                return FilteredCachedLeaderboard.Where(l => l.LevelName.ToLower().Contains(LevelName.ToLower()));
            }

            string? playerName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId)
                .Select(p => p.Username)
                .FirstOrDefault();

            IEnumerable<LevelSubmission> Leaderboard = GetLeaderboard(LevelName);

            var FilteredLeaderboard = Leaderboard.Where(l => l.PlayerName == playerName);
            return FilteredLeaderboard;
        }

        public LevelSubmission? AddSubmission(int playerId, string levelName, double time, int nodeCount, int connectionCount)
        {
            int levelId = 0;
            if (cache.TryGetValue("LeaderboardLevels", out IEnumerable<LeaderboardLevel>? cachedLevels))
            {
                var lvl = cachedLevels!.FirstOrDefault(l => l.Name.Equals(levelName));
                if (lvl != null) levelId = lvl.Id;
            }
            if (levelId == 0)
            {
                levelId = db.LeaderboardLevels.AsNoTracking()
                                              .Where(l => l.Name.Equals(levelName))
                                              .Select(l => l.Id)
                                              .FirstOrDefault();
                if (levelId == 0)
                    return null;
            }

            string? playerName = null;
            if (cache.TryGetValue("Players", out IEnumerable<Player>? cachedPlayers))
            {
                playerName = cachedPlayers!.FirstOrDefault(p => p.Id == playerId)?.Username;
            }
            playerName ??= db.Players.AsNoTracking()
                                     .Where(p => p.Id == playerId)
                                     .Select(p => p.Username)
                                     .FirstOrDefault();
            if (playerName == null)
                return null;

            var newItem = new Entities.LevelSubmission
            {
                PlayerId = playerId,
                LeaderboardLevelId = levelId,
                Time = time,
                NodeCount = nodeCount,
                ConnectionCount = connectionCount
            };

            var existingItem = db.LevelSubmissions
                .FirstOrDefault(s => s.PlayerId == playerId && s.LeaderboardLevelId == levelId);

            if (existingItem != null)
            {
                if (existingItem.NodeCount < nodeCount ||
                    (existingItem.NodeCount == nodeCount && existingItem.ConnectionCount < connectionCount) ||
                    (existingItem.NodeCount == nodeCount && existingItem.ConnectionCount == connectionCount && existingItem.Time <= time))
                {
                    return null;
                }

                existingItem.Time = time;
                existingItem.NodeCount = nodeCount;
                existingItem.ConnectionCount = connectionCount;
                db.SaveChanges();

                newItem = existingItem;
            }
            else
            {
                db.LevelSubmissions.Add(newItem);
                db.SaveChanges();
            }

            if (cache.TryGetValue("Leaderboard", out IEnumerable<LevelSubmission>? cachedLeaderboard))
            {
                var updated = cachedLeaderboard!.ToList();

                updated.RemoveAll(x => x.PlayerName == playerName && x.LevelName == levelName);

                updated.Add(new LevelSubmission
                {
                    Id = newItem.Id,
                    LevelName = levelName,
                    PlayerName = playerName,
                    Time = time,
                    NodeCount = nodeCount,
                    ConnectionCount = connectionCount
                });

                updated = updated.OrderBy(x => x.NodeCount)
                                 .ThenBy(x => x.ConnectionCount)
                                 .ThenBy(x => x.Time)
                                 .ToList();

                cache.Set("Leaderboard", updated);
            }

            return new LevelSubmission
            {
                Id = newItem.Id,
                LevelName = levelName,
                PlayerName = playerName,
                Time = time,
                NodeCount = nodeCount,
                ConnectionCount = connectionCount
            };
        }


        public LeaderboardLevel? AddLeaderboardLevel(string name, string category, int? workshopItemId = null)
        {
            if (cache.TryGetValue("LeaderboardLevels", out IEnumerable<LeaderboardLevel>? cachedLevels))
            {
                if (cachedLevels!.Any(l => l.Name.Equals(name)))
                {
                    return null;
                }
            }
            var existingLevel = db.LeaderboardLevels
                .AsNoTracking()
                .FirstOrDefault(l => l.Name.Equals(name));
            if (existingLevel != null)
            {
                return null;
            }
            var newLevel = new Entities.LeaderboardLevel
            {
                Name = name,
                Category = category,
                WorkshopItemId = workshopItemId
            };
            db.LeaderboardLevels.Add(newLevel);
            db.SaveChanges();

            if (cache.TryGetValue("LeaderboardLevels", out IEnumerable<LeaderboardLevel>? cachedLevelsAfterAdd))
            {
                var updatedCachedLevels = cachedLevelsAfterAdd!.ToList();
                updatedCachedLevels.Add(new LeaderboardLevel
                {
                    Id = newLevel.Id,
                    Name = newLevel.Name,
                    Category = newLevel.Category,
                    WorkshopItemId = newLevel.WorkshopItemId
                });
                cache.Set("LeaderboardLevels", updatedCachedLevels);
            }

            return new LeaderboardLevel
            {
                Id = newLevel.Id,
                Name = newLevel.Name,
                Category = newLevel.Category,
                WorkshopItemId = newLevel.WorkshopItemId
            };
        }

        public bool DeletePlayerSubmission(string playerName, string levelName)
        {
            int inputPlayerId = 0;
            int inputLevelId = 0;

            if (cache.TryGetValue("Players", out IEnumerable<Player>? cachedPlayers))
            {
                var player = cachedPlayers!.FirstOrDefault(p => p.Username.Equals(playerName));
                if (player != null)
                {
                    inputPlayerId = player.Id;
                }
            }
            else
            {
                var player = db.Players
                    .AsNoTracking()
                    .FirstOrDefault(p => p.Username.Equals(playerName));
                if (player != null)
                {
                    inputPlayerId = player.Id;
                }
            }

            if (cache.TryGetValue("LeaderboardLevels", out IEnumerable<LeaderboardLevel>? cachedLevels))
            {
                if (cachedLevels!.Any(l => l.Name.Equals(levelName)))
                {
                    var level = cachedLevels!.First(l => l.Name.Equals(levelName));
                    inputLevelId = level.Id;
                }
            }
            else
            {
                var level = db.LeaderboardLevels
                    .AsNoTracking()
                    .FirstOrDefault(l => l.Name.Equals(levelName));
                if (level != null)
                {
                    inputLevelId = level.Id;
                }
            }

            var existingSubmission = db.LevelSubmissions
                .FirstOrDefault(s => s.PlayerId == inputPlayerId && s.LeaderboardLevelId == inputLevelId);

            if (existingSubmission == null)
                return false;

            db.LevelSubmissions.Remove(existingSubmission);
            db.SaveChanges();

            if (cache.TryGetValue("Leaderboard", out IEnumerable<LevelSubmission>? cachedLeaderboard))
            {
                var updated = cachedLeaderboard!.ToList();
                updated.RemoveAll(l =>
                    l.PlayerName == playerName &&
                    l.LevelName == levelName);
                updated = updated.OrderBy(x => x.NodeCount)
                     .ThenBy(x => x.ConnectionCount)
                     .ThenBy(x => x.Time)
                     .ToList();

                cache.Set("Leaderboard", updated);
            }

            return true;
        }

        public int? GetSubmissionId(string playerName, string levelName)
        {
            if (cache.TryGetValue("Leaderboard", out IEnumerable<LevelSubmission>? cachedLeaderboard))
            {
                var submission = cachedLeaderboard!
                    .FirstOrDefault(ls => ls.PlayerName.Equals(playerName) && ls.LevelName.Equals(levelName));
                if (submission != null)
                {
                    return submission.Id;
                }
            }

            var submissionId = (from s in db.LevelSubmissions.AsNoTracking()
                                join p in db.Players.AsNoTracking() on s.PlayerId equals p.Id
                                join lvl in db.LeaderboardLevels.AsNoTracking() on s.LeaderboardLevelId equals lvl.Id
                                where p.Username.Equals(playerName) && lvl.Name.Equals(levelName)
                                select s.Id).FirstOrDefault();
            if (submissionId == 0)
            {
                return null;
            }
            return submissionId;
        }
    }
}
