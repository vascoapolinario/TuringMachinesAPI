using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Dtos;


namespace TuringMachinesAPI.Services
{
    public class LeaderboardService
    {
        private readonly TuringMachinesDbContext db;

        public LeaderboardService(TuringMachinesDbContext context)
        {
            this.db = context;
        }

        public IEnumerable<LevelSubmission> GetLeaderboard(string? levelName, string? filter)
        {
            var query =
                from s in db.LevelSubmissions.AsNoTracking()
                join p in db.Players.AsNoTracking() on s.PlayerId equals p.Id
                join lvl in db.LeaderboardLevels.AsNoTracking() on s.LeaderboardLevelId equals lvl.Id
                select new
                {
                    Submission = s,
                    PlayerName = p.Username,
                    LevelName = lvl.Name
                };

            if (!string.IsNullOrEmpty(levelName))
            {
                query = query.Where(x => x.LevelName == levelName);
            }

            var normalizedFilter = filter?.ToLowerInvariant();
            query = normalizedFilter switch
            {
                "nodes" => query.OrderBy(x => x.Submission.NodeCount),
                "connections" => query.OrderBy(x => x.Submission.ConnectionCount),
                _ => query.OrderBy(x => x.Submission.Time)
            };

            var result = query
                .Select(x => new LevelSubmission
                {
                    LevelName = x.LevelName,
                    PlayerName = x.PlayerName,
                    Time = x.Submission.Time,
                    NodeCount = x.Submission.NodeCount,
                    ConnectionCount = x.Submission.ConnectionCount
                })
                .ToList();

            return result;
        }


        public IEnumerable<LevelSubmission> GetPlayerLeaderboard(int playerId, string? LevelName, string? Filter)
        {
            string? playerName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId)
                .Select(p => p.Username)
                .FirstOrDefault();

            IEnumerable<LevelSubmission> Leaderboard = GetLeaderboard(LevelName, Filter);

            var FilteredLeaderboard = Leaderboard.Where(l => l.PlayerName == playerName);
            return FilteredLeaderboard;
        }

        public LevelSubmission? AddSubmission(int playerId, string levelName, double time, int nodeCount, int connectionCount)
        {
            var levelId = db.LeaderboardLevels
                .AsNoTracking()
                .Where(l => l.Name.Equals(levelName))
                .Select(l => l.Id)
                .FirstOrDefault();

            if (levelId == 0)
            {
                return null;
            }

            var newItem = new Entities.LevelSubmission
            {
                PlayerId = playerId,
                LeaderboardLevelId = levelId,
                Time = time,
                NodeCount = nodeCount,
                ConnectionCount = connectionCount
            };

            if (db.LevelSubmissions.Any(s => s.PlayerId == playerId && s.LeaderboardLevelId == levelId))
            {
                var existingItem = db.LevelSubmissions
                    .First(s => s.PlayerId == playerId && s.LeaderboardLevelId == levelId);
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

            string? playerName = db.Players
                .AsNoTracking()
                .Where(p => p.Id == playerId)
                .Select(p => p.Username)
                .FirstOrDefault();

            return new LevelSubmission
            {
                LevelName = levelName,
                PlayerName = playerName!,
                Time = time,
                NodeCount = nodeCount,
                ConnectionCount = connectionCount
            };
        }

        public LeaderboardLevel? AddLeaderboardLevel(string name, string category, int? workshopItemId)
        {
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
            return new LeaderboardLevel
            {
                Id = newLevel.Id,
                Name = newLevel.Name,
                Category = newLevel.Category,
                WorkshopItemId = newLevel.WorkshopItemId
            };
        }
    }
}
