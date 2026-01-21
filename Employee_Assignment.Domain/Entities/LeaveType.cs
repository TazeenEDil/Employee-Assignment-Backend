using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Domain.Entities
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxDaysPerYear { get; set; }
        public bool RequiresApproval { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}