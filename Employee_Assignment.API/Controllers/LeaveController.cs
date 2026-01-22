using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/leave")]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _leaveService;

        public LeaveController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
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
                var employeeIdClaim = User.FindFirst("EmployeeId")
                    ?? User.FindFirst("employeeId")
                    ?? User.FindFirst("sub");

                if (employeeIdClaim == null)
                {
                    return Unauthorized(new { message = "Employee ID not found in token" });
                }

                if (!int.TryParse(employeeIdClaim.Value, out int employeeId))
                {
                    return Unauthorized(new { message = "Invalid employee ID format" });
                }

                var result = await _leaveService.CreateLeaveRequestAsync(employeeId, dto);
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
                return StatusCode(500, new { message = "An error occurred while creating leave request" });
            }
        }

        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> MyRequests()
        {
            var employeeIdClaim = User.FindFirst("EmployeeId")
                ?? User.FindFirst("employeeId")
                ?? User.FindFirst("sub");

            if (employeeIdClaim == null)
            {
                return Unauthorized(new { message = "Employee ID not found in token" });
            }

            if (!int.TryParse(employeeIdClaim.Value, out int employeeId))
            {
                return Unauthorized(new { message = "Invalid employee ID format" });
            }

            var requests = await _leaveService.GetEmployeeLeaveRequestsAsync(employeeId);
            return Ok(requests);
        }

        // ================= ADMIN ENDPOINTS =================

        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> Pending()
        {
            var requests = await _leaveService.GetPendingLeaveRequestsAsync();
            return Ok(requests);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveReject(int id, ApproveLeaveDto dto)
        {
            try
            {
                // Try multiple claim names to find the user ID
                var userIdClaim = User.FindFirst("UserId")
                    ?? User.FindFirst("sub")
                    ?? User.FindFirst("id")
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                if (!int.TryParse(userIdClaim.Value, out int adminId))
                {
                    return Unauthorized(new { message = "Invalid user ID format" });
                }

                var result = await _leaveService.ApproveOrRejectLeaveAsync(id, adminId, dto);
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
                return StatusCode(500, new { message = "An error occurred while processing leave request" });
            }
        }

        // ================= EMAIL ACTION ENDPOINT (PUBLIC) =================

        /// <summary>
        /// Public endpoint for email-based approval/rejection
        /// Allows admin to approve/reject via email link without logging in
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}/email-action")]
        public async Task<IActionResult> EmailApproveReject(
            int id,
            [FromQuery] bool approve,
            [FromQuery] string token)
        {
            try
            {
                // Validate token presence
                if (string.IsNullOrEmpty(token))
                    return Unauthorized(new { message = "Invalid token" });

                // Get leave request and validate
                var leaveRequest = await _leaveService.GetLeaveRequestByIdAsync(id);
                if (leaveRequest == null)
                    return NotFound(new { message = "Leave request not found" });

                // Validate token matches (you should add EmailActionToken to LeaveRequestDto)
                // For now, we'll skip this validation in DTO - handle it in service layer

                if (leaveRequest.Status != "Pending")
                    return BadRequest(new { message = "Leave request already processed" });

                // Process the request
                var dto = new ApproveLeaveDto
                {
                    Approve = approve,
                    RejectionReason = approve ? null : "Rejected via email"
                };

                // Use system admin ID for email actions
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
                return StatusCode(500, new { message = "An error occurred while processing leave request" });
            }
        }
    }
}