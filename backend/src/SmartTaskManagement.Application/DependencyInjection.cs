using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SmartTaskManagement.Application.Features.Auth;
using SmartTaskManagement.Application.Features.Auth.Validators;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Application.Features.Projects.Validators;
using SmartTaskManagement.Application.Abstractions.Tasks;
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

        return services;
    }
}
