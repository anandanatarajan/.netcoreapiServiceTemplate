using NLog;
namespace Intellimix_Template.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Try to get correlation id from request or create a new one
            if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[CorrelationHeader] = correlationId;
            }

            // Add correlation id to response
            context.Response.Headers[CorrelationHeader] = correlationId;
            using (ScopeContext.PushProperty("CorrelationId", correlationId)) { 
            await _next(context);
            }
           
        }

    }
}
