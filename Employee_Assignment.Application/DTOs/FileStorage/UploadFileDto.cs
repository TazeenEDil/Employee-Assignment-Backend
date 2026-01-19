using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Application.DTOs.FileStorage
{
    public class UploadFileDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public Stream FileStream { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; }

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string? FileCategory { get; set; }
    }
}