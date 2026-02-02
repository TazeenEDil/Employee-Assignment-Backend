using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Application.DTOs.Employee
{
    public class EmployeeDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; }

        public int PositionId { get; set; }
        public string PositionName { get; set; } 

        public DateTime? CreatedAt { get; set; }
    }
}