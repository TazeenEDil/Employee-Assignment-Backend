using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;

namespace Employee_Assignment.Infrastructure.Resilience
{
    public static class PollyPolicies
    {
        // RETRY POLICY
        // Retries failed requests with exponential backoff
        public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // Handles 408 errors
                .OrResult(response => (int)response.StatusCode == 429) // Too Many Requests
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var errorMessage = outcome.Result != null
                            ? outcome.Result.StatusCode.ToString()
                            : outcome.Exception?.Message ?? "Unknown error";

                        logger.LogWarning(
                            "Retry {RetryCount} after {Delay}s due to: {Result}",
                            retryCount,
                            timespan.TotalSeconds,
                            errorMessage
                        );
                    }
                );
        }

        // CIRCUIT BREAKER POLICY
        // Opens circuit after consecutive failures, prevents cascading failures
        public static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5, // Break after 5 consecutive failures
                    durationOfBreak: TimeSpan.FromSeconds(30), // Keep circuit open for 30 seconds
                    onBreak: (outcome, duration) =>
                    {
                        var errorMessage = outcome.Result != null
                            ? outcome.Result.StatusCode.ToString()
                            : outcome.Exception?.Message ?? "Unknown error";

                        logger.LogError(
                            "Circuit breaker opened for {Duration}s due to: {Result}",
                            duration.TotalSeconds,
                            errorMessage
                        );
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker reset - service is healthy again");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Circuit breaker half-open - testing if service recovered");
                    }
                );
        }

        // TIMEOUT POLICY
        // Prevents requests from hanging indefinitely
        public static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy()
        {
            return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        }

        // COMBINED POLICY (Timeout → Retry → Circuit Breaker)
        public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
        {
            var timeout = GetTimeoutPolicy();
            var retry = GetRetryPolicy(logger);
            var circuitBreaker = GetCircuitBreakerPolicy(logger);

            return Policy.WrapAsync(circuitBreaker, retry, timeout);
        }
    }
}