using Employee_Assignment.DTOs;
using Employee_Assignment.DTOs.Auth;
using Employee_Assignment.Models;

namespace Employee_Assignment.UnitTests.Helpers
{
    public static class TestDataBuilder
    {
        public static Employee CreateEmployee(
            int id = 1,
            string name = "John Doe",
            string email = "john@example.com",
            string position = "Developer")
        {
            return new Employee
            {
                Id = id,
                Name = name,
                Email = email,
                Position = position,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static User CreateUser(
            int id = 1,
            string name = "Admin User",
            string email = "admin@example.com",
            string role = "Admin",
            string password = "Password123!")
        {
            return new User
            {
                Id = id,
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
        }

        public static LoginDto CreateLoginDto(
            string email = "admin@example.com",
            string password = "Password123!")
        {
            return new LoginDto
            {
                Email = email,
                Password = password
            };
        }

        public static RegisterDto CreateRegisterDto(
            string name = "New User",
            string email = "newuser@example.com",
            string password = "Password123!",
            string role = "Employee")
        {
            return new RegisterDto
            {
                Name = name,
                Email = email,
                Password = password,
                ConfirmPassword = password, // ADDED - This was missing!
                Role = role
            };
        }

        public static CreateEmployeeDto CreateEmployeeDto(
            string name = "Jane Smith",
            string email = "jane@example.com",
            string position = "Manager")
        {
            return new CreateEmployeeDto
            {
                Name = name,
                Email = email,
                Position = position
            };
        }

        public static UpdateEmployeeDto UpdateEmployeeDto(
            string name = "Updated Name",
            string email = "updated@example.com",
            string position = "Senior Developer")
        {
            return new UpdateEmployeeDto
            {
                Name = name,
                Email = email,
                Position = position
            };
        }

        public static List<Employee> CreateEmployeeList(int count = 3)
        {
            var employees = new List<Employee>();
            for (int i = 1; i <= count; i++)
            {
                employees.Add(CreateEmployee(
                    id: i,
                    name: $"Employee {i}",
                    email: $"employee{i}@example.com",
                    position: $"Position {i}"
                ));
            }
            return employees;
        }
    }
}