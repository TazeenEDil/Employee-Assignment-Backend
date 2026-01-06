// File: Employee_Assignment.IntegrationTests/AuthIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using Employee_Assignment.DTOs.Auth;
using Employee_Assignment.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace Employee_Assignment.IntegrationTests
{
    public class AuthIntegrationTests : IntegrationTestBase
    {
        public AuthIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        #region Login Integration Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "admin@test.com",
                Password = "Admin123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrEmpty();
            authResponse.Email.Should().Be(loginDto.Email);
            authResponse.Name.Should().Be("Admin User");
            authResponse.Role.Should().Be("Admin");
            authResponse.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "admin@test.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "SomePassword123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "",
                Password = "Password123!"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Register Integration Tests

        [Fact]
        public async Task Register_WithValidData_ReturnsOkWithToken()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail("newuser");
            var registerDto = new RegisterDto
            {
                Name = "New Test User",
                Email = uniqueEmail,
                Password = "NewUser123!",
                ConfirmPassword = "NewUser123!", // IMPORTANT: Must match Password
                Role = "Employee"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrEmpty();
            authResponse.Email.Should().Be(registerDto.Email);
            authResponse.Name.Should().Be(registerDto.Name);
            authResponse.Role.Should().Be(registerDto.Role);
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Duplicate User",
                Email = "admin@test.com", // Already exists in seed data
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = "Employee"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = "invalid-email",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                Role = "Employee"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail("mismatch");
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = uniqueEmail,
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword123!", // Doesn't match
                Role = "Employee"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_AsAdmin_CanRegisterNewAdmin()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail("newadmin");
            var registerDto = new RegisterDto
            {
                Name = "New Admin",
                Email = uniqueEmail,
                Password = "Admin123!",
                ConfirmPassword = "Admin123!",
                Role = "Admin"
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            authResponse!.Role.Should().Be("Admin");
        }

        #endregion

        #region ValidateToken Integration Tests

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsOkWithUserInfo()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("admin@test.com");
            content.Should().Contain("Admin");
        }

        [Fact]
        public async Task ValidateToken_WithoutToken_ReturnsUnauthorized()
        {
            // Act
            var response = await Client.GetAsync("/api/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            SetAuthToken("invalid-token-string");

            // Act
            var response = await Client.GetAsync("/api/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Authentication Flow Tests

        [Fact]
        public async Task AuthFlow_RegisterLoginAndValidate_WorksEndToEnd()
        {
            // Step 1: Register
            var uniqueEmail = GetUniqueEmail("flowtest");
            var registerDto = new RegisterDto
            {
                Name = "Flow Test User",
                Email = uniqueEmail,
                Password = "FlowTest123!",
                ConfirmPassword = "FlowTest123!",
                Role = "Employee"
            };

            var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            registerResult.Should().NotBeNull();

            // Step 2: Login with new credentials
            var loginDto = new LoginDto
            {
                Email = registerDto.Email,
                Password = registerDto.Password
            };

            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            loginResult.Should().NotBeNull();
            loginResult!.Token.Should().NotBeNullOrEmpty();

            // Step 3: Validate token
            SetAuthToken(loginResult.Token);
            var validateResponse = await Client.GetAsync("/api/auth/validate");
            validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task AuthFlow_MultipleLoginAttempts_UpdatesLastLogin()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "admin@test.com",
                Password = "Admin123!"
            };

            // Act - First login
            var response1 = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            response1.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(1000); // Wait 1 second

            // Act - Second login
            var response2 = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert - Both should succeed
            var result1 = await response1.Content.ReadFromJsonAsync<AuthResponseDto>();
            var result2 = await response2.Content.ReadFromJsonAsync<AuthResponseDto>();

            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1!.Token.Should().NotBe(result2!.Token); // Different tokens due to time difference
        }

        #endregion
    }
}