namespace Employee_Assignment.Application.DTOs.Attendance
{
    public class AttendanceStatsDto
    {
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public int LeaveDays { get; set; }
        public double AttendancePercentage { get; set; }
        public double ReportSubmissionRate { get; set; }
    }
}
