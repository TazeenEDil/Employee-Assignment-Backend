using Employee_Assignment.API.Middleware;
using Employee_Assignment.API.HealthChecks;
using Employee_Assignment.Application.Interfaces.Services;
using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Application.Services;
using Employee_Assignment.Infrastructure.Resilience;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Employee_Assignment.Infrastructure.Data;
using Employee_Assignment.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Employee Assignment API",
        Version = "v1",
        Description = "Clean Architecture Employee Management System with Resilience Patterns"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database with Resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.MigrationsAssembly("Employee_Assignment.Infrastructure");
            // Enable connection resiliency
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
            // Set command timeout
            sqlOptions.CommandTimeout(30);
        }
    );

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register Circuit Breaker Service (SINGLETON - shared across all requests)
builder.Services.AddSingleton<ICircuitBreakerService, CircuitBreakerService>();

// Register Repositories (Infrastructure Layer) - Now with Resilience
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IFileStorageRepository, FileStorageRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();

// Attendance Module Repositories
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
builder.Services.AddScoped<IAttendanceAlertRepository, AttendanceAlertRepository>();

// Register Services (Application Layer)
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPositionService, PositionService>();

// Attendance Module Services
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IAttendanceAlertService, AttendanceAlertService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure logging levels
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" }
    )
    .AddCheck<CircuitBreakerHealthCheck>(
        name: "circuit-breaker",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "resilience", "circuit-breaker" }
    );

var app = builder.Build();

// Configure the HTTP request pipeline

// IMPORTANT: Register Global Exception Middleware FIRST
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Assignment API V1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

// Map Health Checks endpoint with detailed response
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Auto-migrate database on startup (optional, remove in production)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Starting database migration...");
        context.Database.Migrate();
        logger.LogInformation("Database migration completed successfully");

        // Log database connection info
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
        var serverName = connectionString?.Split(';')
            .FirstOrDefault(s => s.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=')[1];

        logger.LogInformation("Connected to database server: {ServerName}", serverName ?? "Unknown");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database. Application will continue but may not function correctly.");
    }
}

// Log application startup
app.Logger.LogInformation("Employee Assignment API started successfully");
app.Logger.LogInformation("Attendance Module enabled");
app.Logger.LogInformation("Email Service configured (Console logging)");
app.Logger.LogInformation("Resilience patterns enabled:");
app.Logger.LogInformation("  - Retry: 3 attempts with exponential backoff (2s, 4s, 8s)");
app.Logger.LogInformation("  - Circuit Breaker: Opens after 5 failures, resets after 1 minute");
app.Logger.LogInformation("  - SQL Connection Resiliency: 5 retries, 30s max delay");
app.Logger.LogInformation("Health check endpoint: /health");

app.Run();

// Make the implicit Program class public for integration tests (optional)
public partial class Program { }