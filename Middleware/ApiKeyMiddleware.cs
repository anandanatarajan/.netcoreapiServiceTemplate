using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Intellimix_Template.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ApiKeyHeaderName = "X-Api-Key";
        

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
         
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey) )
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
            
            // If the API key is valid, continue processing the request
            var appsettings = context.RequestServices.GetService<IConfiguration>();
            if (appsettings != null)
            {
                var apiKeyValue = appsettings.GetValue<string>("ApiKey");
        
                if (apiKeyValue is null || !apiKeyValue.Equals(apiKey))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Key not found");
                return;
            }
                await _next(context);
        }
    }


    public class ApiKeyAuthenticateAttribute : Attribute, IAsyncActionFilter
    {
        //private const string ApiKeyHeaderName = "X-Api-Key";
        
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<ApiKeyAuthenticateAttribute>>();
            // Check if the API key is present in the request headers
            var apiKeyHeader = context.HttpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKeyHeader))
            {
                logger?.LogError("Api Key is required");
                context.Result = new UnauthorizedObjectResult("Api Key is required");
                return;// Task.CompletedTask;
            }
            

            // If the API key is valid, continue processing the request
            var appsettings = context.HttpContext.RequestServices.GetService<IConfiguration>();
            if (appsettings != null)
            {
                var apiKeyValue = appsettings.GetValue<string>("ApiKey");

                if (apiKeyValue is null || !apiKeyValue.Equals(apiKeyHeader))
                {
                    logger?.LogError("Invalid Api Key");
                    context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Result = new UnauthorizedObjectResult("Unauthorized");
                    return;// Task.CompletedTask;
                }
            }
            else
            {
                logger?.LogError("Api Key not found in configuration");
                context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Result = new NotFoundObjectResult("settings not found");
                return;// Task.CompletedTask;
            }
         await next(); // Continue to the next action filter or action method  
        }
    }
}
