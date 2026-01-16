using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Application.DTOs.Auth;
using Employee_Assignment.Application.DTOs.Employee;
using Employee_Assignment.Application.DTOs.Position;    
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Employee_Assignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("API: Login attempt for {Email}", loginDto.Email);

            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
            {
                _logger.LogWarning("API: Login failed for {Email}", loginDto.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            _logger.LogInformation("API: Login successful for {Email}", loginDto.Email);
            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("API: Registration attempt for {Email}", registerDto.Email);

            if (await _authService.EmailExistsAsync(registerDto.Email))
            {
                _logger.LogWarning("API: Email already exists {Email}", registerDto.Email);
                return BadRequest(new { message = "Email already exists" });
            }

            var result = await _authService.RegisterAsync(registerDto);

            if (result == null)
            {
                _logger.LogError("API: Registration failed for {Email}", registerDto.Email);
                return BadRequest(new { message = "Registration failed" });
            }

            _logger.LogInformation("API: Registration successful for {Email}", registerDto.Email);
            return Ok(result);
        }

        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                userId,
                email,
                name,
                role,
                isValid = true
            });
        }
    }
}