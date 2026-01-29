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
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IEmployeeRepository employeeRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _employeeRepository = employeeRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
        {
            _logger.LogInformation("=== LOGIN ATTEMPT ===");
            _logger.LogInformation("Email: " + loginDto.Email);

            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found: " + loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            _logger.LogInformation("User found - ID: " + user.Id + ", Name: " + user.Name);

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for: " + loginDto.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            _logger.LogInformation("Password verified successfully");

            await _userRepository.UpdateLastLoginAsync(user.Id);

            var roles = await _roleRepository.GetUserRolesAsync(user.Id);
            var roleNames = roles.Select(r => r.Name).ToList();

            _logger.LogInformation("User roles: " + string.Join(", ", roleNames));

            // ✅ FIXED: Only get employee ID for Employee role, NOT for Admin
            int? employeeId = null;
            if (roleNames.Contains("Employee"))
            {
                try
                {
                    var employees = await _employeeRepository.GetAllEmployeesAsync();
                    _logger.LogInformation($"Total employees in database: {employees.Count()}");

                    var employee = employees.FirstOrDefault(e =>
                        e.Email.Equals(loginDto.Email, StringComparison.OrdinalIgnoreCase));

                    if (employee != null)
                    {
                        employeeId = employee.Id;
                        _logger.LogInformation($"✅ Employee record found - ID: {employeeId.Value}, Name: {employee.Name}, Email: {employee.Email}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ No employee record found for employee user: {loginDto.Email}");
                        _logger.LogWarning("This employee user exists in Users table but not in Employees table");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error looking up employee record");
                }
            }
            else if (roleNames.Contains("Admin"))
            {
                _logger.LogInformation("✅ Admin user - no employee record needed");
            }

            var token = GenerateJwtToken(user, roleNames, employeeId);
            var expiresAt = DateTime.UtcNow.AddMinutes(1440); // 24 hours

            _logger.LogInformation("=== LOGIN SUCCESSFUL ===");
            _logger.LogInformation("Token generated (length: " + token.Length + ")");
            _logger.LogInformation("Token expires at: " + expiresAt);
            if (employeeId.HasValue)
            {
                _logger.LogInformation($"EmployeeId in token: {employeeId.Value}");
            }
            else
            {
                _logger.LogInformation("No EmployeeId in token (Admin user or employee without record)");
            }

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
                Role = roleNames.FirstOrDefault() ?? "Employee",
                ExpiresAt = expiresAt
            };
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("=== REGISTRATION ATTEMPT ===");
            _logger.LogInformation("Email: " + registerDto.Email + ", Role: " + registerDto.Role);

            // Validate PositionId for Employee role
            if (registerDto.Role == "Employee" && (!registerDto.PositionId.HasValue || registerDto.PositionId.Value <= 0))
            {
                _logger.LogWarning("Position is required for Employee registration");
                throw new ArgumentException("Position is required for Employee registration");
            }

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                _logger.LogWarning("Email already exists: " + registerDto.Email);
                throw new DuplicateException("User", "Email", registerDto.Email);
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var user = new User
            {
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.CreateAsync(user);
            _logger.LogInformation("User created - ID: " + createdUser.Id);

            // Assign role to user
            var role = await _roleRepository.GetByNameAsync(registerDto.Role);
            if (role == null)
            {
                throw new NotFoundException("Role", registerDto.Role);
            }

            await _roleRepository.AssignRoleToUserAsync(createdUser.Id, role.RoleId);
            _logger.LogInformation("Role assigned: " + registerDto.Role);

            // ✅ Create employee record ONLY if role is Employee AND PositionId is provided
            int? employeeId = null;
            if (registerDto.Role == "Employee" && registerDto.PositionId.HasValue)
            {
                var employee = new Employee
                {
                    Name = registerDto.Name,
                    Email = registerDto.Email,
                    PositionId = registerDto.PositionId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                var createdEmployee = await _employeeRepository.CreateEmployeeAsync(employee);
                employeeId = createdEmployee.Id;
                _logger.LogInformation("Employee record created - ID: " + employeeId.Value);
            }
            else if (registerDto.Role == "Admin")
            {
                _logger.LogInformation("Admin user registered - no employee record created");
            }

            var token = GenerateJwtToken(createdUser, new List<string> { registerDto.Role }, employeeId);
            var expiresAt = DateTime.UtcNow.AddMinutes(1440); // 24 hours

            _logger.LogInformation("=== REGISTRATION SUCCESSFUL ===");

            return new AuthResponseDto
            {
                Token = token,
                Email = createdUser.Email,
                Name = createdUser.Name,
                Role = registerDto.Role,
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

        // ✅ Generate JWT Token with proper claims
        public string GenerateJwtToken(User user, List<string> roles, int? employeeId = null)
        {
            _logger.LogInformation("=== GENERATING JWT TOKEN ===");

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            _logger.LogInformation("JWT Settings:");
            _logger.LogInformation("  Issuer: " + issuer);
            _logger.LogInformation("  Audience: " + audience);
            _logger.LogInformation("  Secret Key Length: " + (secretKey?.Length ?? 0));

            // Try to get ExpiryInMinutes first, fallback for backward compatibility
            var expiryInMinutes = 1440; // Default 24 hours
            if (jwtSettings["ExpiryInMinutes"] != null)
            {
                expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"]);
            }
            else if (jwtSettings["ExpiryInHours"] != null)
            {
                expiryInMinutes = int.Parse(jwtSettings["ExpiryInHours"]) * 60;
            }

            _logger.LogInformation("Token expiry: " + expiryInMinutes + " minutes");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                
                // ✅ CRITICAL: Add both ClaimTypes.NameIdentifier AND custom UserId claim
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("userid", user.Id.ToString()), // lowercase version
                
                new Claim(ClaimTypes.Email, user.Email)
            };

            _logger.LogInformation("Base claims added - User: " + user.Email + ", ID: " + user.Id);
            _logger.LogInformation("  Added UserId claims: " + user.Id);

            // ✅ Add EmployeeId claim if available (only for Employee role)
            if (employeeId.HasValue)
            {
                claims.Add(new Claim("EmployeeId", employeeId.Value.ToString()));
                claims.Add(new Claim("employeeid", employeeId.Value.ToString())); // lowercase version
                _logger.LogInformation("  ✅ Added EmployeeId claims: " + employeeId.Value);
            }
            else
            {
                _logger.LogInformation("  ⚠️ No EmployeeId to add to token (Admin or employee without record)");
            }

            // ✅ Add role claims (both standard and custom)
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role)); // lowercase version
                _logger.LogInformation("  Added role claims: " + role);
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("=== TOKEN GENERATED ===");
            _logger.LogInformation("Token length: " + tokenString.Length);
            _logger.LogInformation("Token preview: " + tokenString.Substring(0, Math.Min(50, tokenString.Length)) + "...");
            _logger.LogInformation("Expires at: " + expiresAt + " UTC");
            _logger.LogInformation("Total claims: " + claims.Count);

            // ✅ Log all claims for debugging
            _logger.LogInformation("All claims in token:");
            foreach (var claim in claims)
            {
                _logger.LogInformation($"  - {claim.Type} = {claim.Value}");
            }

            return tokenString;
        }
    }
}