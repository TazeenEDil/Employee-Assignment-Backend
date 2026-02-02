using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.FileStorage;
using Employee_Assignment.Application.Exceptions;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Hosting;
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
            ILogger<FileStorageService> logger,
            IHostEnvironment env)
        {
            _repository = repository;
            _employeeRepository = employeeRepository;
            _logger = logger;

            // IHostEnvironment doesn't have WebRootPath, so we build it manually
            var webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadPath = Path.Combine(webRoot, "uploads");

            _logger.LogInformation("ContentRootPath: {ContentRootPath}", env.ContentRootPath);
            _logger.LogInformation("Upload path configured as: {UploadPath}", _uploadPath);

            try
            {
                if (!Directory.Exists(_uploadPath))
                {
                    _logger.LogInformation("Creating upload directory: {UploadPath}", _uploadPath);
                    Directory.CreateDirectory(_uploadPath);
                    _logger.LogInformation("Upload directory created successfully");
                }
                else
                {
                    _logger.LogInformation("Upload directory already exists: {UploadPath}", _uploadPath);
                }

                // Test write permissions
                var testFile = Path.Combine(_uploadPath, "test_permissions.txt");
                File.WriteAllText(testFile, "Permission test");
                File.Delete(testFile);
                _logger.LogInformation("Write permissions verified for upload directory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize upload directory or verify permissions: {UploadPath}", _uploadPath);
                throw new InvalidOperationException($"Failed to initialize upload directory: {ex.Message}", ex);
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
            try
            {
                _logger.LogInformation("=== Service: UploadFileAsync Started ===");
                _logger.LogInformation("EmployeeId: {EmployeeId}", uploadDto.EmployeeId);
                _logger.LogInformation("FileName: {FileName}", uploadDto.FileName);
                _logger.LogInformation("FileSize: {FileSize} bytes", uploadDto.FileSize);
                _logger.LogInformation("FileCategory: {FileCategory}", uploadDto.FileCategory);
                _logger.LogInformation("Upload Path: {UploadPath}", _uploadPath);

                // Validate employee exists
                _logger.LogInformation("Checking if employee exists...");
                if (!await _employeeRepository.EmployeeExistsAsync(uploadDto.EmployeeId))
                {
                    _logger.LogError("Employee {EmployeeId} does not exist", uploadDto.EmployeeId);
                    throw new NotFoundException("Employee", uploadDto.EmployeeId);
                }
                _logger.LogInformation("Employee exists - validation passed");

                // Validate file
                if (uploadDto.FileStream == null || uploadDto.FileStream.Length == 0)
                {
                    _logger.LogError("FileStream is null or empty");
                    throw new ArgumentException("File is required");
                }

                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
                var fileExtension = Path.GetExtension(uploadDto.FileName).ToLowerInvariant();

                _logger.LogInformation("File extension: {FileExtension}", fileExtension);

                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogError("Invalid file extension: {FileExtension}", fileExtension);
                    throw new ArgumentException($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                }

                if (uploadDto.FileSize > 10485760) // 10MB
                {
                    _logger.LogError("File size {FileSize} exceeds limit", uploadDto.FileSize);
                    throw new ArgumentException("File size exceeds 10MB limit");
                }

                // Create folder structure: Year/Month/Date
                var now = DateTime.UtcNow;
                var yearFolder = now.Year.ToString();
                var monthFolder = now.Month.ToString("D2");
                var dateFolder = now.Day.ToString("D2");

                var fullPath = Path.Combine(_uploadPath, yearFolder, monthFolder, dateFolder);
                _logger.LogInformation("Creating directory structure: {FullPath}", fullPath);

                try
                {
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                        _logger.LogInformation("Directory created successfully");
                    }
                    else
                    {
                        _logger.LogInformation("Directory already exists");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {FullPath}", fullPath);
                    throw new Exception($"Failed to create upload directory: {ex.Message}", ex);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(fullPath, uniqueFileName);
                var relativePath = Path.Combine(yearFolder, monthFolder, dateFolder, uniqueFileName);

                _logger.LogInformation("Saving file to: {FilePath}", filePath);
                _logger.LogInformation("Relative path: {RelativePath}", relativePath);

                // Save file to disk
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await uploadDto.FileStream.CopyToAsync(fileStream);
                        await fileStream.FlushAsync();
                    }
                    _logger.LogInformation("File saved successfully to disk");

                    // Verify file was created
                    if (!File.Exists(filePath))
                    {
                        throw new Exception("File was not created on disk");
                    }

                    var fileInfo = new FileInfo(filePath);
                    _logger.LogInformation("File verified on disk. Size: {Size} bytes", fileInfo.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving file to disk at path: {FilePath}", filePath);
                    throw new Exception($"Failed to save file to disk: {ex.Message}", ex);
                }

                // Create FileStorage record
                _logger.LogInformation("Creating FileStorage database record...");
                var fileStorage = new FileStorage
                {
                    FileName = uploadDto.FileName,
                    FilePath = relativePath,
                    FileType = fileExtension.TrimStart('.'),
                    FileSize = uploadDto.FileSize,
                    FileStatus = "Active"
                };

                FileStorage createdFileStorage;
                try
                {
                    createdFileStorage = await _repository.CreateFileStorageAsync(fileStorage);
                    _logger.LogInformation("FileStorage record created with ID: {FileStorageId}", createdFileStorage.FileStorageId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating FileStorage database record");
                    // Clean up the physical file if database insert fails
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                            _logger.LogInformation("Cleaned up physical file after database error");
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogError(deleteEx, "Failed to delete file during cleanup: {FilePath}", filePath);
                        }
                    }
                    throw new Exception($"Failed to create FileStorage record: {ex.Message}", ex);
                }

                // Create EmployeeFile record
                _logger.LogInformation("Creating EmployeeFile database record...");
                var employeeFile = new EmployeeFile
                {
                    EmployeeId = uploadDto.EmployeeId,
                    FileStorageId = createdFileStorage.FileStorageId,
                    FileCategory = uploadDto.FileCategory ?? "General"
                };

                EmployeeFile createdEmployeeFile;
                try
                {
                    createdEmployeeFile = await _repository.CreateEmployeeFileAsync(employeeFile);
                    _logger.LogInformation("EmployeeFile record created with ID: {EmployeeFileId}", createdEmployeeFile.EmployeeFileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating EmployeeFile database record");
                    // Clean up FileStorage record and physical file
                    try
                    {
                        await _repository.DeleteFileStorageAsync(createdFileStorage.FileStorageId);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        _logger.LogInformation("Cleaned up FileStorage record and physical file after error");
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Error during cleanup");
                    }
                    throw new Exception($"Failed to create EmployeeFile record: {ex.Message}", ex);
                }

                _logger.LogInformation("=== Service: UploadFileAsync Completed Successfully ===");

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== Service: UploadFileAsync Failed ===");
                throw;
            }
        }

        public async Task<FileDownloadDto?> DownloadFileAsync(int employeeFileId)
        {
            _logger.LogInformation("Service: Downloading file {EmployeeFileId}", employeeFileId);

            var employeeFile = await _repository.GetEmployeeFileByIdAsync(employeeFileId);
            if (employeeFile == null)
                throw new NotFoundException("Employee File", employeeFileId);

            var fullPath = Path.Combine(_uploadPath, employeeFile.FileStorage.FilePath);

            _logger.LogInformation("Download file path: {FullPath}", fullPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogError("Physical file not found at: {FullPath}", fullPath);
                throw new FileNotFoundException("Physical file not found on server");
            }

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var contentType = GetContentType(employeeFile.FileStorage.FileType);

            _logger.LogInformation("File downloaded successfully. Size: {Size} bytes", fileBytes.Length);

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
            _logger.LogInformation("Attempting to delete physical file: {FullPath}", fullPath);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Physical file deleted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file: {FullPath}", fullPath);
                    throw new Exception($"Failed to delete physical file: {ex.Message}", ex);
                }
            }
            else
            {
                _logger.LogWarning("Physical file not found: {FullPath}", fullPath);
            }

            // Delete database records
            try
            {
                await _repository.DeleteEmployeeFileAsync(employeeFileId);
                await _repository.DeleteFileStorageAsync(employeeFile.FileStorageId);
                _logger.LogInformation("Database records deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete database records");
                throw new Exception($"Failed to delete database records: {ex.Message}", ex);
            }

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