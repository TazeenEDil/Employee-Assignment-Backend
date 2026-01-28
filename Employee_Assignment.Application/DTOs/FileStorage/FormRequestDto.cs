using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Employee_Assignment.Application.DTOs.FileStorage
{
    public class UploadFileRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public IFormFile File { get; set; } = null!;

        [MaxLength(100)]
        public string? Category { get; set; }
    }
}
