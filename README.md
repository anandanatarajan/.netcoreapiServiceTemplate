# Intellimix_Template

## Overview

The **Intellimix Template** is a .NET 9 backend application designed for scalability, resilience, and observability. It provides features such as health checks, JWT authentication, rate limiting, OpenTelemetry metrics/tracing, and more.

---

# Intellimix_Template

## Overview

The **Intellimix Template** is a .NET 9 backend application designed for scalability, resilience, and observability. It provides features such as health checks, JWT authentication, rate limiting, OpenTelemetry metrics/tracing, and more.

---

## Program.cs Documentation

### Entry Point

- The `Program.cs` file is the main entry point.
- It configures services, middleware, and the HTTP pipeline.

### Configuration

- Loads settings from `appsettings.json` and `mail.conf`.
- Example for JWT config in `appsettings.json`:


### Logging

- Uses NLog for logging, configured via `nlog.config`.

### Dependency Injection

- Registers services such as:
- `ILiteDatabase` (LiteDB for local storage)
- `IDBCrud` (database operations)
- `SuperEMailSettings` (email configuration)
- `MailDatastoreOperations` (email operations)
- Circuit breaker and resilient API client

### Authentication & Authorization

- JWT-based authentication using settings from configuration.
- Example:


### OpenTelemetry

- Adds tracing and metrics for observability.
- Metrics and traces can be exported to Prometheus or OTLP endpoints.

### Middleware

- Exception handling and correlation ID middleware.
- Health checks and Prometheus scraping endpoints.

### HTTP Pipeline

- CORS, authentication, authorization, health checks, and controller mapping.

---

## Application Structure

### Controllers

- `UsersController.cs`: User management endpoints.
- `TokenController.cs`: JWT token operations.
- `DefaultController.cs`: Default/root endpoints.

### Models

- `UserModel.cs`: User data structure.
- `RoleModel.cs`: User roles.
- `TokenModel.cs`: JWT token structure.

### Utilities

- `CircuitBreaker.cs`: Implements circuit breaker pattern.
- `Utility.cs`: Helper methods.

### Health Checks

- `HealthCheckExtensions.cs`: Adds custom health checks.

### Configuration Files

- `appsettings.json`: Main application settings.
- `mail.conf`: Email configuration.

---

## Examples

### Adding a Health Check

In `HealthCheckExtensions.cs`:
public static IHealthChecksBuilder AddUserModuleHealthChecks(this IHealthChecksBuilder builder) { return builder.AddCheck("UserModule", () => HealthCheckResult.Healthy()); }

### Creating a Controller

In `Controllers`:
[ApiController] [Route("api/[controller]")] public class SampleController : ControllerBase { [HttpGet] public IActionResult Get() => Ok("Hello, World!"); }


### Registering a Service

In `Program.cs`:
builder.Services.AddSingleton<IMyService, MyService>();


---

## Running the Application

1. **Development Mode**: Uses `DevConnString` from `appsettings.json`.
2. **Production Mode**: Uses `ProdConnString` from `appsettings.json`.
3. **Start**:



---

## Observability

- **Metrics**: Available at `/metrics` (Prometheus format).
- **Tracing**: Sent to configured OTLP endpoint or console.

---

## Conclusion

This documentation provides a high-level overview of the application's entry point and structure. For more details, refer to code comments and configuration files.

