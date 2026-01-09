using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Infrastructure.Repositories
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
            return await _context.Employees
                .Include(e => e.Position)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            _logger.LogInformation("Repository: Fetch employee {EmployeeId}", id);
            return await _context.Employees
                .Include(e => e.Position)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            _logger.LogInformation("Repository: Creating employee {Email}", employee.Email);
            employee.CreatedAt = DateTime.UtcNow;
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Reload to include Position
            return (await GetEmployeeByIdAsync(employee.Id))!;
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
            existing.PositionId = employee.PositionId;

            await _context.SaveChangesAsync();

            // Reload to include Position
            return await GetEmployeeByIdAsync(id);
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