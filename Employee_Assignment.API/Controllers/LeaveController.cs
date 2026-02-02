using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        private readonly ILogger<LeaveController> _logger;

        public LeaveController(ILeaveService leaveService, ILogger<LeaveController> logger)
        {
            _leaveService = leaveService;
            _logger = logger;
        }

        // Extract employee ID from token claims
        private int? GetCurrentEmployeeId()
        {
            var claim =
                User.FindFirst("EmployeeId") ??
                User.FindFirst("employeeid") ??
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                _logger.LogInformation($"✅ Extracted EmployeeId: {id}");
                return id;
            }

            _logger.LogWarning("❌ Could not extract EmployeeId from claims");
            return null;
        }

        // Extract user ID from token claims (for admin actions)
        private int? GetCurrentUserId()
        {
            var claim =
                User.FindFirst("UserId") ??
                User.FindFirst("userid") ??
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("sub");

            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                _logger.LogInformation($"✅ Extracted UserId: {id}");
                return id;
            }

            _logger.LogWarning("❌ Could not extract UserId from claims");
            return null;
        }

        private void LogAllClaims()
        {
            _logger.LogInformation(" All claims in token:");
            foreach (var claim in User.Claims)
            {
                _logger.LogInformation($"  - {claim.Type} = {claim.Value}");
            }
        }

        [Authorize]
        [HttpGet("types")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            _logger.LogInformation(" Getting leave types");
            return Ok(await _leaveService.GetLeaveTypesAsync());
        }

        [Authorize]
        [HttpPost("request")]
        public async Task<IActionResult> CreateLeaveRequest(CreateLeaveRequestDto dto)
        {
            _logger.LogInformation(" Creating leave request");
            LogAllClaims();

            var employeeId = GetCurrentEmployeeId();
            if (!employeeId.HasValue)
            {
                _logger.LogWarning("❌ No EmployeeId found in claims");
                return Forbid();
            }

            try
            {
                var result = await _leaveService.CreateLeaveRequestAsync(employeeId.Value, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating leave request");
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> MyRequests()
        {
            _logger.LogInformation(" Getting my leave requests");
            var employeeId = GetCurrentEmployeeId();
            if (!employeeId.HasValue)
            {
                _logger.LogWarning("❌ No EmployeeId found in claims");
                return Forbid();
            }

            return Ok(await _leaveService.GetEmployeeLeaveRequestsAsync(employeeId.Value));
        }

        [Authorize]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            _logger.LogInformation(" Getting pending leave requests");
            _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");

            LogAllClaims();

            var roleFromClaimTypes = User.FindFirst(ClaimTypes.Role)?.Value;
            var roleFromStandard = User.FindFirst("role")?.Value;
            var isInAdminRole = User.IsInRole("Admin");
            var isInAdminRoleLower = User.IsInRole("admin");

            _logger.LogInformation($"Role from ClaimTypes.Role: {roleFromClaimTypes}");
            _logger.LogInformation($"Role from 'role': {roleFromStandard}");
            _logger.LogInformation($"IsInRole('Admin'): {isInAdminRole}");
            _logger.LogInformation($"IsInRole('admin'): {isInAdminRoleLower}");

            if (!isInAdminRole && !isInAdminRoleLower)
            {
                _logger.LogWarning("❌ User is not Admin");
                return Forbid();
            }

            return Ok(await _leaveService.GetPendingLeaveRequestsAsync());
        }

        [Authorize]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReject(int id, [FromBody] ApproveLeaveDto dto)
        {
            _logger.LogInformation($" Approving/Rejecting leave request {id}");
            _logger.LogInformation($"DTO - Approve: {dto.Approve}, Reason: {dto.RejectionReason}");

            LogAllClaims();

            var isInAdminRole = User.IsInRole("Admin");
            var isInAdminRoleLower = User.IsInRole("admin");

            _logger.LogInformation($"IsInRole('Admin'): {isInAdminRole}");
            _logger.LogInformation($"IsInRole('admin'): {isInAdminRoleLower}");

            if (!isInAdminRole && !isInAdminRoleLower)
            {
                _logger.LogWarning("❌ User is not Admin");
                return Forbid();
            }

            var adminId = GetCurrentUserId();
            if (!adminId.HasValue)
            {
                _logger.LogWarning("❌ No UserId found in claims");
                return Forbid();
            }

            try
            {
                var result = await _leaveService.ApproveOrRejectLeaveAsync(id, adminId.Value, dto);
                _logger.LogInformation($"✅ Leave request {id} processed successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error processing leave request {id}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}/email-action")]
        public async Task<IActionResult> EmailApproveReject(
            int id,
            [FromQuery] bool approve,
            [FromQuery] string token)
        {
            _logger.LogInformation($" Email action for leave request {id}");

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("❌ No token provided");
                return Unauthorized();
            }

            var dto = new ApproveLeaveDto
            {
                Approve = approve,
                RejectionReason = approve ? null : "Rejected via email"
            };

            const int systemAdminId = 1;

            try
            {
                var result = await _leaveService.ApproveOrRejectLeaveAsync(id, systemAdminId, dto);
                _logger.LogInformation($"✅ Email action for leave {id} processed successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error processing email action for leave {id}");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}