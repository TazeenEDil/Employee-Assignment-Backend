using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IFileStorageRepository
    {
        Task<IEnumerable<EmployeeFile>> GetAllEmployeeFilesAsync();
        Task<EmployeeFile?> GetEmployeeFileByIdAsync(int employeeFileId);
        Task<IEnumerable<EmployeeFile>> GetFilesByEmployeeIdAsync(int employeeId);
        Task<FileStorage?> GetFileStorageByIdAsync(int fileStorageId);
        Task<EmployeeFile> CreateEmployeeFileAsync(EmployeeFile employeeFile);
        Task<FileStorage> CreateFileStorageAsync(FileStorage fileStorage);
        Task<bool> DeleteEmployeeFileAsync(int employeeFileId);
        Task<bool> DeleteFileStorageAsync(int fileStorageId);
        Task<bool> FileExistsAsync(int fileStorageId);
    }
}