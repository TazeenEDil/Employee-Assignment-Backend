namespace Employee_Assignment.Application.DTOs.FileStorage
{
    public class FileDownloadDto
    {
        public byte[] FileContent { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}