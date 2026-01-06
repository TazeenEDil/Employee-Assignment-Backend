using Employee_Assignment.Data;
using Employee_Assignment.Models;
using Employee_Assignment.Repositories;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace Employee_Assignment.UnitTests.Repositories
{
    public class EmployeeRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly EmployeeRepository _repository;

        public EmployeeRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new EmployeeRepository(_context, MockLogger.CreateLogger<EmployeeRepository>());
        }

        [Fact]
        public async Task GetAllEmployeesAsync_ReturnsOrderedEmployees()
        {
            // Arrange
            await _context.Employees.AddRangeAsync(
                TestDataBuilder.CreateEmployee(1, "Charlie", "charlie@test.com"),
                TestDataBuilder.CreateEmployee(2, "Alice", "alice@test.com"),
                TestDataBuilder.CreateEmployee(3, "Bob", "bob@test.com")
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllEmployeesAsync();

            // Assert
            result.Should().HaveCount(3);
            result.First().Name.Should().Be("Alice"); // Ordered by name
            result.Last().Name.Should().Be("Charlie");
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_WithValidId_ReturnsEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetEmployeeByIdAsync(employee.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(employee.Id);
            result.Email.Should().Be(employee.Email);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _repository.GetEmployeeByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateEmployeeAsync_CreatesEmployee()
        {
            // Arrange
            var employee = new Employee
            {
                Name = "New Employee",
                Email = "new@test.com",
                Position = "Developer"
            };

            // Act
            var result = await _repository.CreateEmployeeAsync(employee);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

            var savedEmployee = await _context.Employees.FindAsync(result.Id);
            savedEmployee.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateEmployeeAsync_WithValidId_UpdatesEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            var updatedEmployee = new Employee
            {
                Name = "Updated Name",
                Email = "updated@test.com",
                Position = "Senior Developer"
            };

            // Act
            var result = await _repository.UpdateEmployeeAsync(employee.Id, updatedEmployee);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Updated Name");
            result.Email.Should().Be("updated@test.com");
            result.Position.Should().Be("Senior Developer");
        }

        [Fact]
        public async Task UpdateEmployeeAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var updatedEmployee = TestDataBuilder.CreateEmployee();

            // Act
            var result = await _repository.UpdateEmployeeAsync(999, updatedEmployee);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteEmployeeAsync_WithValidId_DeletesAndReturnsTrue()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteEmployeeAsync(employee.Id);

            // Assert
            result.Should().BeTrue();
            var deletedEmployee = await _context.Employees.FindAsync(employee.Id);
            deletedEmployee.Should().BeNull();
        }

        [Fact]
        public async Task DeleteEmployeeAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _repository.DeleteEmployeeAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee(email: "duplicate@test.com");
            await _context.Employees.AddAsync(employee);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.EmailExistsAsync("duplicate@test.com");

            // Assert
            result.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
