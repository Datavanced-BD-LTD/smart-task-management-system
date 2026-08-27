using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;

namespace SmartTaskManagement.Application.Features.Tasks;

public sealed class TaskService(
    ITaskStore taskStore,
    ISystemClock systemClock,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskRequest> updateValidator,
    IValidator<AssignTaskRequest> assignValidator,
    IValidator<UpdateTaskStatusRequest> statusValidator,
    IValidator<UpdateTaskPriorityRequest> priorityValidator,
    IValidator<TaskListQuery> listValidator) : ITaskService
{
    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        // Resource authorization is evaluated here because ownership and membership
        // cannot be determined from a role attribute alone.
        var project = await GetProjectAsync(projectId, cancellationToken);
        EnsureCanManageProject(project, currentUserId, roles, "create");
        await ValidateAssigneeAsync(projectId, request.AssignedToUserId, cancellationToken);

        var taskItem = new TaskItem(
            projectId,
            request.Title,
            request.Description,
            request.AssignedToUserId,
            currentUserId,
            request.Status,
            request.Priority,
            request.DueDate,
            systemClock.UtcNow);

        await taskStore.AddAsync(taskItem, cancellationToken);
        await taskStore.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task<PagedResponse<TaskResponse>> ListByProjectAsync(
        Guid projectId,
        TaskListQuery query,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(listValidator, query, cancellationToken);

        var project = await GetProjectAsync(projectId, cancellationToken);
        await EnsureCanViewProjectAsync(project, currentUserId, roles, cancellationToken);

        var pagedTasks = await taskStore.ListByProjectAsync(
            projectId,
            query,
            cancellationToken);

        return new PagedResponse<TaskResponse>(
            pagedTasks.Items,
            pagedTasks.Page,
            pagedTasks.PageSize,
            pagedTasks.TotalCount,
            pagedTasks.TotalPages);
    }

    public async Task<TaskResponse> GetByIdAsync(
        Guid taskId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var taskItem = await taskStore.FindByIdAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        await EnsureCanViewProjectAsync(project, currentUserId, roles, cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid taskId,
        UpdateTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var taskItem = await taskStore.FindByIdForUpdateAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        // The full update can change privileged fields, so Team Members must use the
        // narrower status endpoint even when the task is assigned to them.
        EnsureCanUpdate(project, currentUserId, roles);

        var assignedToUserId = request.AssignedToUserId ?? taskItem.AssignedToUserId;
        await ValidateAssigneeAsync(project.ProjectId, assignedToUserId, cancellationToken);

        taskItem.UpdateDetails(
            request.Title,
            request.Description,
            assignedToUserId,
            request.Status,
            request.Priority,
            request.DueDate,
            systemClock.UtcNow);

        await taskStore.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task<TaskResponse> AssignAsync(
        Guid taskId,
        AssignTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(assignValidator, request, cancellationToken);

        var taskItem = await taskStore.FindByIdForUpdateAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        EnsureCanManageProject(project, currentUserId, roles, "assign or unassign");
        await ValidateAssigneeAsync(project.ProjectId, request.AssignedUserId, cancellationToken);

        taskItem.AssignTo(request.AssignedUserId, systemClock.UtcNow);
        await taskStore.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task<TaskResponse> UpdateStatusAsync(
        Guid taskId,
        UpdateTaskStatusRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(statusValidator, request, cancellationToken);

        var taskItem = await taskStore.FindByIdForUpdateAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        // Status is intentionally a separate operation: an assigned Team Member may
        // progress work without gaining permission to change assignment or priority.
        await EnsureCanUpdateStatusAsync(
            taskItem,
            project,
            currentUserId,
            roles,
            cancellationToken);

        taskItem.ChangeStatus(request.Status, systemClock.UtcNow);
        await taskStore.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task<TaskResponse> UpdatePriorityAsync(
        Guid taskId,
        UpdateTaskPriorityRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(priorityValidator, request, cancellationToken);

        var taskItem = await taskStore.FindByIdForUpdateAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        EnsureCanManageProject(project, currentUserId, roles, "change priority");

        taskItem.ChangePriority(request.Priority, systemClock.UtcNow);
        await taskStore.SaveChangesAsync(cancellationToken);

        return await GetResponseAsync(taskItem.Id, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid taskId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var taskItem = await taskStore.FindByIdForUpdateAsync(taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
        var project = await GetProjectAsync(taskItem.ProjectId, cancellationToken);

        EnsureCanManageProject(project, currentUserId, roles, "delete");

        taskStore.Remove(taskItem);
        await taskStore.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await taskStore.FindProjectAsync(projectId, cancellationToken);

        if (project is null || project.IsDeleted)
        {
            throw new ProjectNotFoundException(projectId);
        }

        return project;
    }

    private async Task EnsureCanViewProjectAsync(
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (IsProjectManager(roles))
        {
            if (project.ProjectManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException(
                "Project Managers can only access tasks in projects they manage.");
        }

        if (IsTeamMember(roles) &&
            await taskStore.IsProjectMemberAsync(
                project.ProjectId,
                currentUserId,
                cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            "Team Members can only access tasks in projects they belong to.");
    }

    private static void EnsureCanManageProject(
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        string operation)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (IsProjectManager(roles) && project.ProjectManagerId == currentUserId)
        {
            return;
        }

        if (IsTeamMember(roles))
        {
            throw new ForbiddenException($"Team Members cannot {operation} tasks.");
        }

        throw new ForbiddenException(
            "Only the owning Project Manager or an Admin can manage tasks.");
    }

    private async Task EnsureCanUpdateStatusAsync(
        TaskItem taskItem,
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (IsProjectManager(roles))
        {
            if (project.ProjectManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException(
                "Project Managers can only update task status in projects they manage.");
        }

        if (IsTeamMember(roles) &&
            taskItem.AssignedToUserId == currentUserId &&
            await taskStore.IsProjectMemberAsync(
                project.ProjectId,
                currentUserId,
                cancellationToken))
        {
            return;
        }

        throw new ForbiddenException(
            "Team Members can only update the status of tasks assigned to them.");
    }

    private static void EnsureCanUpdate(
        Project project,
        Guid currentUserId,
        IReadOnlyCollection<string> roles)
    {
        if (IsAdmin(roles))
        {
            return;
        }

        if (IsProjectManager(roles))
        {
            if (project.ProjectManagerId == currentUserId)
            {
                return;
            }

            throw new ForbiddenException(
                "Project Managers can only update tasks in projects they manage.");
        }

        if (IsTeamMember(roles))
        {
            throw new ForbiddenException(
                "Team Members cannot use the full task update endpoint. Update task status through the status endpoint.");
        }

        throw new ForbiddenException(
            "Only Admins and the owning Project Manager can use the full task update endpoint.");
    }

    private async Task ValidateAssigneeAsync(
        Guid projectId,
        Guid? assignedToUserId,
        CancellationToken cancellationToken)
    {
        if (!assignedToUserId.HasValue)
        {
            return;
        }

        // Assignment requires both an active account and current project membership;
        // existence alone is not enough to grant access to project work.
        if (!await taskStore.IsActiveUserAsync(assignedToUserId.Value, cancellationToken))
        {
            throw new InvalidTaskAssigneeException();
        }

        if (!await taskStore.IsProjectMemberAsync(
                projectId,
                assignedToUserId.Value,
                cancellationToken))
        {
            throw new TaskAssigneeNotProjectMemberException();
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

    private async Task<TaskResponse> GetResponseAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return await taskStore.FindResponseByIdAsync(taskId, cancellationToken)
            ?? throw new InvalidOperationException(
                "The task response could not be loaded after the operation completed.");
    }
}
