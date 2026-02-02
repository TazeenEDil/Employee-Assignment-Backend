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
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendanceService service,
            ILogger<AttendanceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? GetCurrentEmployeeIdAsync()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

            if (string.IsNullOrEmpty(employeeIdClaim))
            {
                _logger.LogWarning("EmployeeId not found in token claims");
                return null;
            }

            if (int.TryParse(employeeIdClaim, out int empId))
            {
                _logger.LogInformation("EmployeeId from token: " + empId);
                return empId;
            }

            _logger.LogWarning("Failed to parse EmployeeId: " + employeeIdClaim);
            return null;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetMyAttendance(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var result = await _service.GetEmployeeAttendanceAsync(employeeId.Value, start, end);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving my attendance");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("clock-in")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> ClockIn([FromBody] ClockInDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var result = await _service.ClockInAsync(employeeId.Value, dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-in");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("clock-out")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> ClockOut()
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var result = await _service.ClockOutAsync(employeeId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-out");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("break/start")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> StartBreak()
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var result = await _service.StartBreakAsync(employeeId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("break/end")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> EndBreak()
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var result = await _service.EndBreakAsync(employeeId.Value);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("daily-report")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> SubmitDailyReport([FromBody] SubmitDailyReportDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                    return Unauthorized(new { message = "Employee ID not found in token" });

                var result = await _service.SubmitDailyReportAsync(employeeId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting daily report");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("employee/{employeeId}")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetEmployeeAttendance(
            int employeeId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                if (User.IsInRole("Employee"))
                {
                    var currentEmployeeId = GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue || currentEmployeeId.Value != employeeId)
                        return Forbid();
                }

                var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
                var end = endDate ?? DateTime.UtcNow;

                var result = await _service.GetEmployeeAttendanceAsync(employeeId, start, end);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attendance");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("stats/{employeeId}")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> GetStats(int employeeId, [FromQuery] int? year, [FromQuery] int? month)
        {
            try
            {
                if (User.IsInRole("Employee"))
                {
                    var currentEmployeeId = GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue || currentEmployeeId.Value != employeeId)
                        return Forbid();
                }

                var y = year ?? DateTime.UtcNow.Year;
                var m = month ?? DateTime.UtcNow.Month;

                var result = await _service.GetEmployeeStatsAsync(employeeId, y, m);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stats");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("realtime")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRealTimeStats([FromQuery] DateTime? date)
        {
            try
            {
                var d = date ?? DateTime.UtcNow;
                var result = await _service.GetRealTimeStatsAsync(d);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving real-time stats");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("report-submission-rate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportSubmissionRate([FromQuery] DateTime? date)
        {
            try
            {
                var d = date ?? DateTime.UtcNow;
                var rate = await _service.GetDailyReportSubmissionRateAsync(d);
                return Ok(new { submissionRate = rate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving submission rate");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("mark-absent")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAbsentEmployees([FromQuery] DateTime? date)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow.Date;
                await _service.MarkAbsentEmployeesAsync(targetDate);

                return Ok(new
                {
                    message = "Successfully marked absent employees for " + targetDate.ToShortDateString(),
                    date = targetDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking absent employees");
                return StatusCode(500, new { message = "An error occurred while marking absent employees" });
            }
        }
    }
}