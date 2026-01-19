using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.FileStorage;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStorageController : ControllerBase
    {
        private readonly IFileStorageService _service;
        private readonly ILogger<FileStorageController> _logger;

        public FileStorageController(
            IFileStorageService service,
            ILogger<FileStorageController> logger)
        {
            _service = service;
            _logger = logger;
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

                var request = new PaginationRequest
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var result = await _service.GetAllEmployeeFilesAsync(request);
                return Ok(result);
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
        [RequestSizeLimit(10_485_760)] // 10MB limit
        public async Task<IActionResult> UploadFile([FromForm] int employeeId, [FromForm] IFormFile file, [FromForm] string? category)
        {
            try
            {
                _logger.LogInformation("API: Upload file for employee {EmployeeId}", employeeId);

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                // Create UploadFileDto from IFormFile
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
    }
}