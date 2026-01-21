namespace Employee_Assignment.Application.DTOs.Attendance
{
    public class CreateAlertDto
    {
        public int EmployeeId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
