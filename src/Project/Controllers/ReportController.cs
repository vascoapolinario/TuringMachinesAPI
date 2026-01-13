using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TuringMachinesAPI.Enums;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("reports")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService reportService;
        private readonly DiscordWebhookService discordWebhookService;

        public ReportController(ReportService reportService, DiscordWebhookService discordWebhookService)
        {
            this.reportService = reportService;
            this.discordWebhookService = discordWebhookService;
        }

        /// <summary>
        /// Get All Reports
        /// </summary>
        /// <returns>A IEnumerable of Reports</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<Dtos.Report>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetAllReports()
        {
            var reports = reportService.GetAllReports();
            return new OkObjectResult(reports);
        }

        /// <summary>
        /// Submit a new report, no role needed but authorize required.
        /// </summary>
        /// <param name="incomingReport">The report to submit</param>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SubmitReport([FromBody] Dtos.IncomingReport incomingReport)
        {
            var playerIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(playerIdClaim) || !int.TryParse(playerIdClaim, out int playerId))
            {
                return Unauthorized("Player ID not found in token.");
            }

            if (reportService.ReportAlreadyExists(playerId, incomingReport))
            {
                return BadRequest("You have already submitted this report.");
            }

            var playerName = User.Identity?.Name
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (playerName == null)
            {
                return Unauthorized("Player name not found in token.");
            }

            if (playerName.Equals(incomingReport.ReportedPlayerName, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("You cannot report yourself.");
            }

            var report = reportService.CreateReport(playerId, incomingReport);

            if (report == null)
            {
                return NotFound("Reported player or item not found.");
            }

            await discordWebhookService.NotifyNewReport(report);
            return CreatedAtAction(nameof(GetAllReports), new { id = report.Id }, report);
        }

        /// <summary>
        /// Change the status of a report
        /// </summary>
        /// <param name="reportId">The ID of the report to update</param>
        /// <param name="status">The new status of the report</param>
        [HttpPut("{reportId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdateReportStatus(int reportId, [FromBody] ReportStatus status)
        {
            if (!Enum.IsDefined(typeof(ReportStatus), status))
            {
                return BadRequest("Invalid report status.");
            }

            var updated = reportService.ChangeReportStatus(reportId, status);
            if (updated == null)
            {
                return NotFound("Report not found.");
            }
            return Ok(updated);
        }

        /// <summary>
        /// Delete a report by ID
        /// </summary>
        /// <param name="reportId">The ID of the report to delete</param>
        [HttpDelete("{reportId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteReport(int reportId)
        {
            var deleted = reportService.DeleteReport(reportId);
            if (!deleted)
            {
                return NotFound("Report not found.");
            }
            return NoContent();
        }
    }
}
