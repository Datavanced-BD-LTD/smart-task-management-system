namespace SmartTaskManagement.Application.Features.UserManagement;

public sealed record CreateManagedUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role);

public sealed record UpdateManagedUserRoleRequest(string Role);

public sealed record AdminUserListQuery(
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ManagedUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    DateTime CreatedAtUtc);
