using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Application.Features.Dashboard;
using SmartTaskManagement.Infrastructure.Persistence;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Infrastructure.Dashboard;

public sealed class EfDashboardStore(ApplicationDbContext dbContext) : IDashboardStore
{
    public async Task<DashboardAggregate> GetSummaryAsync(
        DashboardScope scope,
        DateTime utcNow,
        int upcomingDays,
        CancellationToken cancellationToken)
    {
        var projects = dbContext.Projects
            .AsNoTracking()
            .Where(project => !project.IsDeleted);

        if (scope.ProjectManagerId.HasValue)
        {
            projects = projects.Where(project =>
                project.ProjectManagerId == scope.ProjectManagerId.Value);
        }

        if (scope.MemberUserId.HasValue)
        {
            projects = projects.Where(project =>
                project.ProjectMembers.Any(member =>
                    member.UserId == scope.MemberUserId.Value));
        }

        var tasks = dbContext.TaskItems
            .AsNoTracking()
            .Where(taskItem =>
                taskItem.Project != null &&
                !taskItem.Project.IsDeleted);

        if (scope.ProjectManagerId.HasValue)
        {
            tasks = tasks.Where(taskItem =>
                taskItem.Project!.ProjectManagerId == scope.ProjectManagerId.Value);
        }

        if (scope.MemberUserId.HasValue)
        {
            tasks = tasks.Where(taskItem =>
                taskItem.Project!.ProjectMembers.Any(member =>
                    member.UserId == scope.MemberUserId.Value));
        }

        if (scope.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(taskItem =>
                taskItem.AssignedToUserId == scope.AssignedToUserId.Value);
        }

        var totalProjects = await projects.CountAsync(cancellationToken);
        var totalTasks = await tasks.CountAsync(cancellationToken);
        var statusCounts = await tasks
            .GroupBy(taskItem => taskItem.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.Status,
                item => item.Count,
                cancellationToken);
        var priorityCounts = await tasks
            .GroupBy(taskItem => taskItem.Priority)
            .Select(group => new
            {
                Priority = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => item.Priority,
                item => item.Count,
                cancellationToken);

        var upcomingEnd = utcNow.AddDays(upcomingDays);
        var upcomingDueTaskCount = await tasks
            .Where(taskItem =>
                (taskItem.Status == TaskStatusEnum.ToDo ||
                 taskItem.Status == TaskStatusEnum.InProgress) &&
                taskItem.DueDate.HasValue &&
                taskItem.DueDate.Value >= utcNow &&
                taskItem.DueDate.Value <= upcomingEnd)
            .CountAsync(cancellationToken);

        return new DashboardAggregate(
            totalProjects,
            totalTasks,
            statusCounts,
            priorityCounts,
            upcomingDueTaskCount);
    }
}
