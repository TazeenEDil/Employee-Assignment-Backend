// File: Employee_Assignment.UnitTests/Controllers/EmployeesControllerTests.cs
using Employee_Assignment.Controllers;
using Employee_Assignment.DTOs;
using Employee_Assignment.Interfaces;
using Employee_Assignment.Models;
using Employee_Assignment.UnitTests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Employee_Assignment.UnitTests.Controllers
{
    public class EmployeesControllerTests
    {
        private readonly Mock<IEmployeeService> _serviceMock;
        private readonly Mock<ILogger<EmployeesController>> _loggerMock;
        private readonly EmployeesController _controller;

        public EmployeesControllerTests()
        {
            _serviceMock = new Mock<IEmployeeService>();
            _loggerMock = MockLogger.Create<EmployeesController>();
            _controller = new EmployeesController(_serviceMock.Object, _loggerMock.Object);
        }

        #region GetEmployees Tests

        [Fact]
        public async Task GetEmployees_ReturnsOkWithEmployeeList()
        {
            // Arrange
            var employees = TestDataBuilder.CreateEmployeeList(3);
            _serviceMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(employees);

            // Act
            var result = await _controller.GetEmployees();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var employeeDtos = okResult!.Value as IEnumerable<EmployeeDto>;

            employeeDtos.Should().NotBeNull();
            employeeDtos.Should().HaveCount(3);

            _serviceMock.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetEmployees_WhenNoEmployees_ReturnsEmptyList()
        {
            // Arrange
            _serviceMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Employee>());

            // Act
            var result = await _controller.GetEmployees();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var employeeDtos = okResult!.Value as IEnumerable<EmployeeDto>;

            employeeDtos.Should().NotBeNull();
            employeeDtos.Should().BeEmpty();
        }

        #endregion

        #region GetEmployee Tests

        [Fact]
        public async Task GetEmployee_WithValidId_ReturnsOkWithEmployee()
        {
            // Arrange
            var employee = TestDataBuilder.CreateEmployee();
            _serviceMock
                .Setup(x => x.GetByIdAsync(employee.Id))
                .ReturnsAsync(employee);

            // Act
            var result = await _controller.GetEmployee(employee.Id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var employeeDto = okResult!.Value as EmployeeDto;

            employeeDto.Should().NotBeNull();
            employeeDto!.Id.Should().Be(employee.Id);
            employeeDto.Email.Should().Be(employee.Email);
        }

        [Fact]
        public async Task GetEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _serviceMock
                .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Employee?)null);

            // Act
            var result = await _controller.GetEmployee(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Employee not found" });
        }

        #endregion

        #region CreateEmployee Tests

        [Fact]
        public async Task CreateEmployee_WithValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = TestDataBuilder.CreateEmployeeDto();
            var createdEmployee = TestDataBuilder.CreateEmployee(
                id: 1,
                name: createDto.Name,
                email: createDto.Email,
                position: createDto.Position
            );

            _serviceMock
                .Setup(x => x.EmailExistsAsync(createDto.Email, null))
                .ReturnsAsync(false);

            _serviceMock
                .Setup(x => x.CreateAsync(It.IsAny<Employee>()))
                .ReturnsAsync(createdEmployee);

            // Act
            var result = await _controller.CreateEmployee(createDto);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;

            createdResult!.ActionName.Should().Be(nameof(EmployeesController.GetEmployee));
            createdResult.RouteValues!["id"].Should().Be(createdEmployee.Id);

            _serviceMock.Verify(x => x.CreateAsync(It.Is<Employee>(e =>
                e.Name == createDto.Name &&
                e.Email == createDto.Email &&
                e.Position == createDto.Position
            )), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var createDto = TestDataBuilder.CreateEmployeeDto();
            _serviceMock
                .Setup(x => x.EmailExistsAsync(createDto.Email, null))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateEmployee(createDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Email already exists" });

            _serviceMock.Verify(x => x.CreateAsync(It.IsAny<Employee>()), Times.Never);
        }

        #endregion

        #region UpdateEmployee Tests

        [Fact]
        public async Task UpdateEmployee_WithValidData_ReturnsOkWithUpdatedEmployee()
        {
            // Arrange
            var updateDto = TestDataBuilder.UpdateEmployeeDto();
            var updatedEmployee = TestDataBuilder.CreateEmployee(
                id: 1,
                name: updateDto.Name,
                email: updateDto.Email,
                position: updateDto.Position
            );

            _serviceMock
                .Setup(x => x.UpdateAsync(1, It.IsAny<Employee>()))
                .ReturnsAsync(updatedEmployee);

            // Act
            var result = await _controller.UpdateEmployee(1, updateDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var employee = okResult!.Value as Employee;

            employee.Should().NotBeNull();
            employee!.Name.Should().Be(updateDto.Name);
            employee.Email.Should().Be(updateDto.Email);
        }

        [Fact]
        public async Task UpdateEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var updateDto = TestDataBuilder.UpdateEmployeeDto();
            _serviceMock
                .Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<Employee>()))
                .ReturnsAsync((Employee?)null);

            // Act
            var result = await _controller.UpdateEmployee(999, updateDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Employee not found" });
        }

        #endregion

        #region DeleteEmployee Tests

        [Fact]
        public async Task DeleteEmployee_WithValidId_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            _serviceMock
                .Setup(x => x.DeleteAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteEmployee(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { message = "Employee deleted successfully" });
        }

        [Fact]
        public async Task DeleteEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _serviceMock
                .Setup(x => x.DeleteAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteEmployee(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Employee not found" });
        }

        #endregion
    }
}