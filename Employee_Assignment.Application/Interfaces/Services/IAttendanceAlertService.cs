using Employee_Assignment.Application.DTOs.Attendance;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IAttendanceAlertService
    {
            Task<List<AttendanceAlertDto>> GetEmployeeAlertsAsync(int employeeId);
            Task<AttendanceAlertDto> CreateAlertAsync(int createdByUserId, CreateAlertDto dto);
            Task MarkAlertAsReadAsync(int alertId);
            Task SendLateAlertAsync(int employeeId, int createdByUserId);
        }
    }

