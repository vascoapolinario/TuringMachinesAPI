using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("workshop")]
    public class WorkshopItemController : ControllerBase
    {
        private readonly WorkshopItemService _service;

        public WorkshopItemController(WorkshopItemService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all workshop items (summary).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WorkshopItem>), StatusCodes.Status200OK)]
        public IActionResult GetAll()
        {
            var items = _service.GetAll();
            return Ok(items);
        }

        /// <summary>
        /// Get a specific workshop item and its content.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(int id)
        {
            var (item, content) = _service.GetById(id);
            if (item == null) return NotFound();
            return Ok(new { item, content });
        }

        /// <summary>
        /// Create a new workshop item (Level only for now).
        /// </summary>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        public IActionResult Create([FromBody] JsonElement levelJson)
        {
            if (levelJson.ValueKind == JsonValueKind.Undefined || levelJson.ValueKind == JsonValueKind.Null)
                return BadRequest("Invalid or empty JSON payload.");

            var authorId = int.Parse(User.FindFirst("id")!.Value);
            var (item, level) = _service.CreateWorkshopLevel(levelJson, authorId);

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new { item, level });
        }

        /// <summary>
        /// Rate a workshop item (1–5).
        /// </summary>
        [Authorize]
        [HttpPost("{id:int}/rate/{value:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Rate(int id, int value)
        {
            var userId = int.Parse(User.FindFirst("id")!.Value);
            var ok = _service.AddOrUpdateRating(id, userId, value);
            return ok ? Ok(new { success = true }) : NotFound();
        }

        /// <summary>
        /// Subscribe or unsubscribe from a workshop item.
        /// </summary>
        [Authorize]
        [HttpPost("{id:int}/subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Subscribe(int id)
        {
            var playerId = int.Parse(User.FindFirst("id")!.Value);
            var ok = _service.ToggleSubscription(id, playerId);
            return ok ? Ok(new { success = true }) : NotFound();
        }
    }
}
