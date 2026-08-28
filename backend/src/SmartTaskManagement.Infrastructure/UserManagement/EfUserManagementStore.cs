using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.UserManagement;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.UserManagement;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Persistence;

namespace SmartTaskManagement.Infrastructure.UserManagement;

public sealed class EfUserManagementStore(ApplicationDbContext dbContext)
    : IUserManagementStore
{
    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    public Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
    }

    public Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<Role?> FindRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<PagedResult<ManagedUserResponse>> ListAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken)
    {
        var users = dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword.Trim()}%";
            users = users.Where(user =>
                EF.Functions.Like(user.FirstName, pattern) ||
                EF.Functions.Like(user.LastName, pattern) ||
                EF.Functions.Like(user.Email, pattern));
        }

        var orderedUsers = users
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Email);

        var totalCount = await orderedUsers.CountAsync(cancellationToken);
        var skip = (long)(query.PageNumber - 1) * query.PageSize;
        var items = skip >= totalCount
            ? []
            : await orderedUsers
                .Skip((int)skip)
                .Take(query.PageSize)
                .Select(user => new ManagedUserResponse(
                    user.UserId,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.FirstName + " " + user.LastName,
                    user.UserRoles
                        .OrderBy(userRole => userRole.RoleId)
                        .Select(userRole => userRole.Role!.Name)
                        .ToArray(),
                    user.IsActive,
                    user.CreatedAtUtc))
                .ToArrayAsync(cancellationToken);

        return new PagedResult<ManagedUserResponse>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
