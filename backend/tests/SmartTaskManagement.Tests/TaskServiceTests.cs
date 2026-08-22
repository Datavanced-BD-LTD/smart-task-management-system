using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
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
    public async Task Admin_can_fully_update_a_task()
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

        var result = await service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Updated title",
                "Updated description",
                assigneeId,
                TaskStatusEnum.InProgress,
                TaskPriority.High,
                new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc)),
            adminId,
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal("Updated title", result.Title);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(assigneeId, result.AssignedToUserId);
        Assert.Equal(TaskStatusEnum.InProgress, result.Status);
        Assert.Equal(TaskPriority.High, result.Priority);
        Assert.Equal(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), result.DueDate);
    }

    [Fact]
    public async Task Authorized_project_manager_can_fully_update_a_task()
    {
        var projectManagerId = Guid.NewGuid();
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

        var result = await service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Manager updated title",
                "Manager updated description",
                assigneeId,
                TaskStatusEnum.InProgress,
                TaskPriority.Critical,
                new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc)),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Equal("Manager updated title", result.Title);
        Assert.Equal("Manager updated description", result.Description);
        Assert.Equal(assigneeId, result.AssignedToUserId);
        Assert.Equal(TaskStatusEnum.InProgress, result.Status);
        Assert.Equal(TaskPriority.Critical, result.Priority);
        Assert.Equal(new DateTime(2026, 2, 2, 12, 0, 0, DateTimeKind.Utc), result.DueDate);
    }

    [Fact]
    public async Task Assigned_team_member_cannot_use_full_task_update_endpoint()
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

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Changed title",
                "Changed description",
                teamMemberId,
                TaskStatusEnum.InProgress,
                TaskPriority.High,
                DateTime.UtcNow.AddDays(10)),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));

        Assert.Contains("full task update endpoint", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Team_member_cannot_change_priority_through_full_task_update()
    {
        var (service, task, teamMemberId) = await CreateAssignedTeamMemberTask();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                task.Title,
                task.Description,
                task.AssignedToUserId,
                task.Status,
                TaskPriority.Critical,
                task.DueDate),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_cannot_change_due_date_through_full_task_update()
    {
        var (service, task, teamMemberId) = await CreateAssignedTeamMemberTask();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                task.Title,
                task.Description,
                task.AssignedToUserId,
                task.Status,
                task.Priority,
                DateTime.UtcNow.AddDays(30)),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_cannot_change_title_or_description_through_full_task_update()
    {
        var (service, task, teamMemberId) = await CreateAssignedTeamMemberTask();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Changed title",
                "Changed description",
                task.AssignedToUserId,
                task.Status,
                task.Priority,
                task.DueDate),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Unauthorized_project_manager_receives_forbidden_for_full_task_update()
    {
        var projectManagerId = Guid.NewGuid();
        var unauthorizedManagerId = Guid.NewGuid();
        var project = CreateProject(projectManagerId);
        var store = CreateStore(project);
        var service = CreateService(store);
        var task = await service.CreateAsync(
            project.ProjectId,
            CreateRequest(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(
            task.Id,
            new UpdateTaskRequest(
                "Changed title",
                task.Description,
                task.AssignedToUserId,
                task.Status,
                task.Priority,
                task.DueDate),
            unauthorizedManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));

        Assert.Contains("projects they manage", exception.Message, StringComparison.Ordinal);
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

    [Fact]
    public async Task Keyword_search_matches_title_and_description_case_insensitively()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Fix Login", description: "Authentication issue"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Update Dashboard", description: "Refresh metrics"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(Keyword: "LOGIN"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Fix Login", result.Items.Single().Title);
    }

    [Fact]
    public async Task Status_filter_returns_matching_tasks()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Todo task", status: TaskStatusEnum.ToDo),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Active task", status: TaskStatusEnum.InProgress),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(Status: TaskStatusEnum.InProgress),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(TaskStatusEnum.InProgress, result.Items.Single().Status);
    }

    [Fact]
    public async Task Priority_filter_returns_matching_tasks()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Low task", priority: TaskPriority.Low),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Critical task", priority: TaskPriority.Critical),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(Priority: TaskPriority.Critical),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(TaskPriority.Critical, result.Items.Single().Priority);
    }

    [Fact]
    public async Task Assignee_filter_returns_matching_tasks()
    {
        var managerId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var otherAssigneeId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project, assigneeId, project.ProjectId);
        store.AddActiveUser(otherAssigneeId);
        store.AddMember(project.ProjectId, otherAssigneeId);
        var service = CreateService(store);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Assigned task", assignedToUserId: assigneeId),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Other task", assignedToUserId: otherAssigneeId),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(AssignedUserId: assigneeId),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(assigneeId, result.Items.Single().AssignedToUserId);
    }

    [Fact]
    public async Task Due_date_filter_returns_tasks_in_range()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);
        var rangeStart = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Inside", dueDate: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Outside", dueDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(DueDateFrom: rangeStart, DueDateTo: rangeEnd),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Inside", result.Items.Single().Title);
    }

    [Fact]
    public async Task Sorting_supports_ascending_and_descending_order()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Alpha"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        await service.CreateAsync(
            project.ProjectId,
            CreateRequest(title: "Zulu"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        var ascending = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(SortColumn: "title", SortDirection: "asc"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);
        var descending = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(SortColumn: "title", SortDirection: "desc"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Equal("Alpha", ascending.Items.First().Title);
        Assert.Equal("Zulu", descending.Items.First().Title);
    }

    [Fact]
    public async Task Pagination_returns_items_and_metadata()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project);
        var service = CreateService(store);

        for (var index = 1; index <= 3; index++)
        {
            await service.CreateAsync(
                project.ProjectId,
                CreateRequest(title: $"Task {index}"),
                managerId,
                [RoleNames.ProjectManager],
                CancellationToken.None);
        }

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(PageNumber: 2, PageSize: 2),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task Page_size_above_maximum_is_rejected()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var service = CreateService(CreateStore(project));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(PageSize: 101),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_sort_column_is_rejected()
    {
        var managerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var service = CreateService(CreateStore(project));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(SortColumn: "unsafeColumn"),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None));
    }

    [Fact]
    public async Task Team_member_can_list_only_member_projects()
    {
        var managerId = Guid.NewGuid();
        var teamMemberId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var store = CreateStore(project, teamMemberId, project.ProjectId);
        var service = CreateService(store);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None);

        Assert.Empty(result.Items);

        var otherProject = CreateProject(Guid.NewGuid());
        store.AddProject(otherProject);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ListByProjectAsync(
            otherProject.ProjectId,
            new TaskListQuery(),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None));
    }

    [Fact]
    public async Task Project_manager_can_list_only_owned_projects()
    {
        var managerId = Guid.NewGuid();
        var otherManagerId = Guid.NewGuid();
        var project = CreateProject(managerId);
        var otherProject = CreateProject(otherManagerId);
        var store = CreateStore(project);
        store.AddProject(otherProject);
        var service = CreateService(store);

        var result = await service.ListByProjectAsync(
            project.ProjectId,
            new TaskListQuery(),
            managerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Empty(result.Items);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ListByProjectAsync(
            otherProject.ProjectId,
            new TaskListQuery(),
            managerId,
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
            new UpdateTaskPriorityRequestValidator(),
            new TaskListQueryValidator());
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

    private static CreateTaskRequest CreateRequest(
        Guid? assignedToUserId = null,
        TaskStatusEnum status = TaskStatusEnum.ToDo,
        TaskPriority priority = TaskPriority.Medium,
        DateTime? dueDate = null,
        string title = "Test Task",
        string? description = "Task description")
    {
        return new CreateTaskRequest(
            title,
            description,
            assignedToUserId,
            status,
            priority,
            dueDate ?? DateTime.UtcNow.AddDays(1));
    }

    private static async Task<(TaskService Service, TaskResponse Task, Guid TeamMemberId)>
        CreateAssignedTeamMemberTask()
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

        return (service, task, teamMemberId);
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

        public Task<TaskResponse?> FindResponseByIdAsync(
            Guid taskId,
            CancellationToken cancellationToken)
        {
            taskItems.TryGetValue(taskId, out var taskItem);
            return Task.FromResult(taskItem is null ? null : ToResponse(taskItem));
        }

        public Task<PagedResult<TaskResponse>> ListByProjectAsync(
            Guid projectId,
            TaskListQuery query,
            CancellationToken cancellationToken)
        {
            var tasks = taskItems.Values
                .Where(taskItem => taskItem.ProjectId == projectId)
                .Select(ToResponse)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var keyword = query.Keyword.Trim();
                tasks = tasks.Where(task =>
                    task.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (task.Description is not null &&
                     task.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            }

            if (query.Status.HasValue)
            {
                tasks = tasks.Where(task => task.Status == query.Status.Value);
            }

            if (query.Priority.HasValue)
            {
                tasks = tasks.Where(task => task.Priority == query.Priority.Value);
            }

            if (query.AssignedUserId.HasValue)
            {
                tasks = tasks.Where(task => task.AssignedToUserId == query.AssignedUserId.Value);
            }

            if (query.DueDateFrom.HasValue)
            {
                tasks = tasks.Where(task => task.DueDate >= query.DueDateFrom.Value);
            }

            if (query.DueDateTo.HasValue)
            {
                tasks = tasks.Where(task => task.DueDate <= query.DueDateTo.Value);
            }

            var isDescending = string.Equals(
                query.SortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            tasks = query.SortColumn.ToLowerInvariant() switch
            {
                "title" => isDescending
                    ? tasks.OrderByDescending(task => task.Title).ThenBy(task => task.Id)
                    : tasks.OrderBy(task => task.Title).ThenBy(task => task.Id),
                "status" => isDescending
                    ? tasks.OrderByDescending(task => task.Status).ThenBy(task => task.Id)
                    : tasks.OrderBy(task => task.Status).ThenBy(task => task.Id),
                "priority" => isDescending
                    ? tasks.OrderByDescending(task => task.Priority).ThenBy(task => task.Id)
                    : tasks.OrderBy(task => task.Priority).ThenBy(task => task.Id),
                "duedate" => isDescending
                    ? tasks.OrderByDescending(task => task.DueDate).ThenBy(task => task.Id)
                    : tasks.OrderBy(task => task.DueDate).ThenBy(task => task.Id),
                _ => isDescending
                    ? tasks.OrderByDescending(task => task.CreatedAtUtc).ThenBy(task => task.Id)
                    : tasks.OrderBy(task => task.CreatedAtUtc).ThenBy(task => task.Id)
            };

            var allTasks = tasks.ToArray();
            var skip = (long)(query.PageNumber - 1) * query.PageSize;
            IReadOnlyCollection<TaskResponse> items = skip > int.MaxValue
                ? []
                : allTasks.Skip((int)skip).Take(query.PageSize).ToArray();

            return Task.FromResult(new PagedResult<TaskResponse>(
                items,
                query.PageNumber,
                query.PageSize,
                allTasks.Length));
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

        private static TaskResponse ToResponse(TaskItem taskItem)
        {
            return new TaskResponse(
                taskItem.Id,
                taskItem.ProjectId,
                taskItem.Title,
                taskItem.Description,
                taskItem.AssignedToUserId,
                taskItem.CreatedByUserId,
                taskItem.Status,
                taskItem.Priority,
                taskItem.DueDate,
                taskItem.CreatedAtUtc,
                taskItem.UpdatedAtUtc);
        }
    }
}
