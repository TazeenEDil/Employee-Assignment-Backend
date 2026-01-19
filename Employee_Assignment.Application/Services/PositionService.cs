using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.Position;
using Employee_Assignment.Application.Exceptions;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
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

        public async Task<PaginatedResponse<PositionDetailDto>> GetPositionsPaginatedAsync(PaginationRequest request)
        {
            _logger.LogInformation("Service: Fetching paginated positions");
            var allPositions = await _repository.GetAllAsync();
            var totalCount = allPositions.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var positionDtos = new List<PositionDetailDto>();
            foreach (var position in allPositions)
            {
                var employeeCount = await _repository.GetEmployeeCountByPositionAsync(position.PositionId);
                positionDtos.Add(new PositionDetailDto
                {
                    PositionId = position.PositionId,
                    Name = position.Name,
                    Description = position.Description,
                    CreatedAt = position.CreatedAt,
                    EmployeeCount = employeeCount
                });
            }

            var paginatedPositions = positionDtos
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PaginatedResponse<PositionDetailDto>
            {
                Items = paginatedPositions,
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        public async Task<PositionDetailDto?> GetPositionByIdAsync(int id)
        {
            _logger.LogInformation("Service: Fetching position {PositionId}", id);
            var position = await _repository.GetByIdAsync(id);
            if (position == null)
                throw new NotFoundException("Position", id);

            var employeeCount = await _repository.GetEmployeeCountByPositionAsync(id);

            return new PositionDetailDto
            {
                PositionId = position.PositionId,
                Name = position.Name,
                Description = position.Description,
                CreatedAt = position.CreatedAt,
                EmployeeCount = employeeCount
            };
        }

        public async Task<PositionDto> CreatePositionAsync(CreatePositionDto dto)
        {
            _logger.LogInformation("Service: Creating position {Name}", dto.Name);

            if (await _repository.NameExistsAsync(dto.Name))
                throw new DuplicateException("Position", "Name", dto.Name);

            var position = new Position
            {
                Name = dto.Name,
                Description = dto.Description
            };

            var created = await _repository.CreateAsync(position);

            return new PositionDto
            {
                PositionId = created.PositionId,
                Name = created.Name,
                Description = created.Description
            };
        }

        public async Task<PositionDto?> UpdatePositionAsync(int id, UpdatePositionDto dto)
        {
            _logger.LogInformation("Service: Updating position {PositionId}", id);

            if (!await _repository.ExistsAsync(id))
                throw new NotFoundException("Position", id);

            if (await _repository.NameExistsAsync(dto.Name, id))
                throw new DuplicateException("Position", "Name", dto.Name);

            var position = new Position
            {
                Name = dto.Name,
                Description = dto.Description
            };

            var updated = await _repository.UpdateAsync(id, position);
            if (updated == null)
                return null;

            return new PositionDto
            {
                PositionId = updated.PositionId,
                Name = updated.Name,
                Description = updated.Description
            };
        }

        public async Task<bool> DeletePositionAsync(int id)
        {
            _logger.LogWarning("Service: Deleting position {PositionId}", id);

            if (!await _repository.ExistsAsync(id))
                throw new NotFoundException("Position", id);

            var employeeCount = await _repository.GetEmployeeCountByPositionAsync(id);
            if (employeeCount > 0)
                throw new InvalidOperationException($"Cannot delete position. {employeeCount} employees are assigned to this position.");

            return await _repository.DeleteAsync(id);
        }
    }
}