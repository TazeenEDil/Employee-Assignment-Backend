using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IPositionRepository
    {
        Task<IEnumerable<Position>> GetAllAsync();
        Task<Position?> GetByIdAsync(int id);
        Task<Position?> GetByNameAsync(string name);
        Task<bool> ExistsAsync(int id);
    }
}
