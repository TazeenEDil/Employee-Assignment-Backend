using Employee_Assignment.Application.DTOs.Attendance;


namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface ILeaveService
    {
        Task<List<LeaveTypeDto>> GetLeaveTypesAsync();
        Task<LeaveRequestDto> CreateLeaveRequestAsync(int employeeId, CreateLeaveRequestDto dto);
        Task<List<LeaveRequestDto>> GetEmployeeLeaveRequestsAsync(int employeeId);
        Task<List<LeaveRequestDto>> GetPendingLeaveRequestsAsync();
        Task<LeaveRequestDto> ApproveOrRejectLeaveAsync(int leaveRequestId, int approvedByUserId, ApproveLeaveDto dto);
        Task<LeaveRequestDto?> GetLeaveRequestByIdAsync(int id);
    }
}
