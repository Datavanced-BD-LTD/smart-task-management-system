using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Abstractions.Authentication;

public interface IAuthStore
{
    Task<User?> FindUserByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<User?> FindUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Role?> FindRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken);

    Task<RefreshToken?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task AddUserAsync(User user, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task RevokeRefreshTokenFamilyAsync(
        Guid userId,
        Guid familyId,
        DateTime revokedAtUtc,
        string reason,
        string? revokedByIp,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
