using Employee_Assignment.Application.DTOs.Common;
using Employee_Assignment.Application.DTOs.Position;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IPositionService
    {
        Task<IEnumerable<PositionDto>> GetAllPositionsAsync();
        Task<PaginatedResponse<PositionDetailDto>> GetPositionsPaginatedAsync(PaginationRequest request);
        Task<PositionDetailDto?> GetPositionByIdAsync(int id);
        Task<PositionDto> CreatePositionAsync(CreatePositionDto dto);
        Task<PositionDto?> UpdatePositionAsync(int id, UpdatePositionDto dto);
        Task<bool> DeletePositionAsync(int id);
    }
}