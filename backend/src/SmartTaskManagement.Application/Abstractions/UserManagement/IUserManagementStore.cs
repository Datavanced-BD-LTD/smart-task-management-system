using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.UserManagement;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Abstractions.UserManagement;

public interface IUserManagementStore
{
    Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Role?> FindRoleByNameAsync(
        string roleName,
        CancellationToken cancellationToken);

    Task<PagedResult<ManagedUserResponse>> ListAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
