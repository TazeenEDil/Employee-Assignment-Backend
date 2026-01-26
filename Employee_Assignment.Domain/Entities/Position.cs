using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Assignment.Domain.Entities
{
    [Table("Positions")]
    public class Position
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PositionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } 
        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}