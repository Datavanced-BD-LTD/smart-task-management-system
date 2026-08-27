using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Ai;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Infrastructure.Authentication;
using SmartTaskManagement.Infrastructure.Ai;
using SmartTaskManagement.Infrastructure.Persistence;
using SmartTaskManagement.Infrastructure.Projects;
using SmartTaskManagement.Infrastructure.Tasks;
using SmartTaskManagement.Infrastructure.Dashboard;

namespace SmartTaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The 'ConnectionStrings:DefaultConnection' configuration value is required.");
        }

        // SQL transient retries help with brief connection interruptions. They do not
        // replace transaction design or retry-safe business operations.
        services.AddDbContext<ApplicationDbContext>(options => options
            .UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

        // Fail during startup rather than discovering invalid security/provider
        // configuration only when the first request reaches the application.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer) &&
                    !string.IsNullOrWhiteSpace(options.Audience) &&
                    options.SigningKey.Length >= 32 &&
                    options.AccessTokenMinutes > 0 &&
                    options.RefreshTokenDays > 0,
                "JWT configuration is missing or invalid.")
            .ValidateOnStart();

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .Validate(
                options =>
                    string.Equals(options.Provider, "Ollama", StringComparison.OrdinalIgnoreCase) &&
                    Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) &&
                    (endpoint.Scheme == Uri.UriSchemeHttp ||
                     endpoint.Scheme == Uri.UriSchemeHttps) &&
                    !string.IsNullOrWhiteSpace(options.Model) &&
                    options.TimeoutSeconds is >= 1 and <= 120,
                "AI configuration is missing or invalid. The supported provider is Ollama.")
            .ValidateOnStart();

        services.AddHttpClient(OllamaAiTaskDescriptionProvider.HttpClientName, client =>
        {
            // The provider uses a linked cancellation token for its configurable
            // timeout, avoiding two competing HttpClient timeout mechanisms.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        // These registrations are the Clean Architecture boundary: application
        // abstractions are fulfilled by EF Core and external-provider adapters here.
        services.AddScoped<IAuthStore, EfAuthStore>();
        services.AddScoped<IProjectStore, EfProjectStore>();
        services.AddScoped<ITaskStore, EfTaskStore>();
        services.AddScoped<IDashboardStore, EfDashboardStore>();
        services.AddScoped<IAiTaskDescriptionProvider, OllamaAiTaskDescriptionProvider>();
        services.AddScoped<IPasswordService, AspNetPasswordService>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<ApplicationDbContext>("sql-server", tags: ["ready"]);

        return services;
    }
}
