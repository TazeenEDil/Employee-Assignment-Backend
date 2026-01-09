using Employee_Assignment.Application.DTOs.Position;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.Services
{
    public class PositionService : IPositionService
    {
        private readonly IPositionRepository _repository;
        private readonly ILogger<PositionService> _logger;

        public PositionService(
            IPositionRepository repository,
            ILogger<PositionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<PositionDto>> GetAllPositionsAsync()
        {
            _logger.LogInformation("Service: Fetching all positions");
            var positions = await _repository.GetAllAsync();

            return positions.Select(p => new PositionDto
            {
                PositionId = p.PositionId,
                Name = p.Name,
                Description = p.Description
            });
        }
    }
}