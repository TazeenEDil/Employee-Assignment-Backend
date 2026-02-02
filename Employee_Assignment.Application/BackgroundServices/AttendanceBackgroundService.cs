using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Employee_Assignment.Application.BackgroundServices
{
    /// <summary>
    /// Background service that automatically marks employees as absent
    /// Runs daily at 11:59 PM Pakistan time
    /// </summary>
    public class AttendanceBackgroundService : BackgroundService
    {
        private readonly ILogger<AttendanceBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly TimeZoneInfo PakistanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");

        public AttendanceBackgroundService(
            ILogger<AttendanceBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Attendance Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pakistanNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);

                    // Run at 11:59 PM Pakistan time
                    var targetTime = new TimeSpan(23, 59, 0);
                    var currentTime = pakistanNow.TimeOfDay;

                    // Calculate delay until next run
                    TimeSpan delay;
                    if (currentTime < targetTime)
                    {
                        // Run today at 11:59 PM
                        delay = targetTime - currentTime;
                    }
                    else
                    {
                        // Run tomorrow at 11:59 PM
                        delay = TimeSpan.FromDays(1) - (currentTime - targetTime);
                    }

                    _logger.LogInformation("Next attendance check scheduled in {Hours} hours and {Minutes} minutes",
                        delay.Hours, delay.Minutes);

                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested)
                    {
                        await MarkAbsentEmployees();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Attendance Background Service");
                    // Wait 1 hour before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Attendance Background Service stopped");
        }

        private async Task MarkAbsentEmployees()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

                var pakistanNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone);
                var today = pakistanNow.Date;

                _logger.LogInformation("Starting automatic absent marking for {Date}", today.ToShortDateString());

                await attendanceService.MarkAbsentEmployeesAsync(today);

                _logger.LogInformation("Completed automatic absent marking for {Date}", today.ToShortDateString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to mark absent employees");
            }
        }
    }
}