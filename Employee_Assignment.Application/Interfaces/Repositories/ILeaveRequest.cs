using Employee_Assignment.Domain.Entities;


namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface ILeaveRequestRepository
    {
        Task<List<LeaveRequest>> GetEmployeeLeaveRequestsAsync(int employeeId);
        Task<List<LeaveRequest>> GetPendingLeaveRequestsAsync();
        Task<List<LeaveRequest>> GetAllLeaveRequestsAsync();
        Task<LeaveRequest?> GetByIdAsync(int id);
        Task<LeaveRequest> CreateAsync(LeaveRequest leaveRequest);
        Task<LeaveRequest> UpdateAsync(LeaveRequest leaveRequest);
        Task<int> GetApprovedLeaveDaysAsync(int employeeId, int leaveTypeId, int year);


    }

}
