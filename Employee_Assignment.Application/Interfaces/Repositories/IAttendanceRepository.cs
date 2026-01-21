using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IAttendanceRepository
    {
        Task<Attendance?> GetTodayAttendanceAsync(int employeeId, DateTime date);
        Task<List<Attendance>> GetEmployeeAttendanceAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<List<Attendance>> GetAllTodayAttendanceAsync(DateTime date);
        Task<Attendance> CreateAsync(Attendance attendance);
        Task<Attendance> UpdateAsync(Attendance attendance);
        Task<List<Attendance>> GetAttendanceForMonthAsync(int employeeId, int year, int month);
    }

}