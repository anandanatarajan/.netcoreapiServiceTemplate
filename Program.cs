using Hangfire;
using Intellimix_Template.Messaging;
using Intellimix_Template.Models;
using Intellimix_Template.UserMgmt;
using Intellimix_Template.utils;
using LiteDB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Web;
using Npgsql;
using NpgsqlTypes;
using NPoco;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using SupermarketRepository;
using SuperMarketRepository.EmailLibrary;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Intellimix_Template
{
    public class Program
    {
        public  static async Task Main(string[] args)
        {


            try
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("mail.conf")
                    .Build();
                var logger = NLog.LogManager.Setup()
                         .LoadConfigurationFromAppSettings()  // loads nlog.config automatically
                            .GetCurrentClassLogger();
                var logFactory = new NLog.Extensions.Logging.NLogLoggerFactory();
                var prglogger = logFactory.CreateLogger<Program>();

                try
                {


                    var builder = WebApplication.CreateBuilder(args);

                    // Add services to the container.
                    builder.Services.AddHealthChecks().AddCheck("Self",() => HealthCheckResult.Healthy())
                        .AddUserModuleHealthChecks();

                    builder.Services.AddControllers();
                    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                    builder.Services.AddOpenApi();
                    builder.Logging.ClearProviders();

                    builder.Services.AddSingleton<ILiteDatabase>(new LiteDatabase("config.db"));
                    string dbconn = string.Empty;
                    if (builder.Environment.IsDevelopment())
                    {
                        dbconn = config.GetConnectionString("DevConnString")??"";
                    }
                    else
                    {
                        dbconn = config.GetConnectionString("ProdConnString") ?? "";
                    }

                    //builder.Services.AddDBClassWithOptions(opt =>
                    //{
                    //    opt.ConnectionString = dbconn;
                    //    opt.LogSQLCommands = true;
                    //    opt.DbProviderFactory = Npgsql.NpgsqlFactory.Instance;
                    //    opt.DatabaseType = DatabaseType.PostgreSQL;

                    //});
                    builder.Services.AddScoped<IDBCrud, DBClass>(_ =>
                    {
                        
                        return new DBClass(new MyDbOptions
                        {
                            ConnectionString = dbconn,
                            LogSQLCommands = true,
                            DbProviderFactory = Npgsql.NpgsqlFactory.Instance,
                            DatabaseType = DatabaseType.PostgreSQL
                        });
                    });
                    var mailsettings = new SuperEMailSettings();
                    config.GetSection("SuperEmailSettings").Bind(mailsettings);
                    builder.Services.AddHangfire(cfg =>
                    {
                        cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
                        cfg.UseRecommendedSerializerSettings();
                        cfg.UseDefaultTypeSerializer();
                        cfg.UseSimpleAssemblyNameTypeSerializer();
                        cfg.UseInMemoryStorage();

                    });
                    builder.Services.AddHangfireServer();
                    builder.Services.AddSingleton<SuperEMailSettings>(_ => mailsettings);
                    builder.Services.AddScoped< MailDatastoreOperations>(x => new MailDatastoreOperations(mailsettings, new BackgroundJobClient()));
                    builder.Services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(policy =>
                        {
                            var origins = config.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
                            policy.WithOrigins(origins)
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        });
                    });

                    

                    builder.Host.UseNLog();
                    builder.Services.Configure<JwtSettings>(config.GetSection("Jwt"));
                    var jwtSettings = config.GetSection("Jwt").Get<JwtSettings>();
                    builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>( _ =>
                    {
                        return new RabbitMqEventBus(config.GetValue<string>("RabbitMqConnectionString") ?? "host=localhost");
                    });
                    
                    builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = jwtSettings?.Issuer,
                                ValidAudience = jwtSettings?.Audience,
                                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Convert.FromBase64String(jwtSettings?.Key!)),
                                ClockSkew = TimeSpan.Zero // Optional: Eliminate clock skew
                            };

                            options.Events = new JwtBearerEvents
                            {
                                OnAuthenticationFailed = context =>
                                {
                                    Console.WriteLine($"JWT Auth failed: {context.Exception.Message}");
                                    return Task.CompletedTask;
                                },
                                OnChallenge = context =>
                                {
                                    // override default 403 with 401 if you want
                                    context.HandleResponse();
                                    context.Response.StatusCode = 401;
                                    return context.Response.WriteAsync("Token is invalid or expired.");
                                }
                            };
                        });
                    builder.Services.AddAuthorization();

                    builder.Services.AddApiVersioning(options =>
                    {
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                        options.ReportApiVersions = true;
                    }).AddApiExplorer(opt =>
                    {
                        opt.GroupNameFormat = "'v'VVV";
                        opt.SubstituteApiVersionInUrl = true;
                    });

                    builder.Services.AddRateLimiter(options =>
                    {
                        options.AddFixedWindowLimiter("fixed", opt =>
                        {
                            opt.PermitLimit = 100;
                            opt.Window = TimeSpan.FromMinutes(1);
                            opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                            opt.QueueLimit = 50;

                        });
                    });

                    builder.Services.AddHttpLogging(logging =>
                    {
                        logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPropertiesAndHeaders | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponsePropertiesAndHeaders;
                        logging.MediaTypeOptions.AddText("application/json");
                        logging.RequestBodyLogLimit = 4096; // limit size
                        logging.ResponseBodyLogLimit = 4096;


                        // Remove sensitive headers
                        logging.RequestHeaders.Remove("Authorization");
                        logging.RequestHeaders.Remove("Cookie");
                        logging.RequestHeaders.Remove("Set-Cookie");

                        logging.ResponseHeaders.Remove("Set-Cookie");
                    });
                    var appMeter = new Meter("Intellimix_Template.Service.Metrics", "1.0.0");
                    var appActivitySource = new ActivitySource("Intellimix.Template");
                    var tracingOtlpEndpoint = config.GetValue<string>("TracingOtlpEndpoint");
                    var otel = builder.Services.AddOpenTelemetry();
                    otel.ConfigureResource(resource => resource
                                            .AddService(serviceName: builder.Environment.ApplicationName));
                    otel.WithMetrics(metrics => metrics// Metrics provider from OpenTelemetry
                                        .AddAspNetCoreInstrumentation()
                                        .AddMeter(appMeter.Name)
                                        // Metrics provides by ASP.NET Core in .NET 8
                                        .AddMeter("Microsoft.AspNetCore.Hosting")
                                        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                                        // Metrics provided by System.Net libraries
                                        .AddMeter("System.Net.Http")
                                        .AddMeter("System.Net.NameResolution")
                                        .AddConsoleExporter()
                                        .AddPrometheusExporter());

                    otel.WithTracing(tracing =>
                    {
                        tracing.AddAspNetCoreInstrumentation();
                        tracing.AddHttpClientInstrumentation();
                        tracing.AddSource(appActivitySource.Name);
                        if (tracingOtlpEndpoint != null)
                        {
                            tracing.AddOtlpExporter(otlpOptions =>
                            {
                                otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
                            });
                        }
                        else
                        {
                            tracing.AddConsoleExporter();
                        }

                    });

                    builder.WebHost.ConfigureKestrel(opt => { 
                        opt.AddServerHeader = false;
                        opt.Limits.KeepAliveTimeout=TimeSpan.FromMinutes(2);
                        opt.Limits.RequestHeadersTimeout=TimeSpan.FromMinutes(1);
                        opt.ListenAnyIP(5254, listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                            // For HTTPS
                            //listenOptions.UseHttps("cert.pfx", "certpassword");
                        });

                    });
                    builder.Host.UseConsoleLifetime(options =>
                    {
                        options.SuppressStatusMessages = false;
                    });
                    builder.Services.AddCircuitBreakerService(config);
                    builder.Services.AddResilientApiClient(config, prglogger);
                    var app = builder.Build();

                    // Configure the HTTP request pipeline.
                    if (app.Environment.IsDevelopment())
                    {
                        app.MapOpenApi();
                        app.MapScalarApiReference();
                    }

                    app.UseHttpLogging();

                    app.UseMiddleware<Middleware.ExceptionHandlingMiddleware>();
                    app.UseMiddleware<Middleware.CorrelationIdMiddleware>();
                    app.UseHealthChecks("/health");
                    app.UseOpenTelemetryPrometheusScrapingEndpoint();
                    app.MapPrometheusScrapingEndpoint();
                    app.UseCors();
                    app.UseCircuitBreaker();
                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.MapControllers();
                    app.Lifetime.ApplicationStopping.Register(() =>
                    {
                        var logger = app.Services.GetService<ILogger<Program>>();
                        logger?.LogInformation("Application is shutting down gracefully...");
                    });

                    var cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (_, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                    };

                    await app.RunAsync(cts.Token);
                    //app.Run();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error thrown while starting :");
                    throw;

                }
                finally
                {
                    NLog.LogManager.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error thrown while starting :" + ex.ToString());
                throw;
            }
        }
    }
}
