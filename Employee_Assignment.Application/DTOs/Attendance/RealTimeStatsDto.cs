namespace Employee_Assignment.Application.DTOs.Attendance
{
    public class RealTimeStatsDto
    {
        public int TotalEmployees { get; set; }
        public int PresentToday { get; set; }
        public int AbsentToday { get; set; }
        public int LateToday { get; set; }
        public int OnLeaveToday { get; set; }
    }
}
