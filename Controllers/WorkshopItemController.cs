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
        /// Get all workshop items.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<WorkshopItem>), StatusCodes.Status200OK)]
        public IActionResult GetAllItems(string? NameFilter)
        {
            var items = _service.GetAll(NameFilter);
            return Ok(items);
        }

        /// <summary>
        /// Get a specific workshop item and its content.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(WorkshopItem), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetById(int id)
        {
            WorkshopItem? item = _service.GetById(id);
            if (item is null)
                return NotFound($"Workshop item with ID {id} not found.");
            return Ok(item);
        }

        /// <summary>
        /// Create a new workshop item
        /// </summary>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Create([FromBody] JsonElement WorkshopItemJson)
        {
            WorkshopItem? item = _service.AddWorkshopItem(WorkshopItemJson);
            if (item is null)
                return BadRequest(new { message = "Invalid workshop item data." });
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        /// <summary>
        /// Rate a workshop item (1–5).
        /// </summary>
        [Authorize]
        [HttpPost("{WorkshopItemId:int}/rate/{Rating:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Rate(
            [FromRoute(Name = "WorkshopItemId")] int WorkshopItemId,
            [FromRoute(Name = "Rating")] int Rating)
        {
            Console.WriteLine("Debugging: Rate endpoint called");
            var playerId = int.Parse(User.FindFirst("id")!.Value);
            Console.WriteLine("Debug: PlayerId: " + playerId);
            Console.WriteLine("Params Debug: WorkshopItemId: " + WorkshopItemId + ", Rating: " + Rating);
            if (Rating < 1 || Rating > 5)
            {
                return BadRequest(new { message = "Rating value must be between 1 and 5." });
            }
            return _service.RateWorkshopItem(playerId, WorkshopItemId, Rating) ? 
                Ok() : NotFound();
        }

        /// <summary>
        /// Subscribe or unsubscribe from a workshop item.
        /// </summary>
        [Authorize]
        [HttpPost("{WorkshopItemId:int}/subscribe")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Subscribe(int WorkshopItemId)
        {
            int UserId = int.Parse(User.FindFirst("id")!.Value);
            var item = _service.GetById(WorkshopItemId);
            if (item is null)
            {
                return NotFound();
            }
            else {
                if (_service.IsUserSubscribed(UserId, WorkshopItemId))
                {
                    _service.UnsubscribeFromWorkshopItem(UserId, WorkshopItemId);
                    return Ok();
                }
                else
                {
                    _service.SubscribeToWorkshopItem(UserId, WorkshopItemId);
                    return Ok();
                }
            }
        }
    }
}
