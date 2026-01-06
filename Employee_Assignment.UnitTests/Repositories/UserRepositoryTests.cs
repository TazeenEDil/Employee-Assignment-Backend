using Employee_Assignment.Data;
using Employee_Assignment.Models;
using Employee_Assignment.Repositories;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace Employee_Assignment.UnitTests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context, MockLogger.CreateLogger<UserRepository>());
        }

        [Fact]
        public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser();
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync(user.Email);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(user.Email);
            result.Name.Should().Be(user.Name);
        }

        [Fact]
        public async Task GetByEmailAsync_WithInvalidEmail_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByEmailAsync("nonexistent@test.com");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_CreatesUser()
        {
            // Arrange
            var user = new User
            {
                Name = "New User",
                Email = "newuser@test.com",
                PasswordHash = "hashedpassword",
                Role = "Employee"
            };

            // Act
            var result = await _repository.CreateAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

            var savedUser = await _context.Users.FindAsync(result.Id);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser(email: "existing@test.com");
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.EmailExistsAsync("existing@test.com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateLastLoginAsync_UpdatesLastLoginTimestamp()
        {
            // Arrange
            var user = TestDataBuilder.CreateUser();
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var previousLogin = user.LastLoginAt;

            // Act
            await _repository.UpdateLastLoginAsync(user.Id);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser!.LastLoginAt.Should().BeAfter(previousLogin!.Value);
            updatedUser.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}