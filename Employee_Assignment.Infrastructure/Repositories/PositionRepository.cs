using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Employee_Assignment.Infrastructure.Resilience;
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
                async () => await _context.Positions.FindAsync(id)
            );
        }

        public async Task<Position?> GetByNameAsync(string name)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions
                    .FirstOrDefaultAsync(p => p.Name == name)
            );
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _circuitBreakerService.ExecuteAsync(
                async () => await _context.Positions.AnyAsync(p => p.PositionId == id)
            );
        }
    }
}