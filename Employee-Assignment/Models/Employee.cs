using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Assignment.Models
{
    [Table("Employees")]
        public class Employee
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }

            [Required]
            [MaxLength(100)]
            public string Name { get; set; }

            [Required]
            [EmailAddress]
            [MaxLength(100)]
            public string Email { get; set; }

            [Required]
            [MaxLength(100)]
            public string Position { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }

