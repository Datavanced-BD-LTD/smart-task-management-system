using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.UserManagement;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.UserManagement;

public sealed class UserManagementService(
    IUserManagementStore store,
    IPasswordService passwordService,
    ISystemClock systemClock,
    IValidator<CreateManagedUserRequest> createValidator,
    IValidator<UpdateManagedUserRequest> updateValidator,
    IValidator<UpdateManagedUserRoleRequest> updateRoleValidator,
    IValidator<AdminUserListQuery> listValidator)
{
    public async Task<ManagedUserResponse> CreateAsync(
        CreateManagedUserRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> currentRoles,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(currentRoles);
        await ValidateAsync(createValidator, request, cancellationToken);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await store.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException();
        }

        var role = await FindManagedRoleAsync(request.Role, cancellationToken);
        var user = new User(request.Email, request.FirstName, request.LastName);
        user.SetPasswordHash(passwordService.HashPassword(user, request.Password));
        user.AssignRole(role);

        await store.AddAsync(user, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<PagedResponse<ManagedUserResponse>> ListAsync(
        AdminUserListQuery query,
        IReadOnlyCollection<string> currentRoles,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(currentRoles);
        await ValidateAsync(listValidator, query, cancellationToken);

        var result = await store.ListAsync(query, cancellationToken);
        return new PagedResponse<ManagedUserResponse>(
            result.Items,
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<ManagedUserResponse> UpdateRoleAsync(
        Guid userId,
        UpdateManagedUserRoleRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> currentRoles,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(currentRoles);
        await ValidateAsync(updateRoleValidator, request, cancellationToken);

        var user = await store.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new ManagedUserNotFoundException(userId);
        }

        if (userId == currentUserId || HasRole(user, RoleNames.Admin))
        {
            throw new ProtectedUserException();
        }

        var role = await FindManagedRoleAsync(request.Role, cancellationToken);
        user.ReplaceRoles(role, systemClock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task<ManagedUserResponse> UpdateAsync(
        Guid userId,
        UpdateManagedUserRequest request,
        IReadOnlyCollection<string> currentRoles,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(currentRoles);
        await ValidateAsync(updateValidator, request, cancellationToken);

        var user = await store.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new ManagedUserNotFoundException(userId);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var existingUser = await store.FindByNormalizedEmailAsync(
            normalizedEmail,
            cancellationToken);
        if (existingUser is not null && existingUser.UserId != userId)
        {
            throw new DuplicateEmailException();
        }

        user.UpdateProfile(
            request.Email,
            request.FirstName,
            request.LastName,
            systemClock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    public async Task DeleteAsync(
        Guid userId,
        Guid currentUserId,
        IReadOnlyCollection<string> currentRoles,
        CancellationToken cancellationToken)
    {
        EnsureAdmin(currentRoles);

        var user = await store.FindByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new ManagedUserNotFoundException(userId);
        }

        if (userId == currentUserId || HasRole(user, RoleNames.Admin))
        {
            throw new ProtectedUserException();
        }

        user.Deactivate(systemClock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> FindManagedRoleAsync(
        string requestedRole,
        CancellationToken cancellationToken)
    {
        var roleName = NormalizeRole(requestedRole);
        if (roleName is null)
        {
            throw new InvalidManagedUserRoleException();
        }

        var role = await store.FindRoleByNameAsync(roleName, cancellationToken);
        return role ?? throw new InvalidManagedUserRoleException();
    }

    private static void EnsureAdmin(IReadOnlyCollection<string> roles)
    {
        if (!roles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("Only Admin users can manage user accounts.");
        }
    }

    private static bool HasRole(User user, string roleName)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role is not null &&
            string.Equals(userRole.Role.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeRole(string role)
    {
        var normalizedRole = role?.Trim();
        return string.Equals(normalizedRole, RoleNames.ProjectManager, StringComparison.OrdinalIgnoreCase)
            ? RoleNames.ProjectManager
            : string.Equals(normalizedRole, RoleNames.TeamMember, StringComparison.OrdinalIgnoreCase)
                ? RoleNames.TeamMember
                : null;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static ManagedUserResponse ToResponse(User user)
    {
        var roles = user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return new ManagedUserResponse(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            string.IsNullOrWhiteSpace(displayName) ? user.Email : displayName,
            roles,
            user.IsActive,
            user.CreatedAtUtc);
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
