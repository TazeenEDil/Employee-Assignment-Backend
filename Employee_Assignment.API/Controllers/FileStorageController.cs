using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.FileStorage;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStorageController : ControllerBase
    {
        private readonly IFileStorageService _service;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<FileStorageController> _logger;

        public FileStorageController(
            IFileStorageService service,
            IEmployeeService employeeService,
            ILogger<FileStorageController> logger)
        {
            _service = service;
            _employeeService = employeeService;
            _logger = logger;
        }

        // Helper method to get current user's email
        private string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("email")?.Value;
        }

        // Helper method to check if user is admin
        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        // Helper method to get employee ID from logged-in user's email
        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return null;

            try
            {
                var employees = await _employeeService.GetAllAsync();
                var employee = employees.FirstOrDefault(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                return employee?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee ID for email {Email}", email);
                return null;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAllFiles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1)
                {
                    return BadRequest(new { message = "Page number and page size must be greater than 0" });
                }

                // If employee, only show their files
                if (!IsAdmin())
                {
                    var employeeId = await GetCurrentEmployeeIdAsync();
                    if (!employeeId.HasValue)
                    {
                        return Unauthorized(new { message = "Could not identify employee" });
                    }

                    _logger.LogInformation("Employee {EmployeeId} requesting their files", employeeId.Value);
                    var employeeFiles = await _service.GetFilesByEmployeeIdAsync(employeeId.Value);

                    // Manual pagination for employee files
                    var totalCount = employeeFiles.Count();
                    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                    var paginatedItems = employeeFiles
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    var result = new PaginatedResponse<EmployeeFileDto>
                    {
                        Items = paginatedItems,
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages
                    };

                    return Ok(result);
                }

                // Admin can see all files
                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var allFilesResult = await _service.GetAllEmployeeFilesAsync(request);
                return Ok(allFilesResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files");
                return StatusCode(500, new { message = "An error occurred while retrieving files" });
            }
        }

        [HttpGet("employee/{employeeId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetFilesByEmployee(int employeeId)
        {
            try
            {
                // Employees can only view their own files
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue)
                    {
                        return Unauthorized(new { message = "Could not identify employee" });
                    }

                    if (currentEmployeeId.Value != employeeId)
                    {
                        _logger.LogWarning("Employee {CurrentId} attempted to access files of employee {RequestedId}",
                            currentEmployeeId.Value, employeeId);
                        return Forbid();
                    }
                }

                _logger.LogInformation("API: Get files for employee {EmployeeId}", employeeId);
                var files = await _service.GetFilesByEmployeeIdAsync(employeeId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for employee {EmployeeId}", employeeId);
                return StatusCode(500, new { message = "An error occurred while retrieving files" });
            }
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFile([FromForm] int employeeId, [FromForm] IFormFile file, [FromForm] string? category)
        {
            try
            {
                _logger.LogInformation("API: Upload file for employee {EmployeeId}", employeeId);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                var uploadDto = new UploadFileDto
                {
                    EmployeeId = employeeId,
                    FileStream = file.OpenReadStream(),
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FileCategory = category
                };

                var result = await _service.UploadFileAsync(uploadDto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { message = "An error occurred while uploading the file" });
            }
        }

        [HttpGet("download/{employeeFileId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DownloadFile(int employeeFileId)
        {
            try
            {
                _logger.LogInformation("API: Download file {EmployeeFileId}", employeeFileId);

                // Check if employee is authorized to download this file
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue)
                    {
                        return Unauthorized(new { message = "Could not identify employee" });
                    }

                    // Get file info to check ownership
                    var allFiles = await _service.GetFilesByEmployeeIdAsync(currentEmployeeId.Value);
                    var fileExists = allFiles.Any(f => f.EmployeeFileId == employeeFileId);

                    if (!fileExists)
                    {
                        _logger.LogWarning("Employee {EmployeeId} attempted to download unauthorized file {FileId}",
                            currentEmployeeId.Value, employeeFileId);
                        return Forbid();
                    }
                }

                var result = await _service.DownloadFileAsync(employeeFileId);
                if (result == null)
                    return NotFound(new { message = "File not found" });

                return File(result.FileContent, result.ContentType, result.FileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {EmployeeFileId}", employeeFileId);
                return StatusCode(500, new { message = "An error occurred while downloading the file" });
            }
        }

        [HttpGet("preview/{employeeFileId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetPreviewUrl(int employeeFileId)
        {
            try
            {
                // Check if employee is authorized to preview this file
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue)
                    {
                        return Unauthorized(new { message = "Could not identify employee" });
                    }

                    var allFiles = await _service.GetFilesByEmployeeIdAsync(currentEmployeeId.Value);
                    var fileExists = allFiles.Any(f => f.EmployeeFileId == employeeFileId);

                    if (!fileExists)
                    {
                        _logger.LogWarning("Employee {EmployeeId} attempted to preview unauthorized file {FileId}",
                            currentEmployeeId.Value, employeeFileId);
                        return Forbid();
                    }
                }

                var url = await _service.GetFilePreviewUrlAsync(employeeFileId);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview URL for {EmployeeFileId}", employeeFileId);
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpDelete("{employeeFileId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFile(int employeeFileId)
        {
            try
            {
                _logger.LogWarning("API: Delete file {EmployeeFileId}", employeeFileId);
                await _service.DeleteFileAsync(employeeFileId);
                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {EmployeeFileId}", employeeFileId);
                return StatusCode(500, new { message = "An error occurred while deleting the file" });
            }
        }

        // NEW: Endpoint for employees to get their own files easily
        [HttpGet("my-files")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyFiles()
        {
            try
            {
                var employeeId = await GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue)
                {
                    return Unauthorized(new { message = "Could not identify employee" });
                }

                _logger.LogInformation("Employee {EmployeeId} requesting their files", employeeId.Value);
                var files = await _service.GetFilesByEmployeeIdAsync(employeeId.Value);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee's files");
                return StatusCode(500, new { message = "An error occurred while retrieving files" });
            }
        }
    }
}