using Microsoft.AspNetCore.Mvc;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        [HttpHead]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(new { status = "Healthy" });
        }
    }
}