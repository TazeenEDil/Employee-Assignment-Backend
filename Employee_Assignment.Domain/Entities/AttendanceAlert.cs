using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Domain.Entities
{
    public class AttendanceAlert
    {
        [Key]
        public int AlertId { get; set; }
        public int EmployeeId { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime AlertDate { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Employee Employee { get; set; } = null!;
        public virtual User CreatedByUser { get; set; } = null!;
    }
}