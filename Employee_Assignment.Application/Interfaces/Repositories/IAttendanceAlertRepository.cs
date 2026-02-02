using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IAttendanceAlertRepository
    {
        Task<List<AttendanceAlert>> GetEmployeeAlertsAsync(int employeeId);
        Task<AttendanceAlert> CreateAsync(AttendanceAlert alert);
        Task MarkAsReadAsync(int alertId);
    }
}
