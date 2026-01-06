using Employee_Assignment.DTOs.Auth;
using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Employee_Assignment.Services;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Employee_Assignment.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = MockLogger.Create<AuthService>();

            SetupConfiguration();
            _authService = new AuthService(
                _userRepositoryMock.Object,
                _configurationMock.Object,
                _loggerMock.Object
            );
        }

        private void SetupConfiguration()
        {
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["SecretKey"]).Returns("ThisIsAVerySecureSecretKeyForTesting12345");
            jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            jwtSection.Setup(x => x["ExpiryInHours"]).Returns("24");

            _configurationMock
                .Setup(x => x.GetSection("JwtSettings"))
                .Returns(jwtSection.Object);
        }

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto();
            var user = TestDataBuilder.CreateUser(password: loginDto.Password);

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(x => x.UpdateLastLoginAsync(user.Id))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(user.Email);
            result.Name.Should().Be(user.Name);
            result.Role.Should().Be(user.Role);
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(user.Id), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ReturnsNull()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto();
            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
        {
            // Arrange
            var loginDto = TestDataBuilder.CreateLoginDto(password: "WrongPassword");
            var user = TestDataBuilder.CreateUser(password: "CorrectPassword");

            _userRepositoryMock
                .Setup(x => x.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().BeNull();
            _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsAuthResponse()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            var createdUser = TestDataBuilder.CreateUser(
                name: registerDto.Name,
                email: registerDto.Email,
                role: registerDto.Role
            );

            _userRepositoryMock
                .Setup(x => x.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(false);

            _userRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(registerDto.Email);
            result.Name.Should().Be(registerDto.Name);
            result.Role.Should().Be(registerDto.Role);

            _userRepositoryMock.Verify(x => x.CreateAsync(It.Is<User>(u =>
                u.Email == registerDto.Email &&
                u.Name == registerDto.Name &&
                u.Role == registerDto.Role &&
                !string.IsNullOrEmpty(u.PasswordHash)
            )), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ReturnsNull()
        {
            // Arrange
            var registerDto = TestDataBuilder.CreateRegisterDto();
            _userRepositoryMock
                .Setup(x => x.EmailExistsAsync(registerDto.Email))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().BeNull();
            _userRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        #endregion

        #region EmailExistsAsync Tests

        [Fact]
        public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
        {
            // Arrange
            var email = "existing@example.com";
            _userRepositoryMock
                .Setup(x => x.EmailExistsAsync(email))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.EmailExistsAsync(email);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_WhenEmailDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _userRepositoryMock
                .Setup(x => x.EmailExistsAsync(email))
                .ReturnsAsync(false);

            // Act
            var result = await _authService.EmailExistsAsync(email);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GenerateJwtToken Tests

        [Fact]
        public void GenerateJwtToken_WithValidUser_ReturnsToken()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser();

            // Act
            var token = _authService.GenerateJwtToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3); // JWT has 3 parts
        }

        #endregion
    }
}