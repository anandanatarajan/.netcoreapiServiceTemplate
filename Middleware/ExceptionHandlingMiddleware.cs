namespace Intellimix_Template.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerFactory _loggerFactory;
        public ExceptionHandlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _loggerFactory = loggerFactory;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            
            var logger = _loggerFactory.CreateLogger<ExceptionHandlingMiddleware>();
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred while processing the request.");
                // Log the exception (you can use a logging framework here)
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Set the response status code and message
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var problemDetails = new
                {
                    Status = context.Response.StatusCode,
                    Title = "An unexpected error occurred.",
                    Detail = ex.Message
                };
                await context.Response.WriteAsync(text: problemDetails.ToString()!);
            }
        }
    }
}
