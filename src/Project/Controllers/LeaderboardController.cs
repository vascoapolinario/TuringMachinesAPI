using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("leaderboard")]

    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardService leaderboardService;

        public LeaderboardController(LeaderboardService leaderboardService)
        {
            this.leaderboardService = leaderboardService;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<LevelSubmission>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetLeaderboard(bool? Player, string? levelName, string? filter)
        {
            if (Player == null)
            {
                return Ok(leaderboardService.GetLeaderboard(levelName, filter));
            }
            else
            {
                int PlayerId = int.Parse(User.FindFirst("id")!.Value);
                return Ok(leaderboardService.GetPlayerLeaderboard(PlayerId, levelName, filter));
            }
        }

        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(LevelSubmission), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult AddSubmission([FromBody] LevelSubmission submission)
        {
            int PlayerId = int.Parse(User.FindFirst("id")!.Value);
            return Ok(leaderboardService.AddSubmission(PlayerId, submission.LevelName, submission.Time, submission.NodeCount, submission.ConnectionCount));
        }
    }
}
