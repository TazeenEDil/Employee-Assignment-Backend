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

        private string GetCurrentUserEmail() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("email")?.Value;

        private bool IsAdmin() => User.IsInRole("Admin");

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email)) return null;

            try
            {
                var employees = await _employeeService.GetAllAsync();
                return employees.FirstOrDefault(e => e.Email.Equals(email, StringComparison.OrdinalIgnoreCase))?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee ID for email {Email}", email);
                return null;
            }
        }

        // -------------------
        // GET all files
        // -------------------
        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetAllFiles([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1 || pageSize < 1) return BadRequest(new { message = "Page number and page size must be greater than 0" });

                if (!IsAdmin())
                {
                    var employeeId = await GetCurrentEmployeeIdAsync();
                    if (!employeeId.HasValue) return Unauthorized(new { message = "Could not identify employee" });

                    var employeeFiles = await _service.GetFilesByEmployeeIdAsync(employeeId.Value);
                    var totalCount = employeeFiles.Count();
                    var paginatedItems = employeeFiles.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                    var result = new PaginatedResponse<EmployeeFileDto>
                    {
                        Items = paginatedItems,
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    };

                    return Ok(result);
                }

                var request = new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize };
                var allFilesResult = await _service.GetAllEmployeeFilesAsync(request);
                return Ok(allFilesResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files");
                return StatusCode(500, new { message = "An error occurred while retrieving files" });
            }
        }

        // -------------------
        // GET files by employee
        // -------------------
        [HttpGet("employee/{employeeId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetFilesByEmployee(int employeeId)
        {
            try
            {
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue) return Unauthorized(new { message = "Could not identify employee" });
                    if (currentEmployeeId.Value != employeeId) return Forbid();
                }

                var files = await _service.GetFilesByEmployeeIdAsync(employeeId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for employee {EmployeeId}", employeeId);
                return StatusCode(500, new { message = "An error occurred while retrieving files" });
            }
        }

        // -------------------
        // POST upload file
        // -------------------
        [HttpPost("upload")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
        {
            try
            {
                _logger.LogInformation("API: Upload file for employee {EmployeeId}", request.EmployeeId);

                if (request.File == null || request.File.Length == 0)
                    return BadRequest(new { message = "File is required" });

                var uploadDto = new UploadFileDto
                {
                    EmployeeId = request.EmployeeId,
                    FileStream = request.File.OpenReadStream(),
                    FileName = request.File.FileName,
                    ContentType = request.File.ContentType,
                    FileSize = request.File.Length,
                    FileCategory = request.Category
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

        // -------------------
        // GET download file
        // -------------------
        [HttpGet("download/{employeeFileId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> DownloadFile(int employeeFileId)
        {
            try
            {
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue) return Unauthorized(new { message = "Could not identify employee" });

                    var allFiles = await _service.GetFilesByEmployeeIdAsync(currentEmployeeId.Value);
                    if (!allFiles.Any(f => f.EmployeeFileId == employeeFileId)) return Forbid();
                }

                var result = await _service.DownloadFileAsync(employeeFileId);
                if (result == null) return NotFound(new { message = "File not found" });

                return File(result.FileContent, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {EmployeeFileId}", employeeFileId);
                return StatusCode(500, new { message = "An error occurred while downloading the file" });
            }
        }

        // -------------------
        // GET file preview URL
        // -------------------
        [HttpGet("preview/{employeeFileId}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetPreviewUrl(int employeeFileId)
        {
            try
            {
                if (!IsAdmin())
                {
                    var currentEmployeeId = await GetCurrentEmployeeIdAsync();
                    if (!currentEmployeeId.HasValue) return Unauthorized(new { message = "Could not identify employee" });

                    var allFiles = await _service.GetFilesByEmployeeIdAsync(currentEmployeeId.Value);
                    if (!allFiles.Any(f => f.EmployeeFileId == employeeFileId)) return Forbid();
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

        // -------------------
        // DELETE file
        // -------------------
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

        // -------------------
        // GET current employee files
        // -------------------
        [HttpGet("my-files")]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> GetMyFiles()
        {
            try
            {
                var employeeId = await GetCurrentEmployeeIdAsync();
                if (!employeeId.HasValue) return Unauthorized(new { message = "Could not identify employee" });

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
