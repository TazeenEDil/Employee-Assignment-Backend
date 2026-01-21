using Employee_Assignment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IAttendanceAlertRepository
    {
        Task<List<AttendanceAlert>> GetEmployeeAlertsAsync(int employeeId);
        Task<AttendanceAlert> CreateAsync(AttendanceAlert alert);
        Task MarkAsReadAsync(int alertId);
    }
}
