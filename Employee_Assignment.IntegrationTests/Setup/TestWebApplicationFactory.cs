// File: Employee_Assignment.IntegrationTests/Setup/TestWebApplicationFactory.cs
using Employee_Assignment.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.IntegrationTests.Setup
{
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
        where TProgram : class
    {
        private string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext with InMemory database - use unique name per factory instance
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });

            builder.UseEnvironment("Testing");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Create the host for TestServer
            var testHost = builder.Build();

            // Configure and seed the database
            using (var scope = testHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var db = services.GetRequiredService<ApplicationDbContext>();
                var logger = services.GetRequiredService<ILogger<TestWebApplicationFactory<TProgram>>>();

                try
                {
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                    SeedTestData(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error seeding test database: {Message}", ex.Message);
                }
            }

            return testHost;
        }

        private static void SeedTestData(ApplicationDbContext context)
        {
            // Seed test users
            context.Users.AddRange(
                new Employee_Assignment.Models.User
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                },
                new Employee_Assignment.Models.User
                {
                    Id = 2,
                    Name = "Regular Employee",
                    Email = "employee@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee123!"),
                    Role = "Employee",
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed test employees
            context.Employees.AddRange(
                new Employee_Assignment.Models.Employee
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john@test.com",
                    Position = "Developer",
                    CreatedAt = DateTime.UtcNow
                },
                new Employee_Assignment.Models.Employee
                {
                    Id = 2,
                    Name = "Jane Smith",
                    Email = "jane@test.com",
                    Position = "Manager",
                    CreatedAt = DateTime.UtcNow
                }
            );

            context.SaveChanges();

            // Detach all entities to avoid tracking issues
            context.ChangeTracker.Clear();
        }

        public void ResetDatabase()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Clear change tracker first
            context.ChangeTracker.Clear();

            // Delete and recreate
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedTestData(context);
        }
    }
}