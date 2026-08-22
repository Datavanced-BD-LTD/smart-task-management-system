using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Application.Features.Dashboard;
using SmartTaskManagement.Application.Features.Dashboard.Validators;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Enums;
using Xunit;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task Admin_dashboard_returns_all_statistics()
    {
        var store = new FakeDashboardStore
        {
            Aggregate = Aggregate(
                totalProjects: 4,
                totalTasks: 10,
                statuses: new Dictionary<TaskStatusEnum, int>
                {
                    [TaskStatusEnum.ToDo] = 2,
                    [TaskStatusEnum.InProgress] = 3,
                    [TaskStatusEnum.Completed] = 4,
                    [TaskStatusEnum.Cancelled] = 1
                },
                priorities: new Dictionary<TaskPriority, int>
                {
                    [TaskPriority.Low] = 1,
                    [TaskPriority.Medium] = 3,
                    [TaskPriority.High] = 4,
                    [TaskPriority.Critical] = 2
                },
                upcomingDueTaskCount: 3)
        };
        var service = CreateService(store);

        var result = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(4, result.TotalProjects);
        Assert.Equal(10, result.TotalTasks);
        Assert.Equal(4, result.CompletedTaskCount);
        Assert.Equal(5, result.PendingTaskCount);
        Assert.Equal(3, result.UpcomingDueTaskCount);
        Assert.Null(store.LastScope!.ProjectManagerId);
        Assert.Null(store.LastScope.MemberUserId);
        Assert.Null(store.LastScope.AssignedToUserId);
    }

    [Fact]
    public async Task Project_manager_dashboard_is_scoped_to_owned_projects()
    {
        var projectManagerId = Guid.NewGuid();
        var store = new FakeDashboardStore();
        var service = CreateService(store);

        await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            projectManagerId,
            [RoleNames.ProjectManager],
            CancellationToken.None);

        Assert.Equal(projectManagerId, store.LastScope!.ProjectManagerId);
        Assert.Null(store.LastScope.MemberUserId);
        Assert.Null(store.LastScope.AssignedToUserId);
    }

    [Fact]
    public async Task Team_member_dashboard_is_scoped_to_member_projects_and_assigned_tasks()
    {
        var teamMemberId = Guid.NewGuid();
        var store = new FakeDashboardStore();
        var service = CreateService(store);

        await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            teamMemberId,
            [RoleNames.TeamMember],
            CancellationToken.None);

        Assert.Null(store.LastScope!.ProjectManagerId);
        Assert.Equal(teamMemberId, store.LastScope.MemberUserId);
        Assert.Equal(teamMemberId, store.LastScope.AssignedToUserId);
    }

    [Fact]
    public async Task Status_counts_include_zero_buckets()
    {
        var store = new FakeDashboardStore
        {
            Aggregate = Aggregate(
                statuses: new Dictionary<TaskStatusEnum, int>
                {
                    [TaskStatusEnum.ToDo] = 2
                })
        };
        var service = CreateService(store);

        var result = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(4, result.TasksByStatus.Count);
        Assert.Equal(2, StatusCount(result, TaskStatusEnum.ToDo));
        Assert.Equal(0, StatusCount(result, TaskStatusEnum.InProgress));
        Assert.Equal(0, StatusCount(result, TaskStatusEnum.Completed));
        Assert.Equal(0, StatusCount(result, TaskStatusEnum.Cancelled));
    }

    [Fact]
    public async Task Priority_counts_include_zero_buckets()
    {
        var store = new FakeDashboardStore
        {
            Aggregate = Aggregate(
                priorities: new Dictionary<TaskPriority, int>
                {
                    [TaskPriority.High] = 3
                })
        };
        var service = CreateService(store);

        var result = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(4, result.TasksByPriority.Count);
        Assert.Equal(0, PriorityCount(result, TaskPriority.Low));
        Assert.Equal(0, PriorityCount(result, TaskPriority.Medium));
        Assert.Equal(3, PriorityCount(result, TaskPriority.High));
        Assert.Equal(0, PriorityCount(result, TaskPriority.Critical));
    }

    [Fact]
    public async Task Completed_and_pending_counts_are_derived_from_status_counts()
    {
        var store = new FakeDashboardStore
        {
            Aggregate = Aggregate(
                statuses: new Dictionary<TaskStatusEnum, int>
                {
                    [TaskStatusEnum.ToDo] = 3,
                    [TaskStatusEnum.InProgress] = 4,
                    [TaskStatusEnum.Completed] = 2,
                    [TaskStatusEnum.Cancelled] = 5
                })
        };
        var service = CreateService(store);

        var result = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(2, result.CompletedTaskCount);
        Assert.Equal(7, result.PendingTaskCount);
    }

    [Fact]
    public async Task Upcoming_due_task_window_defaults_to_seven_days_and_accepts_custom_range()
    {
        var store = new FakeDashboardStore
        {
            Aggregate = Aggregate(upcomingDueTaskCount: 2)
        };
        var service = CreateService(store);

        var defaultResult = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(7, store.LastUpcomingDays);
        Assert.Equal(2, defaultResult.UpcomingDueTaskCount);

        await service.GetSummaryAsync(
            new DashboardSummaryQuery(30),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(30, store.LastUpcomingDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public async Task Invalid_upcoming_days_are_rejected(int upcomingDays)
    {
        var service = CreateService(new FakeDashboardStore());

        await Assert.ThrowsAsync<ValidationException>(() => service.GetSummaryAsync(
            new DashboardSummaryQuery(upcomingDays),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None));
    }

    [Fact]
    public async Task Empty_dashboard_returns_zero_counts()
    {
        var service = CreateService(new FakeDashboardStore
        {
            Aggregate = Aggregate()
        });

        var result = await service.GetSummaryAsync(
            new DashboardSummaryQuery(),
            Guid.NewGuid(),
            [RoleNames.Admin],
            CancellationToken.None);

        Assert.Equal(0, result.TotalProjects);
        Assert.Equal(0, result.TotalTasks);
        Assert.Equal(0, result.CompletedTaskCount);
        Assert.Equal(0, result.PendingTaskCount);
        Assert.Equal(0, result.UpcomingDueTaskCount);
        Assert.All(result.TasksByStatus, item => Assert.Equal(0, item.Count));
        Assert.All(result.TasksByPriority, item => Assert.Equal(0, item.Count));
    }

    private static DashboardService CreateService(FakeDashboardStore store)
    {
        return new DashboardService(
            store,
            new FixedSystemClock(new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc)),
            new DashboardSummaryQueryValidator());
    }

    private static DashboardAggregate Aggregate(
        int totalProjects = 0,
        int totalTasks = 0,
        IReadOnlyDictionary<TaskStatusEnum, int>? statuses = null,
        IReadOnlyDictionary<TaskPriority, int>? priorities = null,
        int upcomingDueTaskCount = 0)
    {
        return new DashboardAggregate(
            totalProjects,
            totalTasks,
            statuses ?? new Dictionary<TaskStatusEnum, int>(),
            priorities ?? new Dictionary<TaskPriority, int>(),
            upcomingDueTaskCount);
    }

    private static int StatusCount(
        DashboardSummaryResponse response,
        TaskStatusEnum status)
    {
        return response.TasksByStatus.Single(item => item.Status == status).Count;
    }

    private static int PriorityCount(
        DashboardSummaryResponse response,
        TaskPriority priority)
    {
        return response.TasksByPriority.Single(item => item.Priority == priority).Count;
    }

    private sealed class FakeDashboardStore : IDashboardStore
    {
        public DashboardAggregate Aggregate { get; init; } = Aggregate();

        public DashboardScope? LastScope { get; private set; }

        public DateTime LastUtcNow { get; private set; }

        public int LastUpcomingDays { get; private set; }

        public Task<DashboardAggregate> GetSummaryAsync(
            DashboardScope scope,
            DateTime utcNow,
            int upcomingDays,
            CancellationToken cancellationToken)
        {
            LastScope = scope;
            LastUtcNow = utcNow;
            LastUpcomingDays = upcomingDays;
            return Task.FromResult(Aggregate);
        }
    }

    private sealed class FixedSystemClock(DateTime utcNow) : ISystemClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
