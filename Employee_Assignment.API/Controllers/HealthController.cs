using Employee_Assignment.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Employee_Assignment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            ApplicationDbContext context,
            ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Basic health check endpoint
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                service = "Employee Assignment API"
            });
        }

        /// <summary>
        /// Database connectivity check
        /// </summary>
        [HttpGet("database")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                // Simple query to test database connection
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    var employeeCount = await _context.Employees.CountAsync();

                    return Ok(new
                    {
                        status = "Healthy",
                        database = "Connected",
                        employeeCount,
                        timestamp = DateTime.UtcNow
                    });
                }

                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    database = "Disconnected",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");

                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    database = "Error",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Detailed health check with all components
        /// </summary>
        [HttpGet("detailed")]
        [Authorize]
        public async Task<IActionResult> GetDetailedHealth()
        {
            var healthChecks = new List<HealthCheckItem>();

            // Check API
            healthChecks.Add(new HealthCheckItem
            {
                Component = "api",
                Status = "Healthy",
                Details = "API is running"
            });

            // Check Database
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                healthChecks.Add(new HealthCheckItem
                {
                    Component = "database",
                    Status = canConnect ? "Healthy" : "Unhealthy",
                    Details = canConnect ? "Database is connected" : "Database is disconnected"
                });
            }
            catch (Exception ex)
            {
                healthChecks.Add(new HealthCheckItem
                {
                    Component = "database",
                    Status = "Unhealthy",
                    Details = $"Database error: {ex.Message}"
                });
            }

            // Check data counts
            try
            {
                var counts = new
                {
                    employees = await _context.Employees.CountAsync(),
                    users = await _context.Users.CountAsync(),
                    positions = await _context.Positions.CountAsync(),
                    roles = await _context.Roles.CountAsync()
                };

                healthChecks.Add(new HealthCheckItem
                {
                    Component = "dataCounts",
                    Status = "Healthy",
                    Details = $"Employees: {counts.employees}, Users: {counts.users}, Positions: {counts.positions}, Roles: {counts.roles}",
                    Data = counts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get data counts");
                healthChecks.Add(new HealthCheckItem
                {
                    Component = "dataCounts",
                    Status = "Unhealthy",
                    Details = $"Failed to retrieve counts: {ex.Message}"
                });
            }

            // Determine overall health
            var overallHealthy = healthChecks.All(h => h.Status == "Healthy");

            return Ok(new
            {
                status = overallHealthy ? "Healthy" : "Degraded",
                checks = healthChecks,
                timestamp = DateTime.UtcNow
            });
        }
    }

    // Helper class for structured health check responses
    public class HealthCheckItem
    {
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}