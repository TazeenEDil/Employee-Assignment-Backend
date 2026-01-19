using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Assignment.Domain.Entities
{
    [Table("EmployeeFiles")]
    public class EmployeeFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeFileId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int FileStorageId { get; set; }

        [MaxLength(100)]
        public string? FileCategory { get; set; } // Resume, Contract, Certificate, etc.

        public DateTime AssignedAt { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [ForeignKey("FileStorageId")]
        public virtual FileStorage FileStorage { get; set; }
    }
}