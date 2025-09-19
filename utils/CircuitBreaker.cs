using Polly;

namespace Intellimix_Template.utils
{
    public static class CircuitBreakerService
    {
        public static IServiceCollection AddCircuitBreakerService(this IServiceCollection services, IConfiguration configuration)
        {
            var retryCount = configuration.GetValue<int>("CircuitBreaker:RetryCount", 3);
            var exceptionsAllowedBeforeBreaking = configuration.GetValue<int>("CircuitBreaker:ExceptionsAllowedBeforeBreaking", 5);
            var durationOfBreakInSeconds = configuration.GetValue<int>("CircuitBreaker:DurationOfBreakInSeconds", 30);
            var circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (ex, breakDelay) =>
                    {
                        // Log the circuit state change to 'Open'
                        Console.WriteLine($"Circuit breaker opened for {breakDelay.TotalSeconds} seconds due to: {ex.Message}");
                    },
                    onReset: () =>
                    {
                        // Log the circuit state change to 'Closed'
                        Console.WriteLine("Circuit breaker reset to closed state.");
                    },
                    onHalfOpen: () =>
                    {
                        // Log the circuit state change to 'Half-Open'
                        Console.WriteLine("Circuit breaker is half-open. Testing the waters...");
                    });
            services.AddSingleton(circuitBreakerPolicy);
            return services;
        }


        public static IApplicationBuilder UseCircuitBreaker(this IApplicationBuilder app)
        {
            // Middleware to apply circuit breaker policy
            return app.Use(async (context, next) =>
            {
                var circuitBreakerPolicy = context.RequestServices.GetRequiredService<Polly.CircuitBreaker.AsyncCircuitBreakerPolicy>();
                await circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    await next();
                });
            });
        }
        public static void AddResilientApiClient(this IServiceCollection services, IConfiguration apiClientConfiguration,ILogger logger)
        {
            services.AddHttpClient("ResilientHttpClient")
                .AddPolicyHandler(GetRetryPolicy(apiClientConfiguration,logger))
                .AddPolicyHandler(GetCircuitBreakerPolicy(apiClientConfiguration, logger));
        }
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IConfiguration config,ILogger logger)
        {
            var retryCount = config.GetValue<int>("ApiClientconfig:RetryCount", 3);
            return Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount, 
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (response, timespan, retryCount, context) =>
                {
                    // Log each retry attempt
                    logger.LogWarning($"Request failed with {response.Result.StatusCode}. Waiting {timespan} before next retry. Retry attempt {retryCount}.");
                }

                );
        }
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IConfiguration config,ILogger logger)
        {
            var exceptionsAllowedBeforeBreaking = config.GetValue<int>("ApiClientconfig:ExceptionsAllowedBeforeBreaking", 5);
            var durationOfBreakInSeconds = config.GetValue<int>("ApiClientconfig:DurationOfBreakInSeconds", 30);
            return Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking, TimeSpan.FromSeconds(durationOfBreakInSeconds),
                onBreak: (response, breakDelay) =>
                  {
                      // Log the circuit state change to 'Open'
                      logger.LogWarning($"Circuit breaker opened for {breakDelay.TotalSeconds} seconds due to: {response.Result.StatusCode}");
                  },
                  onReset: () =>
                  {
                      // Log the circuit state change to 'Closed'
                      logger.LogInformation("Circuit breaker reset to closed state.");
                  },
                  onHalfOpen: () =>
                  {
                      // Log the circuit state change to 'Half-Open'
                      logger.LogInformation("Circuit breaker is half-open. Testing the waters...");
                  }
                );
        }
    }
}
