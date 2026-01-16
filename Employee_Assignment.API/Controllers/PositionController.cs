using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _service;
        private readonly ILogger<PositionsController> _logger;

        public PositionsController(
            IPositionService service,
            ILogger<PositionsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous] // Allow anonymous access to get positions for dropdowns
        public async Task<IActionResult> GetPositions()
        {
            _logger.LogInformation("API: Get all positions called");
            var positions = await _service.GetAllPositionsAsync();
            return Ok(positions);
        }
    }
}