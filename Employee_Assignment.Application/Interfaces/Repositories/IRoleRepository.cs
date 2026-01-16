using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByNameAsync(string name);
        Task<Role?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<UserRole> AssignRoleToUserAsync(int userId, int roleId);
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);
    }
}