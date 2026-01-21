using Employee_Assignment.Application.DTOs.Attendance;


namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IAttendanceService
    {
        Task<AttendanceDto> ClockInAsync(int employeeId, ClockInDto dto);
        Task<AttendanceDto> ClockOutAsync(int employeeId);
        Task<AttendanceDto> StartBreakAsync(int employeeId);
        Task<AttendanceDto> EndBreakAsync(int employeeId);
        Task<AttendanceDto> SubmitDailyReportAsync(int employeeId, SubmitDailyReportDto dto);
        Task<List<AttendanceDto>> GetEmployeeAttendanceAsync(int employeeId, DateTime startDate, DateTime endDate);
        Task<AttendanceStatsDto> GetEmployeeStatsAsync(int employeeId, int year, int month);
        Task<RealTimeStatsDto> GetRealTimeStatsAsync(DateTime date);
        Task<double> GetDailyReportSubmissionRateAsync(DateTime date);
    }
}