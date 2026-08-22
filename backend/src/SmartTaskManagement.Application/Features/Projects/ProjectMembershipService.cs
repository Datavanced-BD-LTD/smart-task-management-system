using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.Projects;

public sealed class ProjectMembershipService(
    IProjectStore projectStore,
    IAuthStore authStore,
    ISystemClock systemClock,
    IValidator<AddProjectMemberRequest> addMemberValidator,
    IValidator<AvailableProjectMemberQuery> availableMemberQueryValidator)
{
    public async Task<PagedResponse<AvailableProjectMemberResponse>> ListAvailableAsync(
        Guid projectId,
        AvailableProjectMemberQuery query,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(availableMemberQueryValidator, query, cancellationToken);

        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManage(project, currentUserId, roles);

        var availableMembers = await projectStore.ListAvailableMembersAsync(
            projectId,
            query,
            cancellationToken);

        return new PagedResponse<AvailableProjectMemberResponse>(
            availableMembers.Items,
            availableMembers.Page,
            availableMembers.PageSize,
            availableMembers.TotalCount,
            availableMembers.TotalPages);
    }

    public async Task<IReadOnlyCollection<ProjectMemberResponse>> ListAsync(
        Guid projectId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanAccessAsync(project, currentUserId, roles, cancellationToken);

        var members = await projectStore.ListMembersAsync(projectId, cancellationToken);

        return members.Select(ToResponse).ToArray();
    }

    public async Task<ProjectMemberResponse> AddAsync(
        Guid projectId,
        AddProjectMemberRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(addMemberValidator, request, cancellationToken);

        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManage(project, currentUserId, roles);

        var user = await authStore.FindUserByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.IsActive || !HasRole(user, RoleNames.TeamMember))
        {
            throw new InvalidProjectMemberException();
        }

        if (await projectStore.IsMemberAsync(projectId, request.UserId, cancellationToken))
        {
            throw new ProjectMemberAlreadyExistsException();
        }

        var member = new ProjectMember(
            projectId,
            request.UserId,
            currentUserId,
            systemClock.UtcNow);

        await projectStore.AddMemberAsync(member, cancellationToken);
        await projectStore.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            member.AddedByUserId,
            member.AddedAtUtc,
            GetDisplayName(user),
            GetPrimaryRole(user));
    }

    public async Task RemoveAsync(
        Guid projectId,
        Guid userId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManage(project, currentUserId, roles);

        var member = await projectStore.FindMemberAsync(projectId, userId, cancellationToken);

        if (member is null)
        {
            throw new ProjectMemberNotFoundException(projectId, userId);
        }

        projectStore.RemoveMember(member);
        await projectStore.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectStore.FindByIdAsync(projectId, cancellationToken);

        if (project is null || project.IsDeleted)
        {
            throw new ProjectNotFoundException(projectId);
        }

        return project;
    }

    private async Task EnsureCanAccessAsync(
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (IsProjectManager(roles) && project.ProjectManagerId == currentUserId)
        {
            return;
        }

        if (IsTeamMember(roles) &&
            await projectStore.IsMemberAsync(project.ProjectId, currentUserId, cancellationToken))
        {
            return;
        }

        throw new ForbiddenException("You do not have access to this project's membership.");
    }

    private static void EnsureCanManage(
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (!IsProjectManager(roles) || project.ProjectManagerId != currentUserId)
        {
            throw new ForbiddenException(
                "Only the owning Project Manager or an Admin can manage project membership.");
        }
    }

    private static bool HasRole(User user, string roleName)
    {
        return user.UserRoles.Any(userRole =>
            userRole.Role is not null &&
            string.Equals(userRole.Role.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAdmin(IReadOnlyCollection<string> roles)
    {
        return roles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsProjectManager(IReadOnlyCollection<string> roles)
    {
        return roles.Contains(RoleNames.ProjectManager, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsTeamMember(IReadOnlyCollection<string> roles)
    {
        return roles.Contains(RoleNames.TeamMember, StringComparer.OrdinalIgnoreCase);
    }

    private static ProjectMemberResponse ToResponse(ProjectMember member)
    {
        if (member.User is null)
        {
            throw new InvalidOperationException(
                "Project membership references a user that could not be loaded.");
        }

        return new ProjectMemberResponse(
            member.User.UserId,
            member.User.Email,
            member.User.FirstName,
            member.User.LastName,
            member.AddedByUserId,
            member.AddedAtUtc,
            GetDisplayName(member.User),
            GetPrimaryRole(member.User));
    }

    private static string GetDisplayName(User user)
    {
        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? user.Email
            : displayName;
    }

    private static string? GetPrimaryRole(User user)
    {
        return user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }
}
