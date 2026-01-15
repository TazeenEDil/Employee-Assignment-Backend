using Employee_Assignment.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace Employee_Assignment.Infrastructure.Resilience
{
    public class CircuitBreakerService : ICircuitBreakerService
    {
        private readonly AsyncCircuitBreakerPolicy _databaseCircuitBreaker;
        private readonly AsyncPolicy _retryPolicy;
        private readonly IAsyncPolicy _combinedPolicy;
        private readonly ILogger<CircuitBreakerService> _logger;

        public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
        {
            _logger = logger;
            _databaseCircuitBreaker = CreateDatabaseCircuitBreaker();
            _retryPolicy = CreateRetryPolicy();
            _combinedPolicy = Policy.WrapAsync(_databaseCircuitBreaker, _retryPolicy);
        }

        private AsyncCircuitBreakerPolicy CreateDatabaseCircuitBreaker()
        {
            return Policy
                .Handle<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<InvalidOperationException>(ex =>
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogError(
                            exception,
                            "⚠️ Database circuit breaker OPENED for {Duration} minutes. Service temporarily unavailable.",
                            duration.TotalMinutes
                        );
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("✅ Database circuit breaker RESET - service is healthy again");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation("🔄 Database circuit breaker HALF-OPEN - testing connection recovery");
                    }
                );
        }

        private AsyncPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<DbUpdateException>()
                .Or<TimeoutException>()
                .Or<InvalidOperationException>(ex =>
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "🔄 Database retry attempt {RetryCount}/3 after {Delay}s",
                            retryCount,
                            timespan.TotalSeconds
                        );
                    }
                );
        }

        /// <summary>
        /// Executes a database operation with full resilience (retry + circuit breaker)
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            return await _combinedPolicy.ExecuteAsync(operation);
        }
        public string GetCircuitBreakerState()
        {
            return _databaseCircuitBreaker.CircuitState.ToString();
        }
    }
}