using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Infrastructure.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PositionRepository> _logger;
        private readonly ICircuitBreakerService _circuitBreakerService;

        public PositionRepository(
            ApplicationDbContext context,
            ILogger<PositionRepository> logger,
            ICircuitBreakerService circuitBreakerService)
        {
            _context = context;
            _logger = logger;
            _circuitBreakerService = circuitBreakerService;
        }

        public async Task<IEnumerable<Position>> GetAllAsync()
        {
            _logger.LogInformation("Repository: Fetching all positions");
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions
                    .OrderBy(p => p.Name)
                    .ToListAsync()
            );
        }

        public async Task<Position?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Repository: Fetch position {PositionId}", id);
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions
                    .Include(p => p.Employees)
                    .FirstOrDefaultAsync(p => p.PositionId == id)
            );
        }

        public async Task<Position?> GetByNameAsync(string name)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions
                    .FirstOrDefaultAsync(p => p.Name == name)
            );
        }

        public async Task<Position> CreateAsync(Position position)
        {
            _logger.LogInformation("Repository: Creating position {Name}", position.Name);
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    position.CreatedAt = DateTime.UtcNow;
                    _context.Positions.Add(position);
                    await _context.SaveChangesAsync();
                    return position;
                }
            );
        }

        public async Task<Position?> UpdateAsync(int id, Position position)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var existing = await _context.Positions.FindAsync(id);
                    if (existing == null)
                    {
                        _logger.LogWarning("Repository: Position {PositionId} not found for update", id);
                        return null;
                    }

                    existing.Name = position.Name;
                    existing.Description = position.Description;
                    await _context.SaveChangesAsync();
                    return existing;
                }
            );
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    var position = await _context.Positions.FindAsync(id);
                    if (position == null)
                    {
                        _logger.LogWarning("Repository: Position {PositionId} not found for delete", id);
                        return false;
                    }

                    _context.Positions.Remove(position);
                    await _context.SaveChangesAsync();
                    return true;
                }
            );
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions.AnyAsync(p => p.PositionId == id)
            );
        }

        public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () =>
                {
                    return excludeId.HasValue
                        ? await _context.Positions.AnyAsync(p => p.Name == name && p.PositionId != excludeId)
                        : await _context.Positions.AnyAsync(p => p.Name == name);
                }
            );
        }

        public async Task<int> GetEmployeeCountByPositionAsync(int positionId)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Employees.CountAsync(e => e.PositionId == positionId)
            );
        }
    }
}