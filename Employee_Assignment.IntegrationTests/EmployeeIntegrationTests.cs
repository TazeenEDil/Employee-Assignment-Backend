// File: Employee_Assignment.IntegrationTests/EmployeeIntegrationTests.cs
using System.Net;
using System.Net.Http.Json;
using Employee_Assignment.DTOs;
using Employee_Assignment.IntegrationTests.Setup;
using Employee_Assignment.Models;
using FluentAssertions;
using Xunit;

namespace Employee_Assignment.IntegrationTests
{
    public class EmployeeIntegrationTests : IntegrationTestBase
    {
        public EmployeeIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        #region GetEmployees Integration Tests

        [Fact]
        public async Task GetEmployees_AsAdmin_ReturnsOkWithEmployeeList()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/employees");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
            employees.Should().NotBeNull();
            employees.Should().HaveCountGreaterOrEqualTo(2); // From seed data
        }

        [Fact]
        public async Task GetEmployees_AsEmployee_ReturnsOkWithEmployeeList()
        {
            // Arrange
            var token = await GetAuthTokenAsync("employee@test.com", "Employee123!");
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/employees");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>();
            employees.Should().NotBeNull();
        }

        [Fact]
        public async Task GetEmployees_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await Client.GetAsync("/api/employees");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region GetEmployee Integration Tests

        [Fact]
        public async Task GetEmployee_AsAdmin_WithValidId_ReturnsEmployee()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/employees/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var employee = await response.Content.ReadFromJsonAsync<EmployeeDto>();
            employee.Should().NotBeNull();
            employee!.Id.Should().Be(1);
            employee.Name.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/employees/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetEmployee_AsEmployee_WithValidId_ReturnsEmployee()
        {
            // Arrange
            var token = await GetAuthTokenAsync("employee@test.com", "Employee123!");
            SetAuthToken(token);

            // Act
            var response = await Client.GetAsync("/api/employees/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region CreateEmployee Integration Tests

        [Fact]
        public async Task CreateEmployee_AsAdmin_WithValidData_ReturnsCreated()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            var uniqueEmail = GetUniqueEmail("newemp");
            var createDto = new CreateEmployeeDto
            {
                Name = "New Employee",
                Email = uniqueEmail,
                Position = "Developer"
            };

            // Act
            var response = await PostAsJsonAsync("/api/employees", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var employee = await response.Content.ReadFromJsonAsync<Employee>();
            employee.Should().NotBeNull();
            employee!.Name.Should().Be(createDto.Name);
            employee.Email.Should().Be(createDto.Email);
            employee.Position.Should().Be(createDto.Position);

            // Verify location header
            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateEmployee_AsAdmin_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            var createDto = new CreateEmployeeDto
            {
                Name = "Duplicate",
                Email = "john@test.com", // Already exists
                Position = "Developer"
            };

            // Act
            var response = await PostAsJsonAsync("/api/employees", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateEmployee_AsEmployee_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync("employee@test.com", "Employee123!");
            SetAuthToken(token);

            var uniqueEmail = GetUniqueEmail("test");
            var createDto = new CreateEmployeeDto
            {
                Name = "Test",
                Email = uniqueEmail,
                Position = "Developer"
            };

            // Act
            var response = await PostAsJsonAsync("/api/employees", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateEmployee_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail("test");
            var createDto = new CreateEmployeeDto
            {
                Name = "Test",
                Email = uniqueEmail,
                Position = "Developer"
            };

            // Act
            var response = await PostAsJsonAsync("/api/employees", createDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region UpdateEmployee Integration Tests

        [Fact]
        public async Task UpdateEmployee_AsAdmin_WithValidData_ReturnsOk()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            var uniqueEmail = GetUniqueEmail("updated");
            var updateDto = new UpdateEmployeeDto
            {
                Name = "Updated Name",
                Email = uniqueEmail,
                Position = "Senior Developer"
            };

            // Act
            var response = await PutAsJsonAsync("/api/employees/1", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var employee = await response.Content.ReadFromJsonAsync<Employee>();
            employee.Should().NotBeNull();
            employee!.Name.Should().Be(updateDto.Name);
            employee.Email.Should().Be(updateDto.Email);
            employee.Position.Should().Be(updateDto.Position);
        }

        [Fact]
        public async Task UpdateEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            var uniqueEmail = GetUniqueEmail("updated");
            var updateDto = new UpdateEmployeeDto
            {
                Name = "Updated",
                Email = uniqueEmail,
                Position = "Developer"
            };

            // Act
            var response = await PutAsJsonAsync("/api/employees/999", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateEmployee_AsEmployee_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync("employee@test.com", "Employee123!");
            SetAuthToken(token);

            var uniqueEmail = GetUniqueEmail("updated");
            var updateDto = new UpdateEmployeeDto
            {
                Name = "Updated",
                Email = uniqueEmail,
                Position = "Developer"
            };

            // Act
            var response = await PutAsJsonAsync("/api/employees/1", updateDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region DeleteEmployee Integration Tests

        [Fact]
        public async Task DeleteEmployee_AsAdmin_WithValidId_ReturnsOk()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Act
            var response = await Client.DeleteAsync("/api/employees/2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify it's deleted
            var getResponse = await Client.GetAsync("/api/employees/2");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteEmployee_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Act
            var response = await Client.DeleteAsync("/api/employees/999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteEmployee_AsEmployee_ReturnsForbidden()
        {
            // Arrange
            var token = await GetAuthTokenAsync("employee@test.com", "Employee123!");
            SetAuthToken(token);

            // Act
            var response = await Client.DeleteAsync("/api/employees/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region CRUD Flow Tests

        [Fact]
        public async Task EmployeeCRUD_CompleteFlow_WorksEndToEnd()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Step 1: Create
            var uniqueEmail = GetUniqueEmail("crudtest");
            var createDto = new CreateEmployeeDto
            {
                Name = "CRUD Test Employee",
                Email = uniqueEmail,
                Position = "Tester"
            };

            var createResponse = await PostAsJsonAsync("/api/employees", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createdEmployee = await createResponse.Content.ReadFromJsonAsync<Employee>();
            var employeeId = createdEmployee!.Id;

            // Step 2: Read
            var getResponse = await Client.GetAsync($"/api/employees/{employeeId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var retrievedEmployee = await getResponse.Content.ReadFromJsonAsync<EmployeeDto>();
            retrievedEmployee!.Name.Should().Be(createDto.Name);

            // Step 3: Update
            var updateUniqueEmail = GetUniqueEmail("updatedcrud");
            var updateDto = new UpdateEmployeeDto
            {
                Name = "Updated CRUD Test",
                Email = updateUniqueEmail,
                Position = "Senior Tester"
            };

            var updateResponse = await PutAsJsonAsync($"/api/employees/{employeeId}", updateDto);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var updatedEmployee = await updateResponse.Content.ReadFromJsonAsync<Employee>();
            updatedEmployee!.Name.Should().Be(updateDto.Name);

            // Step 4: Delete
            var deleteResponse = await Client.DeleteAsync($"/api/employees/{employeeId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Step 5: Verify deletion
            var verifyResponse = await Client.GetAsync($"/api/employees/{employeeId}");
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task EmployeeList_AfterMultipleOperations_ReflectsChanges()
        {
            // Arrange
            var token = await GetAuthTokenAsync("admin@test.com", "Admin123!");
            SetAuthToken(token);

            // Get initial count
            var initialResponse = await Client.GetAsync("/api/employees");
            var initialEmployees = await initialResponse.Content.ReadFromJsonAsync<List<EmployeeDto>>();
            var initialCount = initialEmployees!.Count;

            // Create new employee
            var uniqueEmail = GetUniqueEmail("listtest");
            var createDto = new CreateEmployeeDto
            {
                Name = "List Test",
                Email = uniqueEmail,
                Position = "Developer"
            };

            await PostAsJsonAsync("/api/employees", createDto);

            // Get updated list
            var updatedResponse = await Client.GetAsync("/api/employees");
            var updatedEmployees = await updatedResponse.Content.ReadFromJsonAsync<List<EmployeeDto>>();

            // Assert
            updatedEmployees!.Count.Should().Be(initialCount + 1);
            updatedEmployees.Should().Contain(e => e.Email == uniqueEmail);
        }

        #endregion
    }
}