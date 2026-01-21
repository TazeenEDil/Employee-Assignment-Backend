using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Employee_Assignment.Infrastructure.Repositories
{
    public class AttendanceAlertRepository : IAttendanceAlertRepository
    {
        private readonly ApplicationDbContext _context;

        public AttendanceAlertRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AttendanceAlert>> GetEmployeeAlertsAsync(int employeeId)
        {
            return await _context.AttendanceAlerts
                .Include(aa => aa.Employee)
                .Include(aa => aa.CreatedByUser)
                .Where(aa => aa.EmployeeId == employeeId)
                .OrderByDescending(aa => aa.CreatedAt)
                .ToListAsync();
        }

        public async Task<AttendanceAlert> CreateAsync(AttendanceAlert alert)
        {
            _context.AttendanceAlerts.Add(alert);
            await _context.SaveChangesAsync();
            return alert;
        }

        public async Task MarkAsReadAsync(int alertId)
        {
            var alert = await _context.AttendanceAlerts.FindAsync(alertId);
            if (alert != null)
            {
                alert.IsRead = true;
                alert.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}