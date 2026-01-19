namespace Employee_Assignment.Application.DTOs.FileStorage
{
    public class EmployeeFileDto
    {
        public int EmployeeFileId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int FileStorageId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string? FileCategory { get; set; }
        public string FileStatus { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
