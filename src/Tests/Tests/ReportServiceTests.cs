using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using TuringMachinesAPI.DataSources;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;
using TuringMachinesAPI.Utils;
using Xunit;
using Dtos = TuringMachinesAPI.Dtos;
using Entities = TuringMachinesAPI.Entities;

namespace TuringMachinesAPITests.Tests
{
    [Collection("SequentialTests")]
    public sealed class ReportServiceTests : IDisposable
    {
        private readonly TestApplicationDomain applicationDomain;
        private readonly ReportService service;

        public ReportServiceTests()
        {
            applicationDomain = new TestApplicationDomain();

            string? connectionString = applicationDomain.configuration.GetConnectionString("DefaultConnection");
            Assert.NotNull(connectionString);

            applicationDomain.Services.AddDbContext<TuringMachinesDbContext>(o => o.UseNpgsql(connectionString));
            applicationDomain.Services.AddScoped<ReportService>();

            var provider = applicationDomain.ServiceProvider;
            service = provider.GetRequiredService<ReportService>();

            string? backupPath = applicationDomain.configuration.GetValue<string>("TestsDbBackup:FilePath");
            if (backupPath == null)
                throw new Exception("Não foi possível obter o caminho do ficheiro de configuração.");

            string sql = File.ReadAllText(backupPath);

            using (IServiceScope scope = applicationDomain.ServiceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TuringMachinesDbContext>();
                db.Database.Migrate();
                db.Database.ExecuteSqlRaw(sql);
                db.SaveChanges();
            }
        }

        public void Dispose()
        {
            applicationDomain.Dispose();
        }

        [Fact]
        public void CreateReport_ShouldCreateSuccessfully()
        {
            var report = service.CreateReport(1, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Bob",
                Reason = "Test Reporting a player"
            });

            Assert.NotNull(report);
            Assert.Equal("Alice", report.ReportingUserName);
            Assert.Equal("Player", report.ReportedItemType);
            Assert.Equal("Bob", report.ReportedUserName);
            Assert.Equal("Test Reporting a player", report.Reason);
        }

        [Fact]
        public void GetAllReports_ShouldReturnReports()
        {
            var reports = service.GetAllReports();
            Assert.Empty(reports);

            var createdReport = service.CreateReport(1, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Judy",
                ReportedItemId = 10,
                Reason = "Test Reporting a player"
            });

            var createdReport2 = service.CreateReport(2, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Ivan",
                Reason = "Test Reporting a player"
            });

            reports = service.GetAllReports().ToList();
            Assert.Equal(2, reports.Count());
            Assert.Contains(reports, r => r.Id == createdReport!.Id);
            Assert.Contains(reports, r => r.Id == createdReport2!.Id);
        }

        [Fact]
        public void CreateReport_ShouldReturnNullForNonExistentReportingPlayer()
        {
            var report = service.CreateReport(999, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Bob",
                Reason = "Test Reporting a player"
            });
            Assert.Null(report);
        }

        [Fact]
        public void CreateReport_ShouldReturnNullForNonExistentReportedPlayer()
        {
            var report = service.CreateReport(1, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "NonExistentPlayer",
                Reason = "Test Reporting a non-existent player"
            });
            Assert.Null(report);
        }

        [Fact]
        public void ChangeReportStatus_ShouldChangeSuccessfully()
        {
            var report = service.CreateReport(1, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Bob",
                Reason = "Test Reporting a player"
            });

            Assert.NotNull(report);
            Assert.Equal(ReportStatus.Open.ToString(), report.Status);

            service.ChangeReportStatus(report.Id, ReportStatus.Resolved);
            var updatedReports = service.GetAllReports();
            var updatedReport = updatedReports.First(r => r.Id == report.Id);
            Assert.Equal(ReportStatus.Resolved.ToString(), updatedReport.Status);
        }

        [Fact]
        public void ReportAlreadyExists_ShouldReturnTrueIfExists()
        {
            var incomingReport = new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Bob",
                Reason = "Test Reporting a player"
            };
            var report = service.CreateReport(1, incomingReport);
            Assert.NotNull(report);
            bool exists = service.ReportAlreadyExists(1, incomingReport);
            Assert.True(exists);
            bool notExists = service.ReportAlreadyExists(2, incomingReport);
            Assert.False(notExists);
        }

        [Fact]
        public void DeleteReport_ShouldSucessfulyDelete()
        {
            var report = service.CreateReport(1, new Dtos.IncomingReport
            {
                ReportType = ReportType.Player.ToString(),
                ReportedPlayerName = "Bob",
                Reason = "Test Reporting a player"
            });

            Assert.NotNull(report);
            service.DeleteReport(report.Id);
            var reports = service.GetAllReports();
            Assert.DoesNotContain(reports, r => r.Id == report.Id);
        }
    }
}