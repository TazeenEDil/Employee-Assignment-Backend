
using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Domain.Entities
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public DateTime? BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }
        public string Status { get; set; } = "Absent";
        public string WorkMode { get; set; } = "In-Office";
        public string? DailyReport { get; set; }
        public bool DailyReportSubmitted { get; set; } = false;
        public DateTime? DailyReportSubmittedAt { get; set; }
        public TimeSpan? TotalWorkHours { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Employee Employee { get; set; } = null!;
    }
}