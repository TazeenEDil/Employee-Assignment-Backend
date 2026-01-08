using Employee_Assignment.Data;
using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace Employee_Assignment.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(
            ApplicationDbContext context,
            ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            _logger.LogInformation("Repository: Fetching all employees");
            return await _context.Employees.OrderBy(e => e.Name).ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("Repository: Fetch employee {EmployeeId}", id);
            return await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            _logger.LogInformation("Repository: Creating employee {Email}", employee.Email);

            employee.CreatedAt = DateTime.UtcNow;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return employee;
        }

        public async Task<Employee?> UpdateEmployeeAsync(int id, Employee employee)
        {
            var existing = await _context.Employees.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Repository: Employee {EmployeeId} not found for update", id);
                return null;
            }

            existing.Name = employee.Name;
            existing.Email = employee.Email;
            existing.Position = employee.Position;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                _logger.LogWarning("Repository: Employee {EmployeeId} not found for delete", id);
                return false;
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmployeeExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            return excludeId.HasValue
                ? await _context.Employees.AnyAsync(e => e.Email == email && e.Id != excludeId)
                : await _context.Employees.AnyAsync(e => e.Email == email);
        }
    }
}
