namespace Employee_Assignment.Application.DTOs.FileStorage
{
    public class FileStorageDto
    {
        public int FileStorageId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string FileStatus { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}