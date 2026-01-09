using Employee_Assignment.Application.Exceptions;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Interfaces;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IPositionRepository _positionRepository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IEmployeeRepository repository,
            IPositionRepository positionRepository,
            ILogger<EmployeeService> logger)
        {
            _repository = repository;
            _positionRepository = positionRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            _logger.LogInformation("Service: Fetching all employees");
            return await _repository.GetAllEmployeesAsync();
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Service: Fetching employee with ID {EmployeeId}", id);
            var employee = await _repository.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                throw new NotFoundException("Employee", id);
            }

            return employee;
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            _logger.LogInformation("Service: Creating employee with email {Email}", employee.Email);

            if (await _repository.EmailExistsAsync(employee.Email))
            {
                throw new DuplicateException("Employee", "Email", employee.Email);
            }

            if (!await _positionRepository.ExistsAsync(employee.PositionId))
            {
                throw new NotFoundException("Position", employee.PositionId);
            }

            return await _repository.CreateEmployeeAsync(employee);
        }

        public async Task<Employee?> UpdateAsync(int id, Employee employee)
        {
            _logger.LogInformation("Service: Updating employee with ID {EmployeeId}", id);

            if (!await _repository.EmployeeExistsAsync(id))
            {
                throw new NotFoundException("Employee", id);
            }

            if (await _repository.EmailExistsAsync(employee.Email, id))
            {
                throw new DuplicateException("Employee", "Email", employee.Email);
            }

            if (!await _positionRepository.ExistsAsync(employee.PositionId))
            {
                throw new NotFoundException("Position", employee.PositionId);
            }

            return await _repository.UpdateEmployeeAsync(id, employee);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogWarning("Service: Deleting employee with ID {EmployeeId}", id);

            if (!await _repository.EmployeeExistsAsync(id))
            {
                throw new NotFoundException("Employee", id);
            }

            return await _repository.DeleteEmployeeAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            return await _repository.EmailExistsAsync(email, excludeId);
        }
    }
}