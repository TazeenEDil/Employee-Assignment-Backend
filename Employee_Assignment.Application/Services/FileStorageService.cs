using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.FileStorage;
using Employee_Assignment.Application.Exceptions;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IFileStorageRepository _repository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _uploadPath;

        public FileStorageService(
            IFileStorageRepository repository,
            IEmployeeRepository employeeRepository,
            ILogger<FileStorageService> logger)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _logger = logger;

            // Set upload path to wwwroot/uploads
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Ensure directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<PaginatedResponse<EmployeeFileDto>> GetAllEmployeeFilesAsync(PaginationRequest request)
        {
            _logger.LogInformation("Service: Fetching paginated employee files");

            var allFiles = await _repository.GetAllEmployeeFilesAsync();
            var totalCount = allFiles.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var fileDtos = allFiles.Select(ef => new EmployeeFileDto
            {
                EmployeeFileId = ef.EmployeeFileId,
                EmployeeId = ef.EmployeeId,
                EmployeeName = ef.Employee.Name,
                FileStorageId = ef.FileStorageId,
                FileName = ef.FileStorage.FileName,
                FileType = ef.FileStorage.FileType,
                FileSize = ef.FileStorage.FileSize,
                FileCategory = ef.FileCategory,
                FileStatus = ef.FileStorage.FileStatus,
                UploadedAt = ef.FileStorage.UploadedAt,
                AssignedAt = ef.AssignedAt
            });

            var paginatedFiles = fileDtos
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PaginatedResponse<EmployeeFileDto>
            {
                Items = paginatedFiles,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<IEnumerable<EmployeeFileDto>> GetFilesByEmployeeIdAsync(int employeeId)
        {
            _logger.LogInformation("Service: Fetching files for employee {EmployeeId}", employeeId);

            var files = await _repository.GetFilesByEmployeeIdAsync(employeeId);

            return files.Select(ef => new EmployeeFileDto
            {
                EmployeeFileId = ef.EmployeeFileId,
                EmployeeId = ef.EmployeeId,
                EmployeeName = ef.Employee.Name,
                FileStorageId = ef.FileStorageId,
                FileName = ef.FileStorage.FileName,
                FileType = ef.FileStorage.FileType,
                FileSize = ef.FileStorage.FileSize,
                FileCategory = ef.FileCategory,
                FileStatus = ef.FileStorage.FileStatus,
                UploadedAt = ef.FileStorage.UploadedAt,
                AssignedAt = ef.AssignedAt
            });
        }

        public async Task<EmployeeFileDto> UploadFileAsync(UploadFileDto uploadDto)
        {
            _logger.LogInformation("Service: Uploading file for employee {EmployeeId}", uploadDto.EmployeeId);

            // Validate employee exists
            if (!await _employeeRepository.EmployeeExistsAsync(uploadDto.EmployeeId))
                throw new NotFoundException("Employee", uploadDto.EmployeeId);

            // Validate file
            if (uploadDto.FileStream == null || uploadDto.FileStream.Length == 0)
                throw new ArgumentException("File is required");

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(uploadDto.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

            if (uploadDto.FileSize > 10485760) // 10MB
                throw new ArgumentException("File size exceeds 10MB limit");

            // Create folder structure: Year/Month/Date
            var now = DateTime.UtcNow;
            var yearFolder = now.Year.ToString();
            var monthFolder = now.Month.ToString("D2");
            var dateFolder = now.Day.ToString("D2");

            var fullPath = Path.Combine(_uploadPath, yearFolder, monthFolder, dateFolder);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(fullPath, uniqueFileName);
            var relativePath = Path.Combine(yearFolder, monthFolder, dateFolder, uniqueFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await uploadDto.FileStream.CopyToAsync(fileStream);
            }

            // Create FileStorage record
            var fileStorage = new FileStorage
            {
                FileName = uploadDto.FileName,
                FilePath = relativePath,
                FileType = fileExtension.TrimStart('.'),
                FileSize = uploadDto.FileSize,
                FileStatus = "Active"
            };

            var createdFileStorage = await _repository.CreateFileStorageAsync(fileStorage);

            // Create EmployeeFile record
            var employeeFile = new EmployeeFile
            {
                EmployeeId = uploadDto.EmployeeId,
                FileStorageId = createdFileStorage.FileStorageId,
                FileCategory = uploadDto.FileCategory ?? "General"
            };

            var createdEmployeeFile = await _repository.CreateEmployeeFileAsync(employeeFile);

            return new EmployeeFileDto
            {
                EmployeeFileId = createdEmployeeFile.EmployeeFileId,
                EmployeeId = createdEmployeeFile.EmployeeId,
                EmployeeName = createdEmployeeFile.Employee.Name,
                FileStorageId = createdEmployeeFile.FileStorageId,
                FileName = createdEmployeeFile.FileStorage.FileName,
                FileType = createdEmployeeFile.FileStorage.FileType,
                FileSize = createdEmployeeFile.FileStorage.FileSize,
                FileCategory = createdEmployeeFile.FileCategory,
                FileStatus = createdEmployeeFile.FileStorage.FileStatus,
                UploadedAt = createdEmployeeFile.FileStorage.UploadedAt,
                AssignedAt = createdEmployeeFile.AssignedAt
            };
        }

        public async Task<FileDownloadDto?> DownloadFileAsync(int employeeFileId)
        {
            _logger.LogInformation("Service: Downloading file {EmployeeFileId}", employeeFileId);

            var employeeFile = await _repository.GetEmployeeFileByIdAsync(employeeFileId);
            if (employeeFile == null)
                throw new NotFoundException("Employee File", employeeFileId);

            var fullPath = Path.Combine(_uploadPath, employeeFile.FileStorage.FilePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Physical file not found on server");

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var contentType = GetContentType(employeeFile.FileStorage.FileType);

            return new FileDownloadDto
            {
                FileContent = fileBytes,
                FileName = employeeFile.FileStorage.FileName,
                ContentType = contentType
            };
        }

        public async Task<bool> DeleteFileAsync(int employeeFileId)
        {
            _logger.LogWarning("Service: Deleting file {EmployeeFileId}", employeeFileId);

            var employeeFile = await _repository.GetEmployeeFileByIdAsync(employeeFileId);
            if (employeeFile == null)
                throw new NotFoundException("Employee File", employeeFileId);

            // Delete physical file
            var fullPath = Path.Combine(_uploadPath, employeeFile.FileStorage.FilePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Delete database records
            await _repository.DeleteEmployeeFileAsync(employeeFileId);
            await _repository.DeleteFileStorageAsync(employeeFile.FileStorageId);

            return true;
        }

        public async Task<string> GetFilePreviewUrlAsync(int employeeFileId)
        {
            var employeeFile = await _repository.GetEmployeeFileByIdAsync(employeeFileId);
            if (employeeFile == null)
                throw new NotFoundException("Employee File", employeeFileId);

            return $"/uploads/{employeeFile.FileStorage.FilePath.Replace("\\", "/")}";
        }

        private string GetContentType(string fileType)
        {
            return fileType.ToLowerInvariant() switch
            {
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}