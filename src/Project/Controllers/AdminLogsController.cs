using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("logs")]
    public class AdminLogsController : ControllerBase
    {
        private readonly AdminLogService adminLogService;

        public AdminLogsController(AdminLogService adminLogService)
        {
            this.adminLogService = adminLogService;
        }

        /// <summary>
        /// Get all admin logs.
        /// </summary>
        /// <returns>A list of admin logs.</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<AdminLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetAllAdminLogs()
        {
            var logs = adminLogService.GetAllAdminLogs();
            return Ok(logs);
        }

        /// <summary>
        /// Get admin logs by actor name.
        /// </summary>
        /// <param name="actorName">The name of the actor.</param>
        /// <returns>A list of admin logs.</returns>
        [HttpGet("actor/{actorName}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<AdminLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult GetAdminLogsByActorName(string actorName)
        {
            var logs = adminLogService.GetAdminLogsByActorName(actorName);
            return Ok(logs);
        }

        /// <summary>
        /// Delete a specific admin log by ID.
        /// </summary>
        /// <param name="id">The ID of the admin log to delete.</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult DeleteAdminLog(int id)
        {
            var result = adminLogService.DeleteAdminLog(id);
            if (result)
            {
                return NoContent();
            }
            return NotFound();
        }

        /// <summary>
        /// Delete admin logs by timespan or delete all if no timespan is provided.
        /// </summary>
        /// <param name="TimeSpan"> the timespan to filter logs for deletion (optional).</param>
        /// <returns>An IActionResult indicating the result of the operation.</returns>
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public IActionResult DeleteAdminLogs(TimeSpan? timeSpan)
        {
            var result = adminLogService.DeleteAdminLogs(timeSpan);
            if (result)
            {
                return NoContent();
            }
            return NotFound();
        }
    }
}
