using Employee_Assignment.Application.DTOs.Attendance;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(IAttendanceRepository repository, ILogger<AttendanceService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<AttendanceDto> ClockInAsync(int employeeId, ClockInDto dto)
        {
            var today = DateTime.UtcNow.Date;
            var existing = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (existing != null && existing.ClockIn != null)
                throw new InvalidOperationException("Already clocked in today");

            var now = DateTime.UtcNow;
            var attendance = existing ?? new Attendance
            {
                EmployeeId = employeeId,
                Date = today
            };

            attendance.ClockIn = now;
            attendance.WorkMode = dto.WorkMode;

            // Determine if late (after 9 AM)
            var clockInTime = now.TimeOfDay;
            var lateThreshold = new TimeSpan(9, 0, 0);
            attendance.Status = clockInTime > lateThreshold ? "Late" : "Present";

            var result = existing == null
                ? await _repository.CreateAsync(attendance)
                : await _repository.UpdateAsync(attendance);

            return MapToDto(result);
        }

        public async Task<AttendanceDto> ClockOutAsync(int employeeId)
        {
            var today = DateTime.UtcNow.Date;
            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException("No clock-in record found for today");

            if (attendance.ClockOut != null)
                throw new InvalidOperationException("Already clocked out today");

            attendance.ClockOut = DateTime.UtcNow;

            // Calculate total work hours
            if (attendance.ClockIn != null)
            {
                var totalTime = attendance.ClockOut.Value - attendance.ClockIn.Value;

                // Subtract break time if taken
                if (attendance.BreakStart != null && attendance.BreakEnd != null)
                {
                    var breakTime = attendance.BreakEnd.Value - attendance.BreakStart.Value;
                    totalTime -= breakTime;
                }

                attendance.TotalWorkHours = totalTime;
            }

            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> StartBreakAsync(int employeeId)
        {
            var today = DateTime.UtcNow.Date;
            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null || attendance.ClockIn == null)
                throw new InvalidOperationException("Must clock in before taking a break");

            if (attendance.BreakStart != null)
                throw new InvalidOperationException("Break already started");

            attendance.BreakStart = DateTime.UtcNow;
            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> EndBreakAsync(int employeeId)
        {
            var today = DateTime.UtcNow.Date;
            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null || attendance.BreakStart == null)
                throw new InvalidOperationException("No active break found");

            if (attendance.BreakEnd != null)
                throw new InvalidOperationException("Break already ended");

            attendance.BreakEnd = DateTime.UtcNow;
            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> SubmitDailyReportAsync(int employeeId, SubmitDailyReportDto dto)
        {
            var today = DateTime.UtcNow.Date;
            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException("No attendance record found for today");

            attendance.DailyReport = dto.Report;
            attendance.DailyReportSubmitted = true;
            attendance.DailyReportSubmittedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<List<AttendanceDto>> GetEmployeeAttendanceAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            var records = await _repository.GetEmployeeAttendanceAsync(employeeId, startDate, endDate);
            return records.Select(MapToDto).ToList();
        }

        public async Task<AttendanceStatsDto> GetEmployeeStatsAsync(int employeeId, int year, int month)
        {
            var records = await _repository.GetAttendanceForMonthAsync(employeeId, year, month);

            var totalDays = records.Count;
            var presentDays = records.Count(r => r.Status == "Present");
            var absentDays = records.Count(r => r.Status == "Absent");
            var lateDays = records.Count(r => r.Status == "Late");
            var leaveDays = records.Count(r => r.Status == "OnLeave");
            var reportsSubmitted = records.Count(r => r.DailyReportSubmitted);

            return new AttendanceStatsDto
            {
                TotalDays = totalDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                LeaveDays = leaveDays,
                AttendancePercentage = totalDays > 0 ? (presentDays + lateDays) * 100.0 / totalDays : 0,
                ReportSubmissionRate = totalDays > 0 ? reportsSubmitted * 100.0 / totalDays : 0
            };
        }

        public async Task<RealTimeStatsDto> GetRealTimeStatsAsync(DateTime date)
        {
            var records = await _repository.GetAllTodayAttendanceAsync(date);

            return new RealTimeStatsDto
            {
                TotalEmployees = records.Count,
                PresentToday = records.Count(r => r.Status == "Present"),
                AbsentToday = records.Count(r => r.Status == "Absent"),
                LateToday = records.Count(r => r.Status == "Late"),
                OnLeaveToday = records.Count(r => r.Status == "OnLeave")
            };
        }

        public async Task<double> GetDailyReportSubmissionRateAsync(DateTime date)
        {
            var records = await _repository.GetAllTodayAttendanceAsync(date);
            var total = records.Count;
            var submitted = records.Count(r => r.DailyReportSubmitted);

            return total > 0 ? submitted * 100.0 / total : 0;
        }

        private AttendanceDto MapToDto(Attendance attendance)
        {
            return new AttendanceDto
            {
                AttendanceId = attendance.AttendanceId,
                EmployeeId = attendance.EmployeeId,
                EmployeeName = attendance.Employee?.Name ?? "",
                Date = attendance.Date,
                ClockIn = attendance.ClockIn,
                ClockOut = attendance.ClockOut,
                BreakStart = attendance.BreakStart,
                BreakEnd = attendance.BreakEnd,
                Status = attendance.Status,
                WorkMode = attendance.WorkMode,
                DailyReport = attendance.DailyReport,
                DailyReportSubmitted = attendance.DailyReportSubmitted,
                TotalWorkHours = attendance.TotalWorkHours
            };
        }
    }
}