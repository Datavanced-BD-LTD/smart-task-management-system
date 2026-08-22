using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Persistence;

namespace SmartTaskManagement.Infrastructure.Authentication;

public sealed class EfAuthStore(ApplicationDbContext dbContext) : IAuthStore
{
    public Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public Task<User?> FindUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.UserId == userId, cancellationToken);
    }

    public Task<Role?> FindRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .ThenInclude(user => user!.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                refreshToken => refreshToken.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken)
    {
        await dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task RevokeRefreshTokenFamilyAsync(
        Guid userId,
        Guid familyId,
        DateTime revokedAtUtc,
        string reason,
        string? revokedByIp,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(refreshToken =>
                refreshToken.UserId == userId &&
                refreshToken.FamilyId == familyId &&
                refreshToken.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in activeTokens)
        {
            refreshToken.Revoke(revokedAtUtc, reason, revokedByIp);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
