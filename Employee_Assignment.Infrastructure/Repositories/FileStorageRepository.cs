using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Infrastructure.Repositories
{
    public class FileStorageRepository : IFileStorageRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FileStorageRepository> _logger;
        private readonly ICircuitBreakerService _circuitBreakerService;

        public FileStorageRepository(
            ApplicationDbContext context,
            ILogger<FileStorageRepository> logger,
            ICircuitBreakerService circuitBreakerService)
        {
            _context = context;
            _logger = logger;
            _circuitBreakerService = circuitBreakerService;
        }

        public async Task<IEnumerable<EmployeeFile>> GetAllEmployeeFilesAsync()
        {
            _logger.LogInformation("Repository: Fetching all employee files");
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.EmployeeFiles
                    .Include(ef => ef.Employee)
                    .Include(ef => ef.FileStorage)
                    .OrderByDescending(ef => ef.AssignedAt)
                    .ToListAsync()
            );
        }

        public async Task<EmployeeFile?> GetEmployeeFileByIdAsync(int employeeFileId)
        {
            _logger.LogInformation("Repository: Fetch employee file {EmployeeFileId}", employeeFileId);
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.EmployeeFiles
                    .Include(ef => ef.Employee)
                    .Include(ef => ef.FileStorage)
                    .FirstOrDefaultAsync(ef => ef.EmployeeFileId == employeeFileId)
            );
        }

        public async Task<IEnumerable<EmployeeFile>> GetFilesByEmployeeIdAsync(int employeeId)
        {
            _logger.LogInformation("Repository: Fetch files for employee {EmployeeId}", employeeId);
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.EmployeeFiles
                    .Include(ef => ef.FileStorage)
                    .Include(ef => ef.Employee)
                    .Where(ef => ef.EmployeeId == employeeId)
                    .OrderByDescending(ef => ef.AssignedAt)
                    .ToListAsync()
            );
        }

        public async Task<FileStorage?> GetFileStorageByIdAsync(int fileStorageId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.FileStorages.FindAsync(fileStorageId)
            );
        }

        public async Task<EmployeeFile> CreateEmployeeFileAsync(EmployeeFile employeeFile)
        {
            _logger.LogInformation("Repository: Creating employee file for {EmployeeId}", employeeFile.EmployeeId);
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    employeeFile.AssignedAt = DateTime.UtcNow;
                    _context.EmployeeFiles.Add(employeeFile);
                    await _context.SaveChangesAsync();

                    // Reload 
                    return (await _context.EmployeeFiles
                        .Include(ef => ef.Employee)
                        .Include(ef => ef.FileStorage)
                        .FirstOrDefaultAsync(ef => ef.EmployeeFileId == employeeFile.EmployeeFileId))!;
                }
            );
        }

        public async Task<FileStorage> CreateFileStorageAsync(FileStorage fileStorage)
        {
            _logger.LogInformation("Repository: Creating file storage {FileName}", fileStorage.FileName);
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    fileStorage.UploadedAt = DateTime.UtcNow;
                    _context.FileStorages.Add(fileStorage);
                    await _context.SaveChangesAsync();
                    return fileStorage;
                }
            );
        }

        public async Task<bool> DeleteEmployeeFileAsync(int employeeFileId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var employeeFile = await _context.EmployeeFiles.FindAsync(employeeFileId);
                    if (employeeFile == null)
                    {
                        _logger.LogWarning("Repository: Employee file {EmployeeFileId} not found", employeeFileId);
                        return false;
                    }

                    _context.EmployeeFiles.Remove(employeeFile);
                    await _context.SaveChangesAsync();
                    return true;
                }
            );
        }

        public async Task<bool> DeleteFileStorageAsync(int fileStorageId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var fileStorage = await _context.FileStorages.FindAsync(fileStorageId);
                    if (fileStorage == null)
                    {
                        _logger.LogWarning("Repository: File storage {FileStorageId} not found", fileStorageId);
                        return false;
                    }

                    _context.FileStorages.Remove(fileStorage);
                    await _context.SaveChangesAsync();
                    return true;
                }
            );
        }

        public async Task<bool> FileExistsAsync(int fileStorageId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.FileStorages.AnyAsync(fs => fs.FileStorageId == fileStorageId)
            );
        }
    }
}