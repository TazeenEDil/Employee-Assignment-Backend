using Employee_Assignment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Employee_Assignment.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Position> Positions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ⭐ FIX: Use fixed DateTime instead of DateTime.UtcNow
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, Name = "Admin", CreatedAt = seedDate },
                new Role { RoleId = 2, Name = "Employee", CreatedAt = seedDate }
            );

            // Seed default positions
            modelBuilder.Entity<Position>().HasData(
                new Position { PositionId = 1, Name = "Software Engineer", Description = "Develops software applications", CreatedAt = seedDate },
                new Position { PositionId = 2, Name = "Senior Software Engineer", Description = "Senior level developer", CreatedAt = seedDate },
                new Position { PositionId = 3, Name = "Team Lead", Description = "Leads development teams", CreatedAt = seedDate },
                new Position { PositionId = 4, Name = "Project Manager", Description = "Manages projects", CreatedAt = seedDate },
                new Position { PositionId = 5, Name = "QA Engineer", Description = "Quality assurance specialist", CreatedAt = seedDate },
                new Position { PositionId = 6, Name = "DevOps Engineer", Description = "Infrastructure and deployment", CreatedAt = seedDate },
                new Position { PositionId = 7, Name = "Business Analyst", Description = "Analyzes business requirements", CreatedAt = seedDate },
                new Position { PositionId = 8, Name = "HR Manager", Description = "Human resources management", CreatedAt = seedDate }
            );
        }
    }
}
