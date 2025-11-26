using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("leaderboard")]

    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardService leaderboardService;
        private readonly AdminLogService adminLogService;

        public LeaderboardController(LeaderboardService leaderboardService, AdminLogService adminLogService)
        {
            this.leaderboardService = leaderboardService;
            this.adminLogService = adminLogService;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<LevelSubmission>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetLeaderboard(bool? Player, string? levelName)
        {
            if (Player == null || Player == false)
            {
                return Ok(leaderboardService.GetLeaderboard(levelName));
            }
            else
            {
                int PlayerId = int.Parse(User.FindFirst("id")!.Value);
                return Ok(leaderboardService.GetPlayerLeaderboard(PlayerId, levelName));
            }
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(LevelSubmission), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddSubmission([FromBody] LevelSubmission submission)
        {
            int PlayerId = int.Parse(User.FindFirst("id")!.Value);
            LevelSubmission? Lvlsubmission = leaderboardService.AddSubmission(PlayerId, submission.LevelName, submission.Time, submission.NodeCount, submission.ConnectionCount);
            if (Lvlsubmission == null)
            {
                return BadRequest("Invalid submission data.");
            }
            await adminLogService.CreateAdminLog(PlayerId, Enums.ActionType.Create, Enums.TargetEntityType.LeaderboardSubmission, Lvlsubmission.Id);
            return Ok(Lvlsubmission);
        }

        [HttpPost("level")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(LeaderboardLevel), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> AddLevel([FromBody] LeaderboardLevel level)
        {
            LeaderboardLevel? AddedLevel = leaderboardService.AddLeaderboardLevel(level.Name, level.Category, level.WorkshopItemId);
            if (AddedLevel == null)
            {
                return BadRequest("Invalid level data.");
            }
            await adminLogService.CreateAdminLog(int.Parse(User.FindFirst("id")!.Value), Enums.ActionType.Create, Enums.TargetEntityType.LeaderboardLevel, AddedLevel.Id);
            return Ok(AddedLevel);
        }

        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> DeleteSubmission(string playerName, string levelName)
        {
            if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(levelName))
            {
                return BadRequest("Player name and level name must be provided.");
            }
            
            var success = leaderboardService.DeletePlayerSubmission(playerName, levelName);
            if (!success)
            {
                return NotFound("Submission not found.");
            }
            else
            {
                await adminLogService.CreateAdminLog(int.Parse(User.FindFirst("id")!.Value), Enums.ActionType.Delete, Enums.TargetEntityType.LeaderboardSubmission, 0);
                return NoContent();
            }
        }
    }
}
