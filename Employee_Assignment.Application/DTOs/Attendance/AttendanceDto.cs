namespace Employee_Assignment.Application.DTOs.Attendance
{
    public class AttendanceDto
    {
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public DateTime? BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WorkMode { get; set; } = string.Empty;
        public string? DailyReport { get; set; }
        public bool DailyReportSubmitted { get; set; }
        public TimeSpan? TotalWorkHours { get; set; }
    }
}