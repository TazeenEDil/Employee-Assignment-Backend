using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task UpdateLastLoginAsync(int userId);
    }
}