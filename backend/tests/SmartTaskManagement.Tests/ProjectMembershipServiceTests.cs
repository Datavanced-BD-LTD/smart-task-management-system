using System.Text.Json;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;
using SmartTaskManagement.Application.Features.Projects.Validators;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;
using Xunit;

namespace SmartTaskManagement.Tests;

public sealed class ProjectMembershipServiceTests
{
    [Fact]
    public async Task Admin_can_retrieve_available_members()
    {
        var project = CreateProject(Guid.NewGuid());
        var store = CreateStore(project);
        var service = CreateService(store);

        var result = await service.ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(PageSize: 20),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, member => Assert.Equal(RoleNames.TeamMember, member.Role));
    }

    [Fact]
    public async Task Authorized_project_manager_can_retrieve_available_members()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var service = CreateService(CreateStore(project));

        var result = await service.ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Team_member_receives_forbidden_for_available_members()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var service = CreateService(CreateStore(project));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(),
            Guid.NewGuid(),
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Existing_and_inactive_users_are_excluded()
    {
        var project = CreateProject(Guid.NewGuid());
        var store = CreateStore(project);
        var result = await CreateService(store).ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.DoesNotContain(result.Items, member => member.DisplayName == "Existing Member");
        Assert.DoesNotContain(result.Items, member => member.DisplayName == "Inactive Member");
    }

    [Fact]
    public async Task Keyword_search_is_forwarded_and_applied()
    {
        var project = CreateProject(Guid.NewGuid());
        var store = CreateStore(project);
        var result = await CreateService(store).ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery("search@example.com"),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        var member = Assert.Single(result.Items);
        Assert.Equal("Searchable Member", member.DisplayName);
        Assert.Equal("search@example.com", member.Email);
    }

    [Fact]
    public async Task Pagination_returns_metadata_and_the_requested_page()
    {
        var project = CreateProject(Guid.NewGuid());
        var result = await CreateService(CreateStore(project)).ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(PageNumber: 2, PageSize: 1),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Available_member_response_contains_no_sensitive_fields()
    {
        var project = CreateProject(Guid.NewGuid());
        var result = await CreateService(CreateStore(project)).ListAvailableAsync(
            project.ProjectId,
            new AvailableProjectMemberQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        var serialized = JsonSerializer.Serialize(result.Items);

        Assert.DoesNotContain("Password", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PasswordHash", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectMembershipService CreateService(FakeProjectStore store)
    {
        return new ProjectMembershipService(
            store,
            new FakeAuthStore(),
            new FixedClock(),
            new AddProjectMemberRequestValidator(),
            new AvailableProjectMemberQueryValidator());
    }

    private static FakeProjectStore CreateStore(Project project)
    {
        var store = new FakeProjectStore(project);
        store.AddExistingMember("Existing Member", "existing@example.com");
        store.AddInactiveMember("Inactive Member", "inactive@example.com");
        store.AddAvailableMember("Searchable Member", "search@example.com");
        store.AddAvailableMember("Another Member", "another@example.com");
        return store;
    }

    private static Project CreateProject(Guid managerId)
    {
        return new Project(
            "Member project",
            "Project description",
            managerId,
            managerId,
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTime UtcNow => new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeProjectStore(Project project) : IProjectStore
    {
        private readonly List<AvailableProjectMemberResponse> availableMembers = [];
        private readonly HashSet<Guid> existingMemberIds = [];

        public Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Project?>(project.ProjectId == projectId ? project : null);
        }

        public Task<PagedResult<Project>> ListAsync(
            ProjectListQuery query,
            Guid? projectManagerId,
            Guid? memberUserId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<Project>([], query.Page, query.PageSize, 0));
        }

        public Task<bool> IsMemberAsync(
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(existingMemberIds.Contains(userId));
        }

        public Task<ProjectMember?> FindMemberAsync(
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ProjectMember?>(null);
        }

        public Task<IReadOnlyCollection<ProjectMember>> ListMembersAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ProjectMember>>([]);
        }

        public Task<PagedResult<AvailableProjectMemberResponse>> ListAvailableMembersAsync(
            Guid projectId,
            AvailableProjectMemberQuery query,
            CancellationToken cancellationToken)
        {
            var members = availableMembers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                members = members.Where(member =>
                    member.FirstName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    member.LastName.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase) ||
                    member.Email.Contains(query.Keyword, StringComparison.OrdinalIgnoreCase));
            }

            var matching = members.ToArray();
            var items = matching
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToArray();

            return Task.FromResult(new PagedResult<AvailableProjectMemberResponse>(
                items,
                query.PageNumber,
                query.PageSize,
                matching.Length));
        }

        public Task AddMemberAsync(ProjectMember member, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void RemoveMember(ProjectMember member)
        {
        }

        public Task AddAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void AddExistingMember(string displayName, string email)
        {
            existingMemberIds.Add(Guid.NewGuid());
        }

        public void AddInactiveMember(string displayName, string email)
        {
        }

        public void AddAvailableMember(string displayName, string email)
        {
            var name = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            availableMembers.Add(new AvailableProjectMemberResponse(
                Guid.NewGuid(),
                name.ElementAtOrDefault(0) ?? string.Empty,
                name.ElementAtOrDefault(1) ?? string.Empty,
                displayName,
                email,
                RoleNames.TeamMember));
        }
    }

    private sealed class FakeAuthStore : IAuthStore
    {
        public Task<User?> FindUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => Task.FromResult<User?>(null);

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<User?>(null);

        public Task<Role?> FindRoleByNameAsync(string roleName, CancellationToken cancellationToken)
            => Task.FromResult<Role?>(null);

        public Task<RefreshToken?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
            => Task.FromResult<RefreshToken?>(null);

        public Task AddUserAsync(User user, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RevokeRefreshTokenFamilyAsync(
            Guid userId,
            Guid familyId,
            DateTime revokedAtUtc,
            string reason,
            string? revokedByIp,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
