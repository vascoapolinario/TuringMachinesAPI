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

        public IEnumerable<LevelSubmission> GetLeaderboard(string? LevelName, string? filter)
        {
            var SubmissionsQuery = db.LevelSubmissions.AsNoTracking();

            int? levelId = db.LeaderboardLevels
                .AsNoTracking()
                .Where(l => l.Name == LevelName)
                .Select(l => l.Id)
                .FirstOrDefault();


            if (levelId.HasValue)
            {
                SubmissionsQuery = SubmissionsQuery.Where(l => l.LeaderboardLevelId == levelId);
            }

            var SubmissionItems = SubmissionsQuery.ToList();
            if (filter.IsNullOrEmpty())
            {
                SubmissionItems.OrderBy(s => s.Time);
            }
            else
            {
                if (filter!.ToLower().Equals("nodes"))
                {
                    SubmissionItems.OrderBy(s => s.NodeCount);
                }
                else if (filter.ToLower().Equals("connections"))
                {
                    SubmissionItems.OrderBy(s => s.ConnectionCount);
                }
            }

            return (IEnumerable<LevelSubmission>)SubmissionItems;
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

            db.LevelSubmissions.Add(newItem);
            db.SaveChanges();

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
    }
}
