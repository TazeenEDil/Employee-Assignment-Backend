using Employee_Assignment.Application.DTOs.Employee;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _service;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IEmployeeService service,
            ILogger<EmployeesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private string GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email found in claims");
                return null;
            }

            try
            {
                var employees = await _service.GetAllAsync();
                var employee = employees.FirstOrDefault(e =>
                    e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (employee != null)
                {
                    _logger.LogInformation("Found employee ID: " + employee.Id);
                }

                return employee?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee ID");
                return null;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _service.GetAllAsync();

                var result = employees.Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    PositionId = e.PositionId,
                    PositionName = e.Position?.Name ?? "Unknown",
                    CreatedAt = e.CreatedAt
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees");
                return StatusCode(500, new { message = "Error retrieving employees" });
            }
        }

        [HttpGet("me")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var email = GetCurrentUserEmail();

                if (string.IsNullOrEmpty(email))
                {
                    return Unauthorized(new { message = "Email not found in token" });
                }

                var employeeId = await GetCurrentEmployeeIdAsync();

                if (!employeeId.HasValue)
                {
                    return Unauthorized(new { message = "Could not identify employee" });
                }

                var employee = await _service.GetByIdAsync(employeeId.Value);

                if (employee == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }

                var result = new EmployeeDto
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Email = employee.Email,
                    PositionId = employee.PositionId,
                    PositionName = employee.Position?.Name ?? "Unknown",
                    CreatedAt = employee.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile");
                return StatusCode(500, new { message = "Error retrieving profile" });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var employee = await _service.GetByIdAsync(id);

                if (employee == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }

                var result = new EmployeeDto
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Email = employee.Email,
                    PositionId = employee.PositionId,
                    PositionName = employee.Position?.Name ?? "Unknown",
                    CreatedAt = employee.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee");
                return StatusCode(500, new { message = "Error retrieving employee" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
        {
            try
            {
                var employee = await _service.CreateAsync(new Employee
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PositionId = dto.PositionId
                });

                var result = new EmployeeDto
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Email = employee.Email,
                    PositionId = employee.PositionId,
                    PositionName = employee.Position?.Name ?? "Unknown",
                    CreatedAt = employee.CreatedAt
                };

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, new { message = "Error creating employee" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, UpdateEmployeeDto dto)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, new Employee
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PositionId = dto.PositionId
                });

                if (updated == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }

                var result = new EmployeeDto
                {
                    Id = updated.Id,
                    Name = updated.Name,
                    Email = updated.Email,
                    PositionId = updated.PositionId,
                    PositionName = updated.Position?.Name ?? "Unknown",
                    CreatedAt = updated.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return StatusCode(500, new { message = "Error updating employee" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee");
                return StatusCode(500, new { message = "Error deleting employee" });
            }
        }
    }
}