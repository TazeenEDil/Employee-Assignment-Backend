using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Employee_Assignment.Services;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Employee_Assignment.UnitTests.Services
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _repositoryMock;
        private readonly Mock<ILogger<EmployeeService>> _loggerMock;
        private readonly EmployeeService _employeeService;

        public EmployeeServiceTests()
        {
            _repositoryMock = new Mock<IEmployeeRepository>();
            _loggerMock = MockLogger.Create<EmployeeService>();
            _employeeService = new EmployeeService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEmployees()
        {
            // Arrange
            var employees = TestDataBuilder.CreateEmployeeList(5);
            _repositoryMock
                .Setup(x => x.GetAllEmployeesAsync())
                .ReturnsAsync(employees);

            // Act
            var result = await _employeeService.GetAllAsync();

            // Assert
            result.Should().HaveCount(5);
            result.Should().BeEquivalentTo(employees);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            _repositoryMock
                .Setup(x => x.GetEmployeeByIdAsync(employee.Id))
                .ReturnsAsync(employee);

            // Act
            var result = await _employeeService.GetByIdAsync(employee.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(employee);
        }

        [Fact]
        public async Task CreateAsync_CreatesAndReturnsEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            _repositoryMock
                .Setup(x => x.CreateEmployeeAsync(employee))
                .ReturnsAsync(employee);

            // Act
            var result = await _employeeService.CreateAsync(employee);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(employee);
            _repositoryMock.Verify(x => x.CreateEmployeeAsync(employee), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAndReturnsEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            _repositoryMock
                .Setup(x => x.UpdateEmployeeAsync(employee.Id, employee))
                .ReturnsAsync(employee);

            // Act
            var result = await _employeeService.UpdateAsync(employee.Id, employee);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(employee);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            _repositoryMock
                .Setup(x => x.DeleteEmployeeAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _employeeService.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_ChecksEmailInRepository()
        {
            // Arrange
            var email = "test@example.com";
            _repositoryMock
                .Setup(x => x.EmailExistsAsync(email, null))
                .ReturnsAsync(true);

            // Act
            var result = await _employeeService.EmailExistsAsync(email);

            // Assert
            result.Should().BeTrue();
        }
    }
}