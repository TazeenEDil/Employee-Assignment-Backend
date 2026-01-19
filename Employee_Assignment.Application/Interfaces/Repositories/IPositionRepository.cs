using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IPositionRepository
    {
        Task<IEnumerable<Position>> GetAllAsync();
        Task<Position?> GetByIdAsync(int id);
        Task<Position?> GetByNameAsync(string name);
        Task<Position> CreateAsync(Position position);
        Task<Position?> UpdateAsync(int id, Position position);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task<int> GetEmployeeCountByPositionAsync(int positionId);
    }
}