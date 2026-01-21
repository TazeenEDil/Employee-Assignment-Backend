using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _service;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<LeaveController> _logger;

        public LeaveController(
            ILeaveService service,
            IEmployeeService employeeService,
            ILogger<LeaveController> logger)
        {
            _service = service;
            _employeeService = employeeService;
            _logger = logger;
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return null;

            var employees = await _employeeService.GetAllAsync();
            var employee = employees.FirstOrDefault(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return employee?.Id;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet("types")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            try
            {
                var result = await _service.GetLeaveTypesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave types");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("request")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> CreateLeaveRequest([FromBody] CreateLeaveRequestDto dto)
        {
            try
            {
                var employeeId = await GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee not found" });

                var result = await _service.CreateLeaveRequestAsync(employeeId.Value, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("my-requests")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetMyLeaveRequests()
        {
            try
            {
                var employeeId = await GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee not found" });

                var result = await _service.GetEmployeeLeaveRequestsAsync(employeeId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leave requests");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                var result = await _service.GetPendingLeaveRequestsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending requests");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveOrRejectLeave(int id, [FromBody] ApproveLeaveDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.ApproveOrRejectLeaveAsync(id, userId, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing leave request");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}