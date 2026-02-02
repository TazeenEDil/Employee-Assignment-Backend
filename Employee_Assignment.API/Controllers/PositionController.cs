using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.Position;
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
        [AllowAnonymous] 
        public async Task<IActionResult> GetPositions()
        {
            _logger.LogInformation("API: Get all positions called");
            var positions = await _service.GetAllPositionsAsync();
            return Ok(positions);
        }

        [HttpGet("paginated")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetPositionsPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return BadRequest(new { message = "Page number and page size must be greater than 0" });
                }

                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = await _service.GetPositionsPaginatedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated positions");
                return StatusCode(500, new { message = "An error occurred while retrieving positions" });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetPosition(int id)
        {
            try
            {
                _logger.LogInformation("API: Get position {PositionId}", id);
                var position = await _service.GetPositionByIdAsync(id);
                return Ok(position);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving position {PositionId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the position" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePosition([FromBody] CreatePositionDto dto)
        {
            try
            {
                _logger.LogInformation("API: Create position request");
                var position = await _service.CreatePositionAsync(dto);
                return CreatedAtAction(nameof(GetPosition), new { id = position.PositionId }, position);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position");
                return StatusCode(500, new { message = "An error occurred while creating the position" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] UpdatePositionDto dto)
        {
            try
            {
                _logger.LogInformation("API: Update position {PositionId}", id);
                var position = await _service.UpdatePositionAsync(id, dto);

                if (position == null)
                    return NotFound(new { message = $"Position with ID {id} not found" });

                return Ok(position);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position {PositionId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the position" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            try
            {
                _logger.LogWarning("API: Delete position {PositionId}", id);
                await _service.DeletePositionAsync(id);
                return Ok(new { message = "Position deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting position {PositionId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the position" });
            }
        }
    }
}