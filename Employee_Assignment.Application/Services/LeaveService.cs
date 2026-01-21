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
        private readonly ILogger<LeaveService> _logger;

        public LeaveService(
            ILeaveRequestRepository leaveRepository,
            ILeaveTypeRepository leaveTypeRepository,
            IAttendanceRepository attendanceRepository,
            ILogger<LeaveService> logger)
        {
            _leaveRepository = leaveRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _attendanceRepository = attendanceRepository;
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
                Status = "Pending"
            };

            var result = await _leaveRepository.CreateAsync(leaveRequest);
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

        public async Task<LeaveRequestDto> ApproveOrRejectLeaveAsync(int leaveRequestId, int approvedByUserId, ApproveLeaveDto dto)
        {
            var leaveRequest = await _leaveRepository.GetByIdAsync(leaveRequestId);
            if (leaveRequest == null)
                throw new ArgumentException("Leave request not found");

            if (leaveRequest.Status != "Pending")
                throw new InvalidOperationException("Leave request already processed");

            leaveRequest.Status = dto.Approve ? "Approved" : "Rejected";
            leaveRequest.ApprovedByUserId = approvedByUserId;
            leaveRequest.ApprovedAt = DateTime.UtcNow;

            if (!dto.Approve)
                leaveRequest.RejectionReason = dto.RejectionReason;

            // If approved, mark attendance as OnLeave for those dates
            if (dto.Approve)
            {
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
                    }
                    else
                    {
                        attendance.Status = "OnLeave";
                        await _attendanceRepository.UpdateAsync(attendance);
                    }
                }
            }

            var result = await _leaveRepository.UpdateAsync(leaveRequest);
            return MapToDto(result);
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