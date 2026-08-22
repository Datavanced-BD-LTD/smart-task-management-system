using SmartTaskManagement.Domain.Enums;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Application.Features.Dashboard;

public sealed record DashboardSummaryQuery(int UpcomingDays = 7);

public sealed record DashboardStatusCount(
    TaskStatusEnum Status,
    int Count);

public sealed record DashboardPriorityCount(
    TaskPriority Priority,
    int Count);

public sealed record DashboardSummaryResponse(
    int TotalProjects,
    int TotalTasks,
    IReadOnlyCollection<DashboardStatusCount> TasksByStatus,
    IReadOnlyCollection<DashboardPriorityCount> TasksByPriority,
    int CompletedTaskCount,
    int PendingTaskCount,
    int UpcomingDueTaskCount);

public sealed record DashboardAggregate(
    int TotalProjects,
    int TotalTasks,
    IReadOnlyDictionary<TaskStatusEnum, int> TasksByStatus,
    IReadOnlyDictionary<TaskPriority, int> TasksByPriority,
    int UpcomingDueTaskCount);
