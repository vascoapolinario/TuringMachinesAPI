using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TuringMachinesAPI.Dtos;
using TuringMachinesAPI.Services;



namespace TuringMachinesAPI.Controllers
{
    [ApiController]
    [Route("larps")]
    public class LarpController
    {
        public LarpService larpService;

        public LarpController(LarpService larpService)
        {
            this.larpService = larpService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLarps()
        {
            var larps = larpService.GetAllLarps();
            return new OkObjectResult(larps);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLarp(string reason, int authorNumber)
        {
            var larp = larpService.CreateLarp(reason, authorNumber);
            if (larp == null)
            {
                return new BadRequestObjectResult(new { error = "Invalid author number" });
            }
            return new OkObjectResult(larp);
        }
    }
}
