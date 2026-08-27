using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Api.Middleware;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Infrastructure;
using SmartTaskManagement.Infrastructure.Seeding;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // The bootstrap logger captures startup failures; this configured logger takes
    // over once dependency injection and application configuration are available.
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Program.cs is the composition root: Application registers use cases while
    // Infrastructure supplies their database, security, clock, and AI adapters.
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? [];

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException(
            "At least one CORS origin must be configured under 'Cors:AllowedOrigins'.");
    }

    builder.Services.AddCors(options =>
    {
        // Credentials are needed for the HttpOnly refresh-token cookie, so wildcard
        // origins are intentionally avoided and every frontend origin is explicit.
        options.AddPolicy("Frontend", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });
    // Add rate limiting middleware to control the number of requests per client.
    // This helps prevent abuse and ensures fair usage of the API.
    builder.Services.AddRateLimiter(options =>
    {
        // Infrastructure-level failures use the same envelope as controller and
        // exception responses, keeping frontend error handling predictable.
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsJsonAsync(
                ApiResponseFactory.Failure<object?>(
                    context.HttpContext,
                    "Too many requests. Please try again later.",
                    [new ApiError(
                        "RATE_LIMIT_EXCEEDED",
                        "The request limit has been exceeded. Please wait and try again.")]),
                cancellationToken);
        };
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    });

    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT configuration is required.");
    // Configure JWT authentication options based on the application's configuration.
    builder.Services
        .AddAuthentication(options =>
        {
            // Set the default authentication scheme to JWT Bearer.
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            // Set the default challenge scheme to JWT Bearer.
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Configure the token validation parameters for JWT Bearer authentication.
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),// Allow a 30-second clock skew for token expiration validation.
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };

            options.Events = new JwtBearerEvents
            {
                // Replace framework-default empty 401/403 responses with the API's
                // standard failure contract and trace identifier.
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponseFactory.Failure<object?>(
                            context.HttpContext,
                            "Authentication is required.",
                            [new ApiError("AUTHENTICATION_REQUIRED", "A valid access token is required.")]),
                        context.HttpContext.RequestAborted);
                },
                // Handle forbidden responses by returning a standardized API failure response.
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        ApiResponseFactory.Failure<object?>(
                            context.HttpContext,
                            "You do not have permission to perform this action.",
                            [new ApiError("FORBIDDEN", "The current user is not authorized for this resource.")]),
                        context.HttpContext.RequestAborted);
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        // These policies provide coarse role checks. Ownership, membership, and
        // assignment checks still belong in application services because they need data.
        options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleNames.Admin));
        options.AddPolicy(
            "ProjectManagerOnly",
            policy => policy.RequireRole(RoleNames.Admin, RoleNames.ProjectManager));
        options.AddPolicy(
            "TeamMemberOnly",
            policy => policy.RequireRole(RoleNames.Admin, RoleNames.ProjectManager, RoleNames.TeamMember));
    });

    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var errors = actionContext.ModelState
                    .SelectMany(entry => entry.Value?.Errors.Select(error => new ApiError(
                        "VALIDATION_ERROR",
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage,
                        entry.Key)) ?? [])
                    .ToArray();

                return new BadRequestObjectResult(ApiResponseFactory.Failure<object?>(
                    actionContext.HttpContext,
                    "One or more validation errors occurred.",
                    errors));
            };
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter a valid JWT access token."
        });

        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });
    // Add ProblemDetails middleware to provide standardized error responses.
    builder.Services.AddProblemDetails();
    // Add global exception handler middleware to catch and handle exceptions
    // globally throughout the application.
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();// Enable Serilog request logging middleware
    app.UseExceptionHandler();// Enable global exception handling middleware
    app.UseHttpsRedirection();// Enable HTTPS redirection middleware
    app.UseCors("Frontend");// Enable CORS middleware for the frontend
    app.UseRateLimiter();// Enable rate limiting middleware

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();// Enable Swagger middleware
        app.UseSwaggerUI();// Enable Swagger UI middleware
    }

    // Authentication must populate HttpContext.User before authorization evaluates it.
    app.UseAuthentication();// Enable authentication middleware
    app.UseAuthorization();// Enable authorization middleware

    // Automatic migration/seeding is a development convenience. Production should
    // normally apply reviewed migrations as an explicit deployment step.
    if (app.Environment.IsDevelopment() ||
        app.Configuration.GetValue<bool>("Authentication:ApplyMigrationsOnStartup"))
    {
        await AuthDbSeeder.InitializeAsync(app.Services, app.Configuration);
    }

    app.MapControllers();// Map controller routes

    app.MapGet("/", (HttpContext context) => Results.Ok(ApiResponseFactory.Success(
        context,
        new
        {
            service = "Smart Task Management System API",
            status = "running"
        },
        "Service status retrieved successfully.")));

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
