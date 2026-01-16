using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Employee_Assignment.Infrastructure.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleRepository> _logger;
        private readonly ICircuitBreakerService _circuitBreakerService;

        public RoleRepository(
            ApplicationDbContext context,
            ILogger<RoleRepository> logger,
            ICircuitBreakerService circuitBreakerService)
        {
            _context = context;
            _logger = logger;
            _circuitBreakerService = circuitBreakerService;
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == name)
            );
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Roles.FindAsync(id)
            );
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Roles.AnyAsync(r => r.RoleId == id)
            );
        }

        public async Task<UserRole> AssignRoleToUserAsync(int userId, int roleId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var userRole = new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    return userRole;
                }
            );
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role)
                    .ToListAsync()
            );
        }
    }
}