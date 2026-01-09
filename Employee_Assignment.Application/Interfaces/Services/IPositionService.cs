using Employee_Assignment.Application.DTOs.Position;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IPositionService
    {
        Task<IEnumerable<PositionDto>> GetAllPositionsAsync();
    }
}