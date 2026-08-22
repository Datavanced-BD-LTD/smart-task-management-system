using FluentValidation;
using SmartTaskManagement.Application.Abstractions.Common;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Domain.Constants;
using SmartTaskManagement.Domain.Enums;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Application.Features.Dashboard;

public sealed class DashboardService(
    IDashboardStore dashboardStore,
    ISystemClock systemClock,
    IValidator<DashboardSummaryQuery> queryValidator) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardSummaryQuery query,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(queryValidator, query, cancellationToken);

        var scope = ResolveScope(currentUserId, roles);
        var aggregate = await dashboardStore.GetSummaryAsync(
            scope,
            systemClock.UtcNow,
            query.UpcomingDays,
            cancellationToken);

        var tasksByStatus = Enum.GetValues<TaskStatusEnum>()
            .Select(status => new DashboardStatusCount(
                status,
                aggregate.TasksByStatus.GetValueOrDefault(status)))
            .ToArray();
        var tasksByPriority = Enum.GetValues<TaskPriority>()
            .Select(priority => new DashboardPriorityCount(
                priority,
                aggregate.TasksByPriority.GetValueOrDefault(priority)))
            .ToArray();

        var completedTaskCount = aggregate.TasksByStatus.GetValueOrDefault(
            TaskStatusEnum.Completed);
        var pendingTaskCount = aggregate.TasksByStatus.GetValueOrDefault(TaskStatusEnum.ToDo) +
            aggregate.TasksByStatus.GetValueOrDefault(TaskStatusEnum.InProgress);

        return new DashboardSummaryResponse(
            aggregate.TotalProjects,
            aggregate.TotalTasks,
            tasksByStatus,
            tasksByPriority,
            completedTaskCount,
            pendingTaskCount,
            aggregate.UpcomingDueTaskCount);
    }

    private static DashboardScope ResolveScope(
        Guid currentUserId,
        IReadOnlyCollection<string> roles)
    {
        if (roles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return new DashboardScope(null, null, null);
        }

        if (roles.Contains(RoleNames.ProjectManager, StringComparer.OrdinalIgnoreCase))
        {
            return new DashboardScope(currentUserId, null, null);
        }

        if (roles.Contains(RoleNames.TeamMember, StringComparer.OrdinalIgnoreCase))
        {
            return new DashboardScope(null, currentUserId, currentUserId);
        }

        throw new ForbiddenException("The current role cannot access the dashboard.");
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
