using Employee_Assignment.Controllers;
using Employee_Assignment.DTOs.Auth;
using Employee_Assignment.Interfaces;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Employee_Assignment.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = MockLogger.Create<AuthController>();
            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto();
            var expectedResponse = new AuthResponseDto
            {
                Token = "valid-jwt-token",
                Email = loginDto.Email,
                Name = "Admin User",
                Role = "Admin",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _authServiceMock
                .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResponse);

            _authServiceMock.Verify(x => x.LoginAsync(loginDto), Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto();
            _authServiceMock
                .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync((AuthResponseDto?)null);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().BeEquivalentTo(new { message = "Invalid email or password" });
        }

        [Fact]
        public async Task Login_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ReturnsOkWithToken()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            var expectedResponse = new AuthResponseDto
            {
                Token = "valid-jwt-token",
                Email = registerDto.Email,
                Name = registerDto.Name,
                Role = registerDto.Role,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _authServiceMock
                .Setup(x => x.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(false);

            _authServiceMock
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            _authServiceMock
                .Setup(x => x.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Email already exists" });

            _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterDto>()), Times.Never);
        }

        [Fact]
        public async Task Register_WhenRegistrationFails_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            _authServiceMock
                .Setup(x => x.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(false);

            _authServiceMock
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync((AuthResponseDto?)null);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Registration failed" });
        }

        [Fact]
        public async Task Register_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region ValidateToken Tests

        [Fact]
        public void ValidateToken_WithAuthenticatedUser_ReturnsOkWithUserInfo()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "1"),
                new(ClaimTypes.Email, "admin@example.com"),
                new(ClaimTypes.Name, "Admin User"),
                new(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = _controller.ValidateToken();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;

            var value = okResult!.Value;
            value.Should().NotBeNull();
        }

        #endregion
    }
}