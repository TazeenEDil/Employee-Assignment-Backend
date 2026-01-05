using Employee_Assignment.DTOs;
using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.Controllers
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

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")] // Both roles can view list
        public async Task<IActionResult> GetEmployees()
        {
            _logger.LogInformation("API: Get all employees called");
            var employees = await _service.GetAllAsync();
            return Ok(employees.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Position = e.Position,
                CreatedAt = e.CreatedAt
            }));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")] // Both roles can view details
        public async Task<IActionResult> GetEmployee(int id)
        {
            _logger.LogInformation("API: Get employee {EmployeeId}", id);
            var employee = await _service.GetByIdAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Employee {EmployeeId} not found", id);
                return NotFound(new { message = "Employee not found" });
            }

            return Ok(new EmployeeDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Position = employee.Position,
                CreatedAt = employee.CreatedAt
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin can create
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
        {
            _logger.LogInformation("API: Create employee request");
            if (await _service.EmailExistsAsync(dto.Email))
            {
                _logger.LogWarning("Duplicate email detected: {Email}", dto.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            var employee = await _service.CreateAsync(new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                Position = dto.Position
            });

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can update
        public async Task<IActionResult> UpdateEmployee(int id, UpdateEmployeeDto dto)
        {
            _logger.LogInformation("API: Update employee {EmployeeId}", id);
            var updated = await _service.UpdateAsync(id, new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                Position = dto.Position
            });

            if (updated == null)
            {
                _logger.LogWarning("Employee {EmployeeId} not found for update", id);
                return NotFound(new { message = "Employee not found" });
            }

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogWarning("API: Delete employee {EmployeeId}", id);
            if (!await _service.DeleteAsync(id))
            {
                _logger.LogWarning("Employee {EmployeeId} not found for delete", id);
                return NotFound(new { message = "Employee not found" });
            }

            return Ok(new { message = "Employee deleted successfully" });
        }
    }
}