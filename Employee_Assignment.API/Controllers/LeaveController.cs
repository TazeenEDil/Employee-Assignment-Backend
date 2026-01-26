using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/leave")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;
        private readonly IAuthService _authService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<LeaveController> _logger;

        public LeaveController(
            ILeaveService leaveService,
            IAuthService authService,
            IEmployeeRepository employeeRepository,
            ILogger<LeaveController> logger)
        {
            _leaveService = leaveService;
            _authService = authService;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            try
            {
                _logger.LogInformation("=== Getting Current Employee ID ===");

                // Get email from claims
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                    ?? User.FindFirst("email")?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("No email found in claims");
                    return null;
                }

                _logger.LogInformation("Email from claims: {Email}", email);

                // Get employee by email from employee repository
                var employees = await _employeeRepository.GetAllEmployeesAsync();
                var employee = employees.FirstOrDefault(e =>
                    e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (employee == null)
                {
                    _logger.LogWarning("Employee not found with email: {Email}", email);
                    return null;
                }

                _logger.LogInformation("Employee found - EmployeeId: {EmployeeId}", employee.Id);
                return employee.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current employee ID");
                return null;
            }
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            try
            {
                _logger.LogInformation("=== Getting Current User ID ===");

                // Log all available claims for debugging
                var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogInformation("Available claims: {Claims}", System.Text.Json.JsonSerializer.Serialize(claims));

                // Method 1: Try to get email from claims (most reliable)
                var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                    ?? User.FindFirst("email")?.Value;

                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Found email in claims: {Email}", email);
                    var user = await _authService.GetUserByEmailAsync(email);

                    if (user != null)
                    {
                        _logger.LogInformation("User found by email - UserId: {UserId}", user.Id);
                        return user.Id;
                    }

                    _logger.LogWarning("User not found with email: {Email}", email);
                }

                // Method 2: Try direct UserId claims
                var userIdClaim = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("id")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogInformation("Found UserId claim: {UserIdClaim}", userIdClaim);

                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        _logger.LogInformation("Successfully parsed UserId: {UserId}", userId);
                        return userId;
                    }

                    _logger.LogWarning("Failed to parse UserId claim: {UserIdClaim}", userIdClaim);
                }

                _logger.LogWarning("No valid user identification found in claims");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user ID");
                return null;
            }
        }

        // ================= USER ENDPOINTS =================

        [Authorize]
        [HttpGet("types")]
        public async Task<IActionResult> GetLeaveTypes()
        {
            var types = await _leaveService.GetLeaveTypesAsync();
            return Ok(types);
        }

        [Authorize]
        [HttpPost("request")]
        public async Task<IActionResult> CreateLeaveRequest(CreateLeaveRequestDto dto)
        {
            try
            {
                _logger.LogInformation("Creating leave request");

                // Use the helper method to get employee ID
                var employeeId = await GetCurrentEmployeeIdAsync();

                if (!employeeId.HasValue)
                {
                    _logger.LogWarning("Could not determine employee ID from token");
                    return Unauthorized(new { message = "Employee ID not found. Please log out and log back in." });
                }

                _logger.LogInformation("Creating leave request for EmployeeId: {EmployeeId}", employeeId.Value);

                var result = await _leaveService.CreateLeaveRequestAsync(employeeId.Value, dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument exception in CreateLeaveRequest");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in CreateLeaveRequest");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request");
                return StatusCode(500, new { message = "An error occurred while creating leave request" });
            }
        }

        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> MyRequests()
        {
            try
            {
                _logger.LogInformation("Getting my leave requests");

                // Use the helper method to get employee ID
                var employeeId = await GetCurrentEmployeeIdAsync();

                if (!employeeId.HasValue)
                {
                    _logger.LogWarning("Could not determine employee ID from token");
                    return Unauthorized(new { message = "Employee ID not found. Please log out and log back in." });
                }

                _logger.LogInformation("Getting leave requests for EmployeeId: {EmployeeId}", employeeId.Value);

                var requests = await _leaveService.GetEmployeeLeaveRequestsAsync(employeeId.Value);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my leave requests");
                return StatusCode(500, new { message = "An error occurred while fetching leave requests" });
            }
        }

        // ================= ADMIN ENDPOINTS =================

        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            _logger.LogInformation("Getting pending leave requests");
            var requests = await _leaveService.GetPendingLeaveRequestsAsync();
            _logger.LogInformation("Found {Count} pending requests", requests.Count);
            return Ok(requests);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReject(int id, [FromBody] ApproveLeaveDto dto)
        {
            try
            {
                _logger.LogInformation("=== Processing Leave Request ===");
                _logger.LogInformation("LeaveRequestId: {LeaveRequestId}", id);
                _logger.LogInformation("Approve: {Approve}", dto.Approve);
                _logger.LogInformation("RejectionReason: {Reason}", dto.RejectionReason ?? "N/A");

                // Get admin user ID
                var adminId = await GetCurrentUserIdAsync();

                if (!adminId.HasValue)
                {
                    _logger.LogError("Could not determine admin user ID from token");
                    return Unauthorized(new { message = "User ID not found in token. Please log out and log back in." });
                }

                _logger.LogInformation("Admin ID: {AdminId}", adminId.Value);

                var result = await _leaveService.ApproveOrRejectLeaveAsync(id, adminId.Value, dto);

                _logger.LogInformation("Leave request processed successfully");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Argument exception in ApproveReject");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation in ApproveReject");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ApproveReject");
                return StatusCode(500, new { message = "An error occurred while processing leave request" });
            }
        }

        // ================= EMAIL ACTION ENDPOINT (PUBLIC) =================

        [AllowAnonymous]
        [HttpGet("{id}/email-action")]
        public async Task<IActionResult> EmailApproveReject(
            int id,
            [FromQuery] bool approve,
            [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "Invalid token" });

                var leaveRequest = await _leaveService.GetLeaveRequestByIdAsync(id);
                if (leaveRequest == null)
                    return NotFound(new { message = "Leave request not found" });

                if (leaveRequest.Status != "Pending")
                    return BadRequest(new { message = "Leave request already processed" });

                var dto = new ApproveLeaveDto
                {
                    Approve = approve,
                    RejectionReason = approve ? null : "Rejected via email"
                };

                const int systemAdminId = 1;
                var result = await _leaveService.ApproveOrRejectLeaveAsync(id, systemAdminId, dto);

                return Ok(new
                {
                    message = approve ? "Leave approved successfully" : "Leave rejected successfully",
                    leaveRequest = result
                });
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
                _logger.LogError(ex, "Error in email approve/reject");
                return StatusCode(500, new { message = "An error occurred while processing leave request" });
            }
        }
    }
}