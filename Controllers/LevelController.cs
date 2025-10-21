using Microsoft.AspNetCore.Mvc;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;

namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LevelsController : ControllerBase
    {
        private readonly ILogger<LevelsController> _logger;
        private readonly LevelService _LevelService;

        public LevelsController(ILogger<LevelsController> logger, LevelService LevelService)
        {
            _logger = logger;
            _LevelService = LevelService;
        }

        /// <summary>
        /// Retorna todos os níveis disponíveis (oficiais ou de workshop).
        /// </summary>
        [HttpGet(Name = "GetLevels")]
        [ProducesResponseType(typeof(IEnumerable<Level>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Level>> GetLevels(string? nameFilter)
        {
            return Ok(_LevelService.GetAllLevels(nameFilter));
        }

        /// <summary>
        /// Retorna um nível específico pelo ID.
        /// </summary>
        [HttpGet("{id}", Name = "GetLevel")]
        [ProducesResponseType(typeof(Level), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Level> GetLevel(int id)
        {
            if (_LevelService.GetLevelById(id) is Level level)
            {
                return Ok(level);
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Adiciona um novo nível ao repositório (sem autenticação por agora).
        /// </summary>
        [HttpPost(Name = "AddLevel")]
        [ProducesResponseType(typeof(Level), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<Level> AddLevel([FromBody] string newLevelData)
        {
            if (string.IsNullOrWhiteSpace(newLevelData))
            {
                return BadRequest("Level data cannot be empty.");
            }
            Level createdLevel = _LevelService.AddLevel(newLevelData);
            return CreatedAtRoute("GetLevel", new { id = createdLevel.Id }, createdLevel);
        }
    }
}
