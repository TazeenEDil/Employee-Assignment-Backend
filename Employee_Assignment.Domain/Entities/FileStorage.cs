using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Employee_Assignment.Domain.Entities
{
    [Table("FileStorages")]
    public class FileStorage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FileStorageId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } // Relative path: Year/Month/Date/filename

        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } // pdf, doc, docx, png, jpg, etc.

        [Required]
        public long FileSize { get; set; } // In bytes

        [Required]
        [MaxLength(50)]
        public string FileStatus { get; set; } // Active, Archived, Deleted

        public DateTime UploadedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public virtual ICollection<EmployeeFile> EmployeeFiles { get; set; } = new List<EmployeeFile>();
    }
}