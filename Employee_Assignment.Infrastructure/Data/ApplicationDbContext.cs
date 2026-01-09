using Employee_Assignment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, Name = "Admin", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 2, Name = "Employee", CreatedAt = DateTime.UtcNow }
            );

            // Seed default positions
            modelBuilder.Entity<Position>().HasData(
                new Position { PositionId = 1, Name = "Software Engineer", Description = "Develops software applications", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 2, Name = "Senior Software Engineer", Description = "Senior level developer", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 3, Name = "Team Lead", Description = "Leads development teams", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 4, Name = "Project Manager", Description = "Manages projects", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 5, Name = "QA Engineer", Description = "Quality assurance specialist", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 6, Name = "DevOps Engineer", Description = "Infrastructure and deployment", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 7, Name = "Business Analyst", Description = "Analyzes business requirements", CreatedAt = DateTime.UtcNow },
                new Position { PositionId = 8, Name = "HR Manager", Description = "Human resources management", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}
