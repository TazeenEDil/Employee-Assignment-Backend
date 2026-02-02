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
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<AttendanceService> _logger;

        // Pakistani timezone
        private static readonly TimeZoneInfo PakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        public AttendanceService(
            IAttendanceRepository repository,
            IEmployeeRepository employeeRepository,
            ILogger<AttendanceService> logger)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        private DateTime GetPakistanTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);
        }

        public async Task<AttendanceDto> ClockInAsync(int employeeId, ClockInDto dto)
        {
            var pakistanNow = GetPakistanTime();
            var today = pakistanNow.Date;

            var existing = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (existing != null && existing.ClockIn != null)
                throw new InvalidOperationException("Already clocked in today");

            var attendance = existing ?? new Attendance
            {
                EmployeeId = employeeId,
                Date = today
            };

            attendance.ClockIn = pakistanNow;
            attendance.WorkMode = dto.WorkMode;

           
            var clockInTime = pakistanNow.TimeOfDay;
            var lateThreshold = new TimeSpan(17, 0, 0);
            attendance.Status = clockInTime > lateThreshold ? "Late" : "Present";

            _logger.LogInformation("Employee {EmployeeId} clocked in at {Time} Pakistan time - Status: {Status}",
                employeeId, pakistanNow.ToString("HH:mm:ss"), attendance.Status);

            var result = existing == null
                ? await _repository.CreateAsync(attendance)
                : await _repository.UpdateAsync(attendance);

            return MapToDto(result);
        }

        public async Task<AttendanceDto> ClockOutAsync(int employeeId)
        {
            var pakistanNow = GetPakistanTime();
            var today = pakistanNow.Date;

            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException("No clock-in record found for today");

            if (attendance.ClockOut != null)
                throw new InvalidOperationException("Already clocked out today");

            attendance.ClockOut = pakistanNow;

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

                //  Check if clocking out before 5 PM - mark as Late
                var clockOutTime = pakistanNow.TimeOfDay;
                var earlyLeaveThreshold = new TimeSpan(0, 0, 0); // 12am

                if (clockOutTime < earlyLeaveThreshold && attendance.Status == "Present")
                {
                    attendance.Status = "Present";
                    _logger.LogInformation("Employee {EmployeeId} clocked out before 12 AM - marked as Present", employeeId);
                }
            }

            _logger.LogInformation("Employee {EmployeeId} clocked out at {Time} Pakistan time - Total hours: {Hours}",
                employeeId, pakistanNow.ToString("HH:mm:ss"), attendance.TotalWorkHours);

            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> StartBreakAsync(int employeeId)
        {
            var pakistanNow = GetPakistanTime();
            var today = pakistanNow.Date;

            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null || attendance.ClockIn == null)
                throw new InvalidOperationException("Must clock in before taking a break");

            if (attendance.BreakStart != null)
                throw new InvalidOperationException("Break already started");

            attendance.BreakStart = pakistanNow;
            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> EndBreakAsync(int employeeId)
        {
            var pakistanNow = GetPakistanTime();
            var today = pakistanNow.Date;

            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null || attendance.BreakStart == null)
                throw new InvalidOperationException("No active break found");

            if (attendance.BreakEnd != null)
                throw new InvalidOperationException("Break already ended");

            attendance.BreakEnd = pakistanNow;
            var result = await _repository.UpdateAsync(attendance);
            return MapToDto(result);
        }

        public async Task<AttendanceDto> SubmitDailyReportAsync(int employeeId, SubmitDailyReportDto dto)
        {
            var pakistanNow = GetPakistanTime();
            var today = pakistanNow.Date;

            var attendance = await _repository.GetTodayAttendanceAsync(employeeId, today);

            if (attendance == null)
                throw new InvalidOperationException("No attendance record found for today");

            attendance.DailyReport = dto.Report;
            attendance.DailyReportSubmitted = true;
            attendance.DailyReportSubmittedAt = pakistanNow;

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

        // ✅ NEW: Mark employees as absent automatically
        public async Task MarkAbsentEmployeesAsync(DateTime date)
        {
            _logger.LogInformation("Starting automatic absent marking for {Date}", date.ToShortDateString());

            // Get all employees
            var allEmployees = await _employeeRepository.GetAllEmployeesAsync();

            foreach (var employee in allEmployees)
            {
                var attendance = await _repository.GetTodayAttendanceAsync(employee.Id, date);

                // If no attendance record exists, create one marked as Absent
                if (attendance == null)
                {
                    var newAttendance = new Attendance
                    {
                        EmployeeId = employee.Id,
                        Date = date.Date,
                        Status = "Absent"
                    };

                    await _repository.CreateAsync(newAttendance);
                    _logger.LogInformation("Marked employee {EmployeeId} ({Name}) as Absent for {Date}",
                        employee.Id, employee.Name, date.ToShortDateString());
                }
                // If attendance exists but no clock-in, mark as Absent
                else if (attendance.ClockIn == null && attendance.Status != "OnLeave")
                {
                    attendance.Status = "Absent";
                    await _repository.UpdateAsync(attendance);
                    _logger.LogInformation("Updated employee {EmployeeId} ({Name}) to Absent for {Date}",
                        employee.Id, employee.Name, date.ToShortDateString());
                }
            }

            _logger.LogInformation("Completed automatic absent marking for {Date}", date.ToShortDateString());
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