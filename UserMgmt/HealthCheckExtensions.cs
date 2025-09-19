using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupermarketRepository;
using System.Diagnostics;
namespace Intellimix_Template.UserMgmt
{
    public static class HealthCheckExtensions
    {

        public static IHealthChecksBuilder AddUserModuleHealthChecks(this IHealthChecksBuilder builder)
        {
            //return builder.AddCheck("UserModule", () =>
            //{
            //    return HealthCheckResult.Healthy("User module is healthy");
            //}, tags: new[] { "user_module" });
            return builder.AddCheck<UserModuleHealthCheck>(HealthCheckTags.Name, tags: new[] { HealthCheckTags.ReadinessTag });
        }
    }

    public static class HealthCheckTags
    {
        public const string Name = "UserModule";
        public const string ReadinessTag = "user-module-readiness";
        public const string LivenessTag = "user-module-liveness";
    }
    public class UserModuleHealthCheck : IHealthCheck
    {
        private readonly ILogger<UserModuleHealthCheck> _logger;
        private readonly IDBCrud _dbCrud;
        private static readonly ActivitySource _activitySource = new ActivitySource("UserModule.Health","1.0.0");
        public UserModuleHealthCheck(ILogger<UserModuleHealthCheck> logger, IDBCrud dbCrud)
        {
            _logger = logger;
            _dbCrud = dbCrud;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Implement your health check logic here
            bool isHealthy = true; // Replace with actual health check logic
            using var activity = _activitySource.StartActivity("UserModuleHealthCheck");
            activity?.SetTag("module", "UserModule");

            if (isHealthy)
            {
                activity?.SetTag("health.status", "Healthy");
                return Task.FromResult(HealthCheckResult.Healthy("User module is healthy"));
            }
            else
            {
                activity?.SetTag("health.status", "Unhealthy");
                return Task.FromResult(HealthCheckResult.Unhealthy("User module is unhealthy"));
            }
        }

        
    }
}
