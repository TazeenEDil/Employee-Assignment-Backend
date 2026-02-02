using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly ILeaveRequestRepository _leaveRepository;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<LeaveService> _logger;

        public LeaveService(
            ILeaveRequestRepository leaveRepository,
            ILeaveTypeRepository leaveTypeRepository,
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository,
            IEmailService emailService,
            ILogger<LeaveService> logger)
        {
            _leaveRepository = leaveRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<LeaveTypeDto>> GetLeaveTypesAsync()
        {
            var types = await _leaveTypeRepository.GetAllAsync();
            return types.Select(t => new LeaveTypeDto
            {
                LeaveTypeId = t.LeaveTypeId,
                Name = t.Name,
                Description = t.Description,
                MaxDaysPerYear = t.MaxDaysPerYear
            }).ToList();
        }

        public async Task<LeaveRequestDto> CreateLeaveRequestAsync(int employeeId, CreateLeaveRequestDto dto)
        {
            _logger.LogInformation($"📝 Creating leave request for employee {employeeId}");

            // Validate leave type
            var leaveType = await _leaveTypeRepository.GetByIdAsync(dto.LeaveTypeId);
            if (leaveType == null)
                throw new ArgumentException("Invalid leave type");

            // Calculate total days
            var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;

            // Check if exceeds max days per year
            var year = dto.StartDate.Year;
            var usedDays = await _leaveRepository.GetApprovedLeaveDaysAsync(employeeId, dto.LeaveTypeId, year);
            var remainingDays = leaveType.MaxDaysPerYear - usedDays;

            _logger.LogInformation($"📊 Leave Balance Check:");
            _logger.LogInformation($"   Leave Type: {leaveType.Name}");
            _logger.LogInformation($"   Max Days/Year: {leaveType.MaxDaysPerYear}");
            _logger.LogInformation($"   Used Days: {usedDays}");
            _logger.LogInformation($"   Remaining Days: {remainingDays}");
            _logger.LogInformation($"   Requested Days: {totalDays}");

            // Check if request exceeds remaining days
            if (totalDays > remainingDays)
            {
                if (remainingDays == 0)
                {
                    _logger.LogWarning($"⚠️ Employee has no {leaveType.Name} days left");
                    throw new InvalidOperationException(
                        $"You have exhausted your {leaveType.Name} allotment of {leaveType.MaxDaysPerYear} days for {year}. You have 0 days remaining."
                    );
                }
                else
                {
                    _logger.LogWarning($"⚠️ Request exceeds remaining days");
                    throw new InvalidOperationException(
                        $"You have {remainingDays} day{(remainingDays == 1 ? "" : "s")} of {leaveType.Name} left. Your request for {totalDays} day{(totalDays == 1 ? "" : "s")} exceeds your remaining balance."
                    );
                }
            }

            // Check if total would exceed max days per year
            if (usedDays + totalDays > leaveType.MaxDaysPerYear)
            {
                _logger.LogWarning($"⚠️ Request would exceed annual limit");
                throw new InvalidOperationException(
                    $"Your leave request exceeds the {leaveType.Name} allotment of {leaveType.MaxDaysPerYear} days per year. You have {remainingDays} day{(remainingDays == 1 ? "" : "s")} remaining."
                );
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveTypeId = dto.LeaveTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalDays = totalDays,
                Reason = dto.Reason,
                Status = "Pending",
                EmailActionToken = Guid.NewGuid().ToString()
            };

            var result = await _leaveRepository.CreateAsync(leaveRequest);
            _logger.LogInformation($"✅ Leave request created successfully");

            
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            if (employee != null)
            {
                try
                {
                    _logger.LogInformation($"📧 Sending leave request notification to admin");
                    await _emailService.SendLeaveRequestForApprovalEmailAsync(
                        adminEmail: "admin@company.com",
                        employeeName: employee.Name,
                        startDate: result.StartDate,
                        endDate: result.EndDate,
                        leaveRequestId: result.LeaveRequestId,
                        actionToken: result.EmailActionToken
                    );
                    _logger.LogInformation($"✅ Notification email sent to admin");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send leave approval email to admin");
                }
            }

            return MapToDto(result);
        }

        public async Task<List<LeaveRequestDto>> GetEmployeeLeaveRequestsAsync(int employeeId)
        {
            var requests = await _leaveRepository.GetEmployeeLeaveRequestsAsync(employeeId);
            return requests.Select(MapToDto).ToList();
        }

        public async Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
        {
            var requests = await _leaveRepository.GetPendingLeaveRequestsAsync();
            return requests.Select(MapToDto).ToList();
        }

        public async Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id)
        {
            var leaveRequest = await _leaveRepository.GetByIdAsync(id);
            return leaveRequest == null ? null : MapToDto(leaveRequest);
        }

        public async Task<LeaveRequestDto> ApproveOrRejectLeaveAsync(int leaveRequestId, int approvedByUserId, ApproveLeaveDto dto)
        {
            _logger.LogInformation($"🔍 Processing leave request {leaveRequestId} by user {approvedByUserId}");
            _logger.LogInformation($"   Approve: {dto.Approve}, Reason: {dto.RejectionReason}");

            var leaveRequest = await _leaveRepository.GetByIdAsync(leaveRequestId);
            if (leaveRequest == null)
            {
                _logger.LogError($"❌ Leave request {leaveRequestId} not found");
                throw new ArgumentException("Leave request not found");
            }

            if (leaveRequest.Status != "Pending")
            {
                _logger.LogWarning($"⚠️ Leave request {leaveRequestId} already processed. Status: {leaveRequest.Status}");
                throw new InvalidOperationException("Leave request already processed");
            }

            _logger.LogInformation($"📋 Leave request details: EmployeeId={leaveRequest.EmployeeId}, Status={leaveRequest.Status}");

            // Get employee record
            var employee = await _employeeRepository.GetEmployeeByIdAsync(leaveRequest.EmployeeId);
            if (employee == null)
            {
                _logger.LogError($"❌ Employee record not found for EmployeeId: {leaveRequest.EmployeeId}");
                throw new ArgumentException($"Employee record not found for EmployeeId {leaveRequest.EmployeeId}");
            }

            _logger.LogInformation($"👤 Employee: {employee.Name} (EmployeeId: {employee.Id})");

            // Check if employee has email
            if (string.IsNullOrEmpty(employee.Email))
            {
                _logger.LogError($"❌ Employee {employee.Name} (ID: {employee.Id}) has no email address");

                // Update the leave status in database first
                leaveRequest.Status = dto.Approve ? "Approved" : "Rejected";
                leaveRequest.ApprovedByUserId = approvedByUserId;
                leaveRequest.ApprovedAt = DateTime.UtcNow;
                if (!dto.Approve)
                {
                    leaveRequest.RejectionReason = dto.RejectionReason;
                }

                var resultWithoutEmail = await _leaveRepository.UpdateAsync(leaveRequest);

                // THROW EXCEPTION so frontend knows email failed
                throw new InvalidOperationException($"Leave {(dto.Approve ? "approved" : "rejected")} but employee {employee.Name} has no email configured");
            }

            _logger.LogInformation($"📧 Employee email: {employee.Email}");

            // Update leave request status
            leaveRequest.Status = dto.Approve ? "Approved" : "Rejected";
            leaveRequest.ApprovedByUserId = approvedByUserId;
            leaveRequest.ApprovedAt = DateTime.UtcNow;

            if (!dto.Approve)
            {
                leaveRequest.RejectionReason = dto.RejectionReason;
                _logger.LogInformation($"❌ Rejecting leave request. Reason: {dto.RejectionReason}");

                // Update database first
                var result = await _leaveRepository.UpdateAsync(leaveRequest);
                _logger.LogInformation($"✅ Leave request {leaveRequestId} marked as rejected in database");

                // Then send rejection email
                try
                {
                    _logger.LogInformation($"📧 Sending rejection email to: {employee.Email}");
                    await _emailService.SendLeaveRejectedEmailAsync(
                        employee.Email,
                        employee.Name,
                        leaveRequest.StartDate,
                        leaveRequest.EndDate,
                        dto.RejectionReason ?? "No reason provided"
                    );
                    _logger.LogInformation($"✅ Rejection email sent successfully to {employee.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ CRITICAL: Failed to send rejection email to {employee.Email}");
                    _logger.LogError($"   Error Type: {ex.GetType().Name}");
                    _logger.LogError($"   Error Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"   Inner Exception: {ex.InnerException.Message}");
                    }
                    // THROW exception so frontend knows email failed
                    throw new InvalidOperationException($"Leave rejected but email failed: {ex.Message}", ex);
                }

                return MapToDto(result);
            }
            else
            {
                _logger.LogInformation($"✅ Approving leave request");

                // Mark attendance as OnLeave for the date range
                _logger.LogInformation($" Marking attendance as OnLeave for dates {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}");

                for (var date = leaveRequest.StartDate.Date; date <= leaveRequest.EndDate.Date; date = date.AddDays(1))
                {
                    var attendance = await _attendanceRepository.GetTodayAttendanceAsync(leaveRequest.EmployeeId, date);
                    if (attendance == null)
                    {
                        attendance = new Attendance
                        {
                            EmployeeId = leaveRequest.EmployeeId,
                            Date = date,
                            Status = "OnLeave"
                        };
                        await _attendanceRepository.CreateAsync(attendance);
                        _logger.LogInformation($"   Created OnLeave attendance for {date:yyyy-MM-dd}");
                    }
                    else
                    {
                        attendance.Status = "OnLeave";
                        await _attendanceRepository.UpdateAsync(attendance);
                        _logger.LogInformation($"   Updated attendance to OnLeave for {date:yyyy-MM-dd}");
                    }
                }

                // Update database first
                var result = await _leaveRepository.UpdateAsync(leaveRequest);
                _logger.LogInformation($"✅ Leave request {leaveRequestId} marked as approved in database");

                // Then send approval email
                try
                {
                    _logger.LogInformation($"📧 Sending approval email to: {employee.Email}");
                    await _emailService.SendLeaveApprovedEmailAsync(
                        employee.Email,
                        employee.Name,
                        leaveRequest.StartDate,
                        leaveRequest.EndDate
                    );
                    _logger.LogInformation($"✅ Approval email sent successfully to {employee.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ CRITICAL: Failed to send approval email to {employee.Email}");
                    _logger.LogError($"   Error Type: {ex.GetType().Name}");
                    _logger.LogError($"   Error Message: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"   Inner Exception: {ex.InnerException.Message}");
                    }
                    // THROW exception so frontend knows email failed
                    throw new InvalidOperationException($"Leave approved but email failed: {ex.Message}", ex);
                }

                return MapToDto(result);
            }
        }

        private LeaveRequestDto MapToDto(LeaveRequest lr)
        {
            return new LeaveRequestDto
            {
                LeaveRequestId = lr.LeaveRequestId,
                EmployeeId = lr.EmployeeId,
                EmployeeName = lr.Employee?.Name ?? "",
                LeaveTypeId = lr.LeaveTypeId,
                LeaveTypeName = lr.LeaveType?.Name ?? "",
                StartDate = lr.StartDate,
                EndDate = lr.EndDate,
                TotalDays = lr.TotalDays,
                Reason = lr.Reason,
                Status = lr.Status,
                ApprovedByName = lr.ApprovedByUser?.Name,
                ApprovedAt = lr.ApprovedAt,
                RejectionReason = lr.RejectionReason,
                CreatedAt = lr.CreatedAt
            };
        }
    }
}