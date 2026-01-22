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
        public DbSet<FileStorage> FileStorages { get; set; }
        public DbSet<EmployeeFile> EmployeeFiles { get; set; }

        // Attendance module tables
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<AttendanceAlert> AttendanceAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

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

            // Seed default leave types
            modelBuilder.Entity<LeaveType>().HasData(
                new LeaveType { LeaveTypeId = 1, Name = "Sick Leave", Description = "For medical issues", MaxDaysPerYear = 10, CreatedAt = seedDate },
                new LeaveType { LeaveTypeId = 2, Name = "Casual Leave", Description = "For personal matters", MaxDaysPerYear = 15, CreatedAt = seedDate },
                new LeaveType { LeaveTypeId = 3, Name = "Annual Leave", Description = "Yearly vacation", MaxDaysPerYear = 20, CreatedAt = seedDate },
                new LeaveType { LeaveTypeId = 4, Name = "Emergency Leave", Description = "For emergencies", MaxDaysPerYear = 5, CreatedAt = seedDate },
                new LeaveType { LeaveTypeId = 5, Name = "Maternity Leave", Description = "For new mothers", MaxDaysPerYear = 90, CreatedAt = seedDate },
                new LeaveType { LeaveTypeId = 6, Name = "Paternity Leave", Description = "For new fathers", MaxDaysPerYear = 15, CreatedAt = seedDate }
            );

            // Configure EmployeeFile relationships
            modelBuilder.Entity<EmployeeFile>()
                .HasOne(ef => ef.Employee)
                .WithMany()
                .HasForeignKey(ef => ef.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeFile>()
                .HasOne(ef => ef.FileStorage)
                .WithMany(fs => fs.EmployeeFiles)
                .HasForeignKey(ef => ef.FileStorageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Attendance relationships
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeId, a.Date })
                .IsUnique();

            // Configure LeaveRequest relationships
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Employee)
                .WithMany()
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.LeaveType)
                .WithMany(lt => lt.LeaveRequests)
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.ApprovedByUser)
                .WithMany()
                .HasForeignKey(lr => lr.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure AttendanceAlert relationships
            modelBuilder.Entity<AttendanceAlert>()
                .HasOne(aa => aa.Employee)
                .WithMany()
                .HasForeignKey(aa => aa.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AttendanceAlert>()
                .HasOne(aa => aa.CreatedByUser)
                .WithMany()
                .HasForeignKey(aa => aa.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .Property(lr => lr.EmailActionToken)
                .IsRequired()
                .HasMaxLength(100);
        }

    }
    }
