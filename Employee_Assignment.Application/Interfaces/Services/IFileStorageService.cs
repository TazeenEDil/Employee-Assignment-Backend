using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.FileStorage;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task<PaginatedResponse<EmployeeFileDto>> GetAllEmployeeFilesAsync(PaginationRequest request);
        Task<IEnumerable<EmployeeFileDto>> GetFilesByEmployeeIdAsync(int employeeId);
        Task<EmployeeFileDto> UploadFileAsync(UploadFileDto uploadDto);
        Task<FileDownloadDto?> DownloadFileAsync(int employeeFileId);
        Task<bool> DeleteFileAsync(int employeeFileId);
        Task<string> GetFilePreviewUrlAsync(int employeeFileId);
    }
}