
using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } // "Admin" or "Employee"

        // Optional: Only required for Employee role
        public int? PositionId { get; set; }
    }
}