using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SmartTaskManagement.Application.Features.Auth;
using SmartTaskManagement.Application.Features.Auth.Validators;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Application.Features.Projects.Validators;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Application.Abstractions.Ai;
using SmartTaskManagement.Application.Features.Ai;
using SmartTaskManagement.Application.Features.Dashboard;
using SmartTaskManagement.Application.Features.Tasks;

namespace SmartTaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<ProjectMembershipService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAiTaskDescriptionService, AiTaskDescriptionService>();

        return services;
    }
}
