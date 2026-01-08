using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;

namespace Employee_Assignment.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IEmployeeRepository repository,
            ILogger<EmployeeService> logger)
        {
            _repository = repository;
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
            return await _repository.GetEmployeeByIdAsync(id);
        }

        public async Task<Employee> CreateAsync(Employee employee)
        {
            _logger.LogInformation("Service: Creating employee with email {Email}", employee.Email);
            return await _repository.CreateEmployeeAsync(employee);
        }

        public async Task<Employee?> UpdateAsync(int id, Employee employee)
        {
            _logger.LogInformation("Service: Updating employee with ID {EmployeeId}", id);
            return await _repository.UpdateEmployeeAsync(id, employee);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogWarning("Service: Deleting employee with ID {EmployeeId}", id);
            return await _repository.DeleteEmployeeAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            return await _repository.EmailExistsAsync(email, excludeId);
        }
    }
}
