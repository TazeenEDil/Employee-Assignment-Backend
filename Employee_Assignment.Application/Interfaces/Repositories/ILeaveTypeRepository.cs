using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface ILeaveTypeRepository
    {
        Task<List<LeaveType>> GetAllAsync();
        Task<LeaveType?> GetByIdAsync(int id);
    }
}
