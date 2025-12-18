using Employee_Assignment.DTOs;
using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeRepository _repository;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IEmployeeRepository repository,
            ILogger<EmployeesController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EmployeeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployees()
        {
            try
            {
                var employees = await _repository.GetAllEmployeesAsync();
                var employeeDtos = employees.Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Email = e.Email,
                    Position = e.Position,
                    CreatedAt = e.CreatedAt
                });

                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                return StatusCode(500, "An error occurred while retrieving employees");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                var employee = await _repository.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    return NotFound(new { message = $"Employee with ID {id} not found" });
                }

                var employeeDto = new EmployeeDto
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Email = employee.Email,
                    Position = employee.Position,
                    CreatedAt = employee.CreatedAt
                };

                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {EmployeeId}", id);
                return StatusCode(500, "An error occurred while retrieving the employee");
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email already exists
                if (await _repository.EmailExistsAsync(createDto.Email))
                {
                    return BadRequest(new { message = "An employee with this email already exists" });
                }

                var employee = new Employee
                {
                    Name = createDto.Name,
                    Email = createDto.Email,
                    Position = createDto.Position
                };

                var createdEmployee = await _repository.CreateEmployeeAsync(employee);

                var employeeDto = new EmployeeDto
                {
                    Id = createdEmployee.Id,
                    Name = createdEmployee.Name,
                    Email = createdEmployee.Email,
                    Position = createdEmployee.Position,
                    CreatedAt = createdEmployee.CreatedAt
                };

                return CreatedAtAction(
                    nameof(GetEmployee),
                    new { id = employeeDto.Id },
                    employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                return StatusCode(500, "An error occurred while creating the employee");
            }
        }

      
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if email already exists for another employee
                if (await _repository.EmailExistsAsync(updateDto.Email, id))
                {
                    return BadRequest(new { message = "An employee with this email already exists" });
                }

                var employee = new Employee
                {
                    Name = updateDto.Name,
                    Email = updateDto.Email,
                    Position = updateDto.Position
                };

                var updatedEmployee = await _repository.UpdateEmployeeAsync(id, employee);
                if (updatedEmployee == null)
                {
                    return NotFound(new { message = $"Employee with ID {id} not found" });
                }

                var employeeDto = new EmployeeDto
                {
                    Id = updatedEmployee.Id,
                    Name = updatedEmployee.Name,
                    Email = updatedEmployee.Email,
                    Position = updatedEmployee.Position,
                    CreatedAt = updatedEmployee.CreatedAt
                };

                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId}", id);
                return StatusCode(500, "An error occurred while updating the employee");
            }
        }

       
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var deleted = await _repository.DeleteEmployeeAsync(id);
                if (!deleted)
                {
                    return NotFound(new { message = $"Employee with ID {id} not found" });
                }

                return Ok(new { message = "Employee deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId}", id);
                return StatusCode(500, "An error occurred while deleting the employee");
            }
        }
    }
}