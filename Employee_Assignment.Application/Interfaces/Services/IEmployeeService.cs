using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetAllAsync();

        Task<Employee?> GetByIdAsync(int id);

        Task<Employee> CreateAsync(Employee employee);

        Task<Employee?> UpdateAsync(int id, Employee employee);

        Task<bool> DeleteAsync(int id);

        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    }
}
