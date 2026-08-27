using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Authentication;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Projects;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.Projects;

public sealed class ProjectService(
    IProjectStore projectStore,
    IAuthStore authStore,
    ISystemClock systemClock,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator,
    IValidator<ProjectListQuery> listValidator)
{
    public async Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(createValidator, request, cancellationToken);
        EnsureCanCreate(roles);

        // Project Manager ownership is resolved on the server so callers cannot
        // assign work to an arbitrary manager by changing a request field.
        var projectManagerId = await ResolveProjectManagerIdAsync(
            request.ProjectManagerId,
            currentUserId,
            roles,
            cancellationToken);
        var now = systemClock.UtcNow;
        var project = new Project(
            request.Name,
            request.Description,
            projectManagerId,
            currentUserId,
            now);

        await projectStore.AddAsync(project, cancellationToken);
        await projectStore.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(project, cancellationToken);
    }

    public async Task<ProjectResponse> UpdateAsync(
        Guid projectId,
        UpdateProjectRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManage(project, currentUserId, roles);

        var projectManagerId = await ResolveProjectManagerIdAsync(
            request.ProjectManagerId ?? project.ProjectManagerId,
            currentUserId,
            roles,
            cancellationToken,
            isUpdate: true,
            existingProjectManagerId: project.ProjectManagerId);

        project.UpdateDetails(
            request.Name,
            request.Description,
            projectManagerId,
            systemClock.UtcNow);

        await projectStore.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(project, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid projectId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManage(project, currentUserId, roles);

        project.SoftDelete(currentUserId, systemClock.UtcNow);
        await projectStore.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProjectResponse> GetByIdAsync(
        Guid projectId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        EnsureCanRead(roles);

        var project = await GetProjectAsync(projectId, cancellationToken);

        if (IsProjectManager(roles) && !IsAdmin(roles) && project.ProjectManagerId != currentUserId)
        {
            throw new ForbiddenException("Project Managers can only access their own projects.");
        }

        if (IsTeamMember(roles) &&
            !IsAdmin(roles) &&
            !IsProjectManager(roles) &&
            !await projectStore.IsMemberAsync(projectId, currentUserId, cancellationToken))
        {
            throw new ForbiddenException("Team Members can only access projects they belong to.");
        }

        return await ToResponseAsync(project, cancellationToken);
    }

    public async Task<PagedResponse<ProjectResponse>> ListAsync(
        ProjectListQuery query,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(listValidator, query, cancellationToken);
        EnsureCanRead(roles);

        // Store-level scope filters the query before pagination, so users cannot infer
        // or receive projects outside the resources their role permits them to see.
        Guid? projectManagerId =
            IsProjectManager(roles) && !IsAdmin(roles)
                ? currentUserId
                : null;
        Guid? memberUserId =
            IsTeamMember(roles) && !IsAdmin(roles) && !IsProjectManager(roles)
                ? currentUserId
                : null;
        var pagedProjects = await projectStore.ListAsync(
            query,
            projectManagerId,
            memberUserId,
            cancellationToken);

        return new PagedResponse<ProjectResponse>(
            pagedProjects.Items.Select(ToResponse).ToArray(),
            pagedProjects.Page,
            pagedProjects.PageSize,
            pagedProjects.TotalCount,
            pagedProjects.TotalPages);
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

    private async Task<Guid> ResolveProjectManagerIdAsync(
        Guid? requestedProjectManagerId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken,
        bool isUpdate = false,
        Guid? existingProjectManagerId = null)
    {
        if (!IsAdmin(roles))
        {
            if (requestedProjectManagerId.HasValue &&
                requestedProjectManagerId.Value != currentUserId)
            {
                throw new ForbiddenException(
                    "Only Admin users can assign a different Project Manager.");
            }

            return isUpdate && existingProjectManagerId.HasValue
                ? existingProjectManagerId.Value
                : currentUserId;
        }

        var projectManagerId = requestedProjectManagerId ??
            (isUpdate && existingProjectManagerId.HasValue
                ? existingProjectManagerId.Value
                : currentUserId);
        var projectManager = await authStore.FindUserByIdAsync(
            projectManagerId,
            cancellationToken);

        if (projectManager is null || !projectManager.IsActive)
        {
            throw new InvalidProjectManagerException();
        }

        var canManageProjects = projectManager.UserRoles.Any(userRole =>
            userRole.Role is not null &&
            (string.Equals(userRole.Role.Name, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(userRole.Role.Name, RoleNames.ProjectManager, StringComparison.OrdinalIgnoreCase)));

        if (!canManageProjects)
        {
            throw new InvalidProjectManagerException();
        }

        return projectManagerId;
    }

    private static void EnsureCanCreate(IReadOnlyCollection<string> roles)
    {
        if (!IsAdmin(roles) && !IsProjectManager(roles))
        {
            throw new ForbiddenException(
                "Only Admins and Project Managers can create projects.");
        }
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
                "Only the owning Project Manager or an Admin can modify this project.");
        }
    }

    private static void EnsureCanRead(IReadOnlyCollection<string> roles)
    {
        if (!IsAdmin(roles) && !IsProjectManager(roles) && !IsTeamMember(roles))
        {
            throw new ForbiddenException("The current role cannot access projects.");
        }
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

    private async Task<ProjectResponse> ToResponseAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        // Responses include friendly identity fields while keeping the entity and its
        // navigation graph out of the API contract.
        var projectManager = project.ProjectManager ?? await authStore.FindUserByIdAsync(
            project.ProjectManagerId,
            cancellationToken);
        var createdByUser = project.CreatedByUser ?? await authStore.FindUserByIdAsync(
            project.CreatedByUserId,
            cancellationToken);

        return ToResponse(project, projectManager, createdByUser);
    }

    private static ProjectResponse ToResponse(Project project)
    {
        return ToResponse(project, project.ProjectManager, project.CreatedByUser);
    }

    private static ProjectResponse ToResponse(
        Project project,
        User? projectManager,
        User? createdByUser)
    {
        return new ProjectResponse(
            project.ProjectId,
            project.Name,
            project.Description,
            project.ProjectManagerId,
            project.CreatedByUserId,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            GetDisplayName(projectManager),
            projectManager?.Email,
            GetDisplayName(createdByUser),
            createdByUser?.Email);
    }

    private static string? GetDisplayName(User? user)
    {
        if (user is null)
        {
            return null;
        }

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(displayName)
            ? user.Email
            : displayName;
    }
}
