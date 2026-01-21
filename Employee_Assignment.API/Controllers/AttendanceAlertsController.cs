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
    public class AttendanceAlertsController : ControllerBase
    {
        private readonly IAttendanceAlertService _service;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<AttendanceAlertsController> _logger;

        public AttendanceAlertsController(
            IAttendanceAlertService service,
            IEmployeeService employeeService,
            ILogger<AttendanceAlertsController> logger)
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

        [HttpGet("my-alerts")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetMyAlerts()
        {
            try
            {
                var employeeId = await GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee not found" });

                var result = await _service.GetEmployeeAlertsAsync(employeeId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alerts");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAlert([FromBody] CreateAlertDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _service.CreateAlertAsync(userId, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("{id}/read")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _service.MarkAlertAsReadAsync(id);
                return Ok(new { message = "Alert marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking alert as read");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}