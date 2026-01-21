using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class AttendanceAlertService : IAttendanceAlertService
    {
        private readonly IAttendanceAlertRepository _repository;
        private readonly ILogger<AttendanceAlertService> _logger;

        public AttendanceAlertService(IAttendanceAlertRepository repository, ILogger<AttendanceAlertService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<AttendanceAlertDto>> GetEmployeeAlertsAsync(int employeeId)
        {
            var alerts = await _repository.GetEmployeeAlertsAsync(employeeId);
            return alerts.Select(a => new AttendanceAlertDto
            {
                AlertId = a.AlertId,
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee?.Name ?? "",
                AlertType = a.AlertType,
                Message = a.Message,
                AlertDate = a.AlertDate,
                IsRead = a.IsRead,
                CreatedAt = a.CreatedAt
            }).ToList();
        }

        public async Task<AttendanceAlertDto> CreateAlertAsync(int createdByUserId, CreateAlertDto dto)
        {
            var alert = new AttendanceAlert
            {
                EmployeeId = dto.EmployeeId,
                AlertType = dto.AlertType,
                Message = dto.Message,
                AlertDate = DateTime.UtcNow.Date,
                CreatedByUserId = createdByUserId
            };

            var result = await _repository.CreateAsync(alert);

            return new AttendanceAlertDto
            {
                AlertId = result.AlertId,
                EmployeeId = result.EmployeeId,
                AlertType = result.AlertType,
                Message = result.Message,
                AlertDate = result.AlertDate,
                IsRead = result.IsRead,
                CreatedAt = result.CreatedAt
            };
        }

        public async Task MarkAlertAsReadAsync(int alertId)
        {
            await _repository.MarkAsReadAsync(alertId);
        }

        public async Task SendLateAlertAsync(int employeeId, int createdByUserId)
        {
            var alert = new CreateAlertDto
            {
                EmployeeId = employeeId,
                AlertType = "Late",
                Message = "You were late to clock in today. Please ensure punctuality."
            };

            await CreateAlertAsync(createdByUserId, alert);
        }
    }
}