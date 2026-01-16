using Employee_Assignment.Application.DTOs.Employee;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Application.DTOs.Auth;
using Employee_Assignment.Application.DTOs.Position;
using Employee_Assignment.Application.Interfaces;
using Employee_Assignment.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetEmployees()
        {
            _logger.LogInformation("API: Get all employees called");
            var employees = await _service.GetAllAsync();
            return Ok(employees.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                PositionId = e.PositionId,
                PositionName = e.Position?.Name ?? "Unknown",
                CreatedAt = e.CreatedAt
            }));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            _logger.LogInformation("API: Get employee {EmployeeId}", id);
            var employee = await _service.GetByIdAsync(id);

            return Ok(new EmployeeDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                PositionId = employee.PositionId,
                PositionName = employee.Position?.Name ?? "Unknown",
                CreatedAt = employee.CreatedAt
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
        {
            _logger.LogInformation("API: Create employee request");

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

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, UpdateEmployeeDto dto)
        {
            _logger.LogInformation("API: Update employee {EmployeeId}", id);

            var updated = await _service.UpdateAsync(id, new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                PositionId = dto.PositionId
            });

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

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.LogWarning("API: Delete employee {EmployeeId}", id);
            await _service.DeleteAsync(id);
            return Ok(new { message = "Employee deleted successfully" });
        }
    }
}