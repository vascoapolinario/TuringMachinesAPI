using Microsoft.Extensions.Caching.Memory;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Utils;

namespace TuringMachinesAPI.Services
{
    public class ReportService
    {
        private readonly TuringMachinesDbContext db;
        private readonly IMemoryCache cache;

        public ReportService(TuringMachinesDbContext db, IMemoryCache cache)
        {
            this.db = db;
            this.cache = cache;
        }

        public IEnumerable<Dtos.Report> GetAllReports()
        {
            if (cache.TryGetValue("Reports", out IEnumerable<Dtos.Report>? cachedReports))
            {
                if (cachedReports is not null)
                {
                    return cachedReports;
                }
            }

            var reports = db.Reports
                .Select(r => new Dtos.Report
                {
                    Id = r.Id,
                    ReportingUserName = r.ReportingUserName,
                    ReportedItemType = r.ReportedItemType.ToString(),
                    ReportedUserName = r.ReportedUserName,
                    ReportedItemId = r.ReportedItemId,
                    Reason = r.Reason,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

            cache.Set("Reports", reports);
            return reports;
        }

        public Dtos.Report? CreateReport(int playerId, Dtos.IncomingReport incomingReport)
        {
            var reportingPlayer = db.Players.Find(playerId);
            if (reportingPlayer is null)
            {
                return null;
            }

            if (incomingReport.ReportType == "Player" && incomingReport.ReportedItemId == null)
            {
                var reportedPlayer = db.Players
                    .Where(p => p.Username == incomingReport.ReportedPlayerName)
                    .Select(p => p.Id)
                    .FirstOrDefault();
                if (reportedPlayer == 0)
                {
                    return null;
                }
                incomingReport.ReportedItemId = reportedPlayer;
            }
            else if (incomingReport.ReportedItemId == null)
            {
                return null;
            }

            var reportEntity = new Entities.Report
            {
                ReportingUserName = reportingPlayer.Username,
                ReportedItemType = Enum.TryParse<Enums.ReportType>(incomingReport.ReportType, out var reportType)
                    ? reportType
                    : Enums.ReportType.Other,
                ReportedUserName = incomingReport.ReportedPlayerName,
                ReportedUserId = db.Players
                    .Where(p => p.Username == incomingReport.ReportedPlayerName)
                    .Select(p => p.Id)
                    .FirstOrDefault(),
                ReportedItemId = incomingReport.ReportedItemId!.Value,
                Reason = incomingReport.Reason,
                Status = Enums.ReportStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Reports.Add(reportEntity);
            db.SaveChanges();

            if (cache.TryGetValue("Reports", out IEnumerable<Dtos.Report>? cachedReports))
            {
                if (cachedReports is not null)
                {
                    var updatedReports = cachedReports.ToList();
                    updatedReports.Add(new Dtos.Report
                    {
                        Id = reportEntity.Id,
                        ReportingUserName = reportEntity.ReportingUserName,
                        ReportedItemType = reportEntity.ReportedItemType.ToString(),
                        ReportedUserName = reportEntity.ReportedUserName,
                        ReportedItemId = reportEntity.ReportedItemId,
                        Reason = reportEntity.Reason,
                        Status = reportEntity.Status.ToString(),
                        CreatedAt = reportEntity.CreatedAt,
                        UpdatedAt = reportEntity.UpdatedAt
                    });
                    cache.Set("Reports", updatedReports);
                }
            }

            Dtos.Report Report = new Dtos.Report
            {
                Id = reportEntity.Id,
                ReportingUserName = reportEntity.ReportingUserName,
                ReportedItemType = reportEntity.ReportedItemType.ToString(),
                ReportedUserName = reportEntity.ReportedUserName,
                ReportedItemId = reportEntity.ReportedItemId,
                Reason = reportEntity.Reason,
                Status = reportEntity.Status.ToString(),
                CreatedAt = reportEntity.CreatedAt,
                UpdatedAt = reportEntity.UpdatedAt
            };

            return Report;
        }

        public Dtos.Report? ChangeReportStatus(int reportId, Enums.ReportStatus newStatus)
        {
            var reportEntity = db.Reports.Find(reportId);
            if (reportEntity is null)
            {
                return null;
            }
            reportEntity.Status = newStatus;
            reportEntity.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            if (cache.TryGetValue("Reports", out IEnumerable<Dtos.Report>? cachedReports))
            {
                if (cachedReports is not null)
                {
                    var updatedReports = cachedReports.ToList();
                    var reportDto = updatedReports.FirstOrDefault(r => r.Id == reportId);
                    if (reportDto is not null)
                    {
                        reportDto.Status = newStatus.ToString();
                        reportDto.UpdatedAt = reportEntity.UpdatedAt;
                        cache.Set("Reports", updatedReports);
                    }
                }
            }

            Dtos.Report Report = new Dtos.Report
            {
                Id = reportEntity.Id,
                ReportingUserName = reportEntity.ReportingUserName,
                ReportedItemType = reportEntity.ReportedItemType.ToString(),
                ReportedUserName = reportEntity.ReportedUserName,
                ReportedItemId = reportEntity.ReportedItemId,
                Reason = reportEntity.Reason,
                Status = reportEntity.Status.ToString(),
                CreatedAt = reportEntity.CreatedAt,
                UpdatedAt = reportEntity.UpdatedAt
            };
            return Report;
        }

        public bool DeleteReport(int reportId)
        {
            var reportEntity = db.Reports.Find(reportId);
            if (reportEntity is null)
            {
                return false;
            }
            db.Reports.Remove(reportEntity);
            db.SaveChanges();
            if (cache.TryGetValue("Reports", out IEnumerable<Dtos.Report>? cachedReports))
            {
                if (cachedReports is not null)
                {
                    var updatedReports = cachedReports.Where(r => r.Id != reportId).ToList();
                    cache.Set("Reports", updatedReports);
                }
            }
            return true;
        }

        public bool ReportAlreadyExists(int playerId, Dtos.IncomingReport incomingReport)
        {
            string reportingPlayerName = CacheHelper.GetPlayerFromCacheById(cache, playerId)?.Username ?? db.Players.Find(playerId)?.Username ?? string.Empty;

            if (cache.TryGetValue("Reports", out IEnumerable<Dtos.Report>? cachedReports) && cachedReports is not null)
            {
                return cachedReports.Any(r =>
                    r.ReportingUserName == reportingPlayerName &&
                    r.ReportedItemType == incomingReport.ReportType &&
                    r.ReportedUserName == incomingReport.ReportedPlayerName &&
                    r.ReportedItemId == incomingReport.ReportedItemId);
            }
            else
            {
                if (!Enum.TryParse<Enums.ReportType>(incomingReport.ReportType, out var reportType))
                {
                    return false;
                }

                return db.Reports.Any(r =>
                    r.ReportingUserName == reportingPlayerName &&
                    r.ReportedItemType == reportType &&
                    r.ReportedUserName == incomingReport.ReportedPlayerName &&
                    r.ReportedItemId == incomingReport.ReportedItemId);
            }
        }
    }
}
