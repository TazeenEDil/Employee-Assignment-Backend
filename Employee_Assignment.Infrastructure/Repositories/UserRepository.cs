using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly ICircuitBreakerService _circuitBreakerService;

        public UserRepository(
            ApplicationDbContext context,
            ILogger<UserRepository> logger,
            ICircuitBreakerService circuitBreakerService)
        {
            _context = context;
            _logger = logger;
            _circuitBreakerService = circuitBreakerService;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            _logger.LogInformation("Repository: Fetching user by email {Email}", email);

            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == email)
            );
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Repository: Fetching user by ID {UserId}", id);

            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == id)
            );
        }

        public async Task<User> CreateAsync(User user)
        {
            _logger.LogInformation("Repository: Creating user {Email}", user.Email);

            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    user.CreatedAt = DateTime.UtcNow;
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    return user;
                }
            );
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Users.AnyAsync(u => u.Email == email)
            );
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        user.LastLoginAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                    return Task.CompletedTask;
                }
            );
        }
    }
}