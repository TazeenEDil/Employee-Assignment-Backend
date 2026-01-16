using Employee_Assignment.Application.DTOs.Auth;
using Employee_Assignment.Domain.Entities;

namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        string GenerateJwtToken(User user, List<string> roles);
    }
}