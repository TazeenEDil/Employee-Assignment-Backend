namespace Employee_Assignment.Application.DTOs.Attendance
{
    public class LeaveTypeDto
    {
        public int LeaveTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxDaysPerYear { get; set; }
    }
}