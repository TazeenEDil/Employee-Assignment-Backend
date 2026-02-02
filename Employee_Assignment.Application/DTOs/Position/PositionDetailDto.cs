namespace Employee_Assignment.Application.DTOs.Position
{
    public class PositionDetailDto
    {
        public int PositionId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int EmployeeCount { get; set; }
    }
}