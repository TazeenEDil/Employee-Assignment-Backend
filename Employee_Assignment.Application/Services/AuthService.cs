using BCrypt.Net;
using Employee_Assignment.Application.DTOs.Auth;
using Employee_Assignment.Application.Exceptions;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Employee_Assignment.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("Service: Login attempt for {Email}", loginDto.Email);

            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Service: User not found {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Service: Invalid password for {Email}", loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            await _userRepository.UpdateLastLoginAsync(user.Id);

            var roles = await _roleRepository.GetUserRolesAsync(user.Id);
            var roleNames = roles.Select(r => r.Name).ToList();

            var token = GenerateJwtToken(user, roleNames);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            _logger.LogInformation("Service: Login successful for {Email}", loginDto.Email);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Roles = roleNames,
                ExpiresAt = expiresAt
            };
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Service: Registration attempt for {Email}", registerDto.Email);

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                _logger.LogWarning("Service: Email already exists {Email}", registerDto.Email);
                throw new DuplicateException("User", "Email", registerDto.Email);
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = passwordHash
            };

            var createdUser = await _userRepository.CreateAsync(user);

            // Assign role to user
            var role = await _roleRepository.GetByNameAsync(registerDto.Role);
            if (role == null)
            {
                throw new NotFoundException("Role", registerDto.Role);
            }

            await _roleRepository.AssignRoleToUserAsync(createdUser.Id, role.RoleId);

            var token = GenerateJwtToken(createdUser, new List<string> { registerDto.Role });
            var expiresAt = DateTime.UtcNow.AddHours(24);

            _logger.LogInformation("Service: Registration successful for {Email}", registerDto.Email);

            return new AuthResponseDto
            {
                Token = token,
                Email = createdUser.Email,
                Name = createdUser.Name,
                Roles = new List<string> { registerDto.Role },
                ExpiresAt = expiresAt
            };
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userRepository.EmailExistsAsync(email);
        }

        public string GenerateJwtToken(User user, List<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryInHours = int.Parse(jwtSettings["ExpiryInHours"] ?? "24");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add multiple role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryInHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
