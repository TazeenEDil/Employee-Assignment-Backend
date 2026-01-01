using Employee_Assignment.DTOs.Auth;
using Employee_Assignment.Models;

namespace Employee_Assignment.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> EmailExistsAsync(string email);
        string GenerateJwtToken(User user);
    }
}