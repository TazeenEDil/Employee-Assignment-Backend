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
            // Validate leave type
            var leaveType = await _leaveTypeRepository.GetByIdAsync(dto.LeaveTypeId);
            if (leaveType == null)
                throw new ArgumentException("Invalid leave type");

            // Calculate total days
            var totalDays = (dto.EndDate.Date - dto.StartDate.Date).Days + 1;

            // Check if exceeds max days per year
            var year = dto.StartDate.Year;
            var usedDays = await _leaveRepository.GetApprovedLeaveDaysAsync(employeeId, dto.LeaveTypeId, year);

            if (usedDays + totalDays > leaveType.MaxDaysPerYear)
                throw new InvalidOperationException($"Exceeds maximum {leaveType.Name} days ({leaveType.MaxDaysPerYear}) for the year");

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

            // Send email to admin for approval
            var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
            if (employee == null)
                throw new ArgumentException("Employee not found");

            try
            {
                _logger.LogInformation("Sending leave request notification to admin");
                await _emailService.SendLeaveRequestForApprovalEmailAsync(
                    adminEmail: "admin@company.com", // Replace with actual admin email or get from settings
                    employeeName: employee.Name,
                    startDate: result.StartDate,
                    endDate: result.EndDate,
                    leaveRequestId: result.LeaveRequestId,
                    actionToken: result.EmailActionToken
                );
                _logger.LogInformation("Leave request notification sent to admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave approval notification email");
                // Don't throw - leave request is created, email failure shouldn't block
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
            var leaveRequest = await _leaveRepository.GetByIdAsync(leaveRequestId);
            if (leaveRequest == null)
                throw new ArgumentException("Leave request not found");

            if (leaveRequest.Status != "Pending")
                throw new InvalidOperationException("Leave request already processed");

            // Get employee details for email
            var employee = await _employeeRepository.GetEmployeeByIdAsync(leaveRequest.EmployeeId);
            if (employee == null)
                throw new ArgumentException("Employee not found");

            _logger.LogInformation("Processing leave request for employee: {EmployeeName} ({Email})", employee.Name, employee.Email);

            // Update leave request status
            leaveRequest.Status = dto.Approve ? "Approved" : "Rejected";
            leaveRequest.ApprovedByUserId = approvedByUserId;
            leaveRequest.ApprovedAt = DateTime.UtcNow;

            if (!dto.Approve)
            {
                // REJECTION
                leaveRequest.RejectionReason = dto.RejectionReason;

                // Update database first
                var result = await _leaveRepository.UpdateAsync(leaveRequest);
                _logger.LogInformation("Leave request rejected in database");

                // Send rejection email
                try
                {
                    _logger.LogInformation("Attempting to send rejection email to {Email}", employee.Email);
                    await _emailService.SendLeaveRejectedEmailAsync(
                        employee.Email,
                        employee.Name,
                        leaveRequest.StartDate,
                        leaveRequest.EndDate,
                        dto.RejectionReason ?? "No reason provided"
                    );
                    _logger.LogInformation("✅ Rejection email sent successfully to {Email}", employee.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send rejection email to {Email}", employee.Email);
                    // Don't throw - email failure shouldn't block the process
                }

                return MapToDto(result);
            }
            else
            {
                // APPROVAL
                _logger.LogInformation("Approving leave request");

                // Mark attendance as OnLeave for those dates
                _logger.LogInformation("Marking attendance as OnLeave from {StartDate} to {EndDate}",
                    leaveRequest.StartDate, leaveRequest.EndDate);

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
                        _logger.LogInformation("Created OnLeave attendance for {Date}", date.ToShortDateString());
                    }
                    else
                    {
                        attendance.Status = "OnLeave";
                        await _attendanceRepository.UpdateAsync(attendance);
                        _logger.LogInformation("Updated attendance to OnLeave for {Date}", date.ToShortDateString());
                    }
                }

                // Update database
                var result = await _leaveRepository.UpdateAsync(leaveRequest);
                _logger.LogInformation("Leave request approved in database");

                // Send approval email
                try
                {
                    _logger.LogInformation("Attempting to send approval email to {Email}", employee.Email);
                    await _emailService.SendLeaveApprovedEmailAsync(
                        employee.Email,
                        employee.Name,
                        leaveRequest.StartDate,
                        leaveRequest.EndDate
                    );
                    _logger.LogInformation("✅ Approval email sent successfully to {Email}", employee.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send approval email to {Email}", employee.Email);
                    // Don't throw - email failure shouldn't block the process
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