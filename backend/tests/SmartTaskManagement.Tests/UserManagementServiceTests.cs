using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.UserManagement;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.UserManagement;
using SmartTaskManagement.Application.Features.UserManagement.Validators;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;
using Xunit;

namespace SmartTaskManagement.Tests;

public sealed class UserManagementServiceTests
{
    [Fact]
    public async Task Admin_can_create_a_project_manager_user()
    {
        var store = new FakeUserManagementStore();
        var service = CreateService(store);

        var result = await service.CreateAsync(
            new CreateManagedUserRequest(
                "maria.manager@example.com",
                "StrongPass1!",
                "Maria",
                "Manager",
                RoleNames.ProjectManager),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal("Maria Manager", result.DisplayName);
        Assert.Contains(RoleNames.ProjectManager, result.Roles);
        Assert.Equal("HASHED", store.AddedUser!.PasswordHash);
    }

    [Fact]
    public async Task Non_admin_cannot_manage_users()
    {
        var service = CreateService(new FakeUserManagementStore());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(
            new CreateManagedUserRequest(
                "member@example.com",
                "StrongPass1!",
                "Team",
                "Member",
                RoleNames.TeamMember),
            Guid.NewGuid(),
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_managed_role_is_rejected()
    {
        var service = CreateService(new FakeUserManagementStore());

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateManagedUserRequest(
                "member@example.com",
                "StrongPass1!",
                "Team",
                "Member",
                RoleNames.Admin),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None));
    }

    [Fact]
    public async Task Admin_can_change_an_active_user_role()
    {
        var user = CreateUser("member@example.com", "Member", "User");
        user.AssignRole(new Role(3, RoleNames.TeamMember));
        var store = new FakeUserManagementStore(user);
        var service = CreateService(store);

        var result = await service.UpdateRoleAsync(
            user.UserId,
            new UpdateManagedUserRoleRequest(RoleNames.ProjectManager),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Contains(RoleNames.ProjectManager, result.Roles);
        Assert.DoesNotContain(RoleNames.TeamMember, result.Roles);
    }

    [Fact]
    public async Task Admin_cannot_change_an_admin_account_role()
    {
        var user = CreateUser("admin@example.com", "System", "Administrator");
        user.AssignRole(new Role(1, RoleNames.Admin));
        var service = CreateService(new FakeUserManagementStore(user));

        await Assert.ThrowsAsync<ProtectedUserException>(() => service.UpdateRoleAsync(
            user.UserId,
            new UpdateManagedUserRoleRequest(RoleNames.TeamMember),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None));
    }

    [Fact]
    public async Task Admin_user_list_is_paginated()
    {
        var store = new FakeUserManagementStore(
            CreateUser("one@example.com", "One", "User"),
            CreateUser("two@example.com", "Two", "User"));
        var service = CreateService(store);

        var result = await service.ListAsync(
            new AdminUserListQuery(PageNumber: 1, PageSize: 1),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    private static UserManagementService CreateService(FakeUserManagementStore store)
    {
        return new UserManagementService(
            store,
            new FakePasswordService(),
            new FixedClock(),
            new CreateManagedUserRequestValidator(),
            new UpdateManagedUserRoleRequestValidator(),
            new AdminUserListQueryValidator());
    }

    private static User CreateUser(string email, string firstName, string lastName)
    {
        return new User(email, firstName, lastName);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTime UtcNow { get; } = new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string HashPassword(User user, string password) => "HASHED";

        public bool VerifyPassword(User user, string password, string passwordHash) => true;
    }

    private sealed class FakeUserManagementStore(params User[] users) : IUserManagementStore
    {
        private readonly List<User> users = users.ToList();
        private readonly List<Role> roles =
        [
            new Role(1, RoleNames.Admin),
            new Role(2, RoleNames.ProjectManager),
            new Role(3, RoleNames.TeamMember)
        ];

        public User? AddedUser { get; private set; }

        public Task<bool> ExistsByNormalizedEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(users.Any(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(users.SingleOrDefault(user => user.UserId == userId));
        }

        public Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            return Task.FromResult(roles.SingleOrDefault(role => role.Name == roleName));
        }

        public Task<PagedResult<ManagedUserResponse>> ListAsync(
            AdminUserListQuery query,
            CancellationToken cancellationToken)
        {
            var filtered = users
                .Where(user => user.IsActive)
                .Where(user => string.IsNullOrWhiteSpace(query.Keyword) ||
                    user.Email.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    user.FirstName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    user.LastName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase))
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(ToResponse)
                .ToArray();

            var totalCount = users.Count(user => user.IsActive &&
                (string.IsNullOrWhiteSpace(query.Keyword) ||
                 user.Email.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                 user.FirstName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                 user.LastName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase)));

            return Task.FromResult(new PagedResult<ManagedUserResponse>(
                filtered,
                query.PageNumber,
                query.PageSize,
                totalCount));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUser = user;
            users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static ManagedUserResponse ToResponse(User user)
        {
            var roles = user.UserRoles
                .Where(userRole => userRole.Role is not null)
                .Select(userRole => userRole.Role!.Name)
                .ToArray();

            return new ManagedUserResponse(
                user.UserId,
                user.Email,
                user.FirstName,
                user.LastName,
                $"{user.FirstName} {user.LastName}",
                roles,
                user.IsActive,
                user.CreatedAtUtc);
        }
    }
}
