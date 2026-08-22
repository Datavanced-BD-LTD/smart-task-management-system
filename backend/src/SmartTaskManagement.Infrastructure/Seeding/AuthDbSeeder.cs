using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Authentication;
using SmartTaskManagement.Infrastructure.Persistence;

namespace SmartTaskManagement.Infrastructure.Seeding;

public static class AuthDbSeeder
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        await SeedRolesAsync(dbContext, cancellationToken);
        await SeedAdminAsync(scope.ServiceProvider, dbContext, configuration, cancellationToken);
    }

    private static async Task SeedRolesAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role(1, RoleNames.Admin),
            new Role(2, RoleNames.ProjectManager),
            new Role(3, RoleNames.TeamMember)
        };

        foreach (var role in roles)
        {
            if (!await dbContext.Roles.AnyAsync(existing => existing.Name == role.Name, cancellationToken))
            {
                dbContext.Roles.Add(role);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminAsync(
        IServiceProvider serviceProvider,
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var seedOptions = configuration
            .GetSection(SeedAdminOptions.SectionName)
            .Get<SeedAdminOptions>();

        if (seedOptions is null ||
            string.IsNullOrWhiteSpace(seedOptions.Email) ||
            string.IsNullOrWhiteSpace(seedOptions.Password))
        {
            return;
        }

        var normalizedEmail = seedOptions.Email.Trim().ToUpperInvariant();
        if (await dbContext.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return;
        }

        var adminRole = await dbContext.Roles
            .SingleAsync(role => role.Name == RoleNames.Admin, cancellationToken);
        var passwordService = serviceProvider.GetRequiredService<IPasswordService>();
        var adminUser = new User(
            seedOptions.Email,
            seedOptions.FirstName,
            seedOptions.LastName);

        adminUser.SetPasswordHash(passwordService.HashPassword(adminUser, seedOptions.Password));
        adminUser.AssignRole(adminRole);

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
