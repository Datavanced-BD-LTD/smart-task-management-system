using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Features.Tasks;
using SmartTaskManagement.Application.Features.Tasks.Validators;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Domain.Enums;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;
using Xunit;

namespace SmartTaskManagement.Tests;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task Admin_can_create_a_task()
    {
        var adminId = Guid.NewGuid();
        var project = CreateProject(Guid.NewGuid());
        var assigneeId = Guid.NewGuid();
        var store = CreateStore(project, assigneeId, project.ProjectId);
        var service = CreateService(store);

        var result = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(assigneeId),
            adminId,
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(project.ProjectId, result.ProjectId);
        Assert.Equal(adminId, result.CreatedByUserId);
        Assert.Equal(assigneeId, result.AssignedToUserId);
    }

    [Fact]
    public async Task Project_manager_can_create_a_task_in_an_authorized_project()
    {
        var projectManagerId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        var result = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Equal(project.ProjectId, result.ProjectId);
        Assert.Equal(projectManagerId, result.CreatedByUserId);
    }

    [Fact]
    public async Task Unauthorized_user_cannot_access_another_projects_task()
    {
        var projectManagerId = Guid.NewGuid();
        var unauthorizedUserId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(
            task.Id,
            unauthorizedUserId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_cannot_delete_a_task()
    {
        var projectManagerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(teamMemberId),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(
            task.Id,
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_project_returns_project_not_found()
    {
        var store = new FakeTaskStore();
        var service = CreateService(store);

        await Assert.ThrowsAsync<ProjectNotFoundException>(() => service.CreateAsync(
            Guid.NewGuid(),
            CreateRequest(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_assigned_user_returns_assignee_error()
    {
        var project = CreateProject(Guid.NewGuid());
        var store = CreateStore(project);
        var service = CreateService(store);

        await Assert.ThrowsAsync<InvalidTaskAssigneeException>(() => service.CreateAsync(
            project.ProjectId,
            CreateRequest(Guid.NewGuid()),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None));
    }

    [Fact]
    public async Task Admin_can_assign_a_task()
    {
        var projectManagerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, assigneeId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.AssignAsync(
            task.Id,
            new AssignTaskRequest(assigneeId),
            adminId,
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(assigneeId, result.AssignedToUserId);
    }

    [Fact]
    public async Task Project_manager_can_assign_only_within_an_authorized_project()
    {
        var authorizedManagerId = Guid.NewGuid();
        var otherManagerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var project = CreateProject(otherManagerId);
        var store = CreateStore(project, assigneeId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            otherManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AssignAsync(
            task.Id,
            new AssignTaskRequest(assigneeId),
            authorizedManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Assignment_fails_when_user_is_not_a_project_member()
    {
        var projectManagerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        store.AddActiveUser(assigneeId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<TaskAssigneeNotProjectMemberException>(() => service.AssignAsync(
            task.Id,
            new AssignTaskRequest(assigneeId),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_cannot_assign_a_task()
    {
        var projectManagerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        store.AddActiveUser(assigneeId);
        store.AddMember(project.ProjectId, assigneeId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AssignAsync(
            task.Id,
            new AssignTaskRequest(assigneeId),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Assigned_team_member_can_update_task_status()
    {
        var projectManagerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(teamMemberId),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.UpdateStatusAsync(
            task.Id,
            new UpdateTaskStatusRequest(TaskStatusEnum.InProgress),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None);

        Assert.Equal(TaskStatusEnum.InProgress, result.Status);
        Assert.Equal(teamMemberId, result.AssignedToUserId);
    }

    [Fact]
    public async Task Unassigned_team_member_cannot_update_task_status()
    {
        var projectManagerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateStatusAsync(
            task.Id,
            new UpdateTaskStatusRequest(TaskStatusEnum.InProgress),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_cannot_change_priority()
    {
        var projectManagerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(teamMemberId),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdatePriorityAsync(
            task.Id,
            new UpdateTaskPriorityRequest(TaskPriority.High),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_status_and_priority_are_rejected()
    {
        var projectManagerId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.UpdateStatusAsync(
            task.Id,
            new UpdateTaskStatusRequest((TaskStatusEnum)99),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.UpdatePriorityAsync(
            task.Id,
            new UpdateTaskPriorityRequest((TaskPriority)99),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_status_transition_is_rejected()
    {
        var projectManagerId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        await Assert.ThrowsAsync<SmartTaskManagement.Domain.Exceptions.InvalidTaskStatusTransitionException>(() => service.UpdateStatusAsync(
            task.Id,
            new UpdateTaskStatusRequest(TaskStatusEnum.Completed),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    private static TaskService CreateService(FakeTaskStore store)
    {
        return new TaskService(
            store,
            new FixedClock(),
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new AssignTaskRequestValidator(),
            new UpdateTaskStatusRequestValidator(),
            new UpdateTaskPriorityRequestValidator());
    }

    private static Project CreateProject(Guid projectManagerId)
    {
        return new Project(
            "Test Project",
            "Project description",
            projectManagerId,
            projectManagerId,
            DateTime.UtcNow);
    }

    private static CreateTaskRequest CreateRequest(Guid? assignedToUserId = null)
    {
        return new CreateTaskRequest(
            "Test Task",
            "Task description",
            assignedToUserId,
            TaskStatusEnum.ToDo,
            TaskPriority.Medium,
            DateTime.UtcNow.AddDays(1));
    }

    private static FakeTaskStore CreateStore(
        Project project,
        Guid? activeUserId = null,
        Guid? memberProjectId = null)
    {
        var store = new FakeTaskStore();
        store.AddProject(project);

        if (activeUserId.HasValue)
        {
            store.AddActiveUser(activeUserId.Value);
        }

        if (activeUserId.HasValue && memberProjectId.HasValue)
        {
            store.AddMember(memberProjectId.Value, activeUserId.Value);
        }

        return store;
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTime UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeTaskStore : ITaskStore
    {
        private readonly Dictionary<Guid, Project> projects = [];
        private readonly Dictionary<Guid, TaskItem> taskItems = [];
        private readonly HashSet<Guid> activeUsers = [];
        private readonly HashSet<(Guid ProjectId, Guid UserId)> projectMembers = [];

        public void AddProject(Project project)
        {
            projects[project.ProjectId] = project;
        }

        public void AddActiveUser(Guid userId)
        {
            activeUsers.Add(userId);
        }

        public void AddMember(Guid projectId, Guid userId)
        {
            projectMembers.Add((projectId, userId));
        }

        public Task<Project?> FindProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }

        public Task<bool> IsProjectMemberAsync(
            Guid projectId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(projectMembers.Contains((projectId, userId)));
        }

        public Task<bool> IsActiveUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(activeUsers.Contains(userId));
        }

        public Task<TaskItem?> FindByIdAsync(
            Guid taskId,
            CancellationToken cancellationToken)
        {
            taskItems.TryGetValue(taskId, out var taskItem);
            return Task.FromResult(taskItem);
        }

        public Task<TaskItem?> FindByIdForUpdateAsync(
            Guid taskId,
            CancellationToken cancellationToken)
        {
            taskItems.TryGetValue(taskId, out var taskItem);
            return Task.FromResult(taskItem);
        }

        public Task<IReadOnlyCollection<TaskItem>> ListByProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<TaskItem> result = taskItems.Values
                .Where(taskItem => taskItem.ProjectId == projectId)
                .ToArray();

            return Task.FromResult(result);
        }

        public Task AddAsync(
            TaskItem taskItem,
            CancellationToken cancellationToken)
        {
            taskItems[taskItem.Id] = taskItem;
            return Task.CompletedTask;
        }

        public void Remove(TaskItem taskItem)
        {
            taskItems.Remove(taskItem.Id);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
