using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;
namespace Employee_Assignment.API.HealthChecks
{
    public class CircuitBreakerHealthCheck : IHealthCheck
    {
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;
        public CircuitBreakerHealthCheck(AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreaker)
        {
            _circuitBreaker = circuitBreaker;
        }
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var state = _circuitBreaker.CircuitState;
            return state switch
            {
                CircuitState.Closed => Task.FromResult(
                    HealthCheckResult.Healthy("Circuit breaker is closed - service is healthy")),
                CircuitState.HalfOpen => Task.FromResult(
                    HealthCheckResult.Degraded("Circuit breaker is half-open - testing service recovery")),
                CircuitState.Open => Task.FromResult(
                    HealthCheckResult.Unhealthy("Circuit breaker is open - service is unavailable")),
                CircuitState.Isolated => Task.FromResult(
                    HealthCheckResult.Unhealthy("Circuit breaker is isolated - service manually disabled")),
                _ => Task.FromResult(
                    HealthCheckResult.Unhealthy("Circuit breaker state unknown"))
            };
        }
    }
}
