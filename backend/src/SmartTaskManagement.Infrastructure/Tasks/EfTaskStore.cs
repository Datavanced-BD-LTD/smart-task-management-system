using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Tasks;
using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Infrastructure.Persistence;

namespace SmartTaskManagement.Infrastructure.Tasks;

public sealed class EfTaskStore(ApplicationDbContext dbContext) : ITaskStore
{
    public Task<Project?> FindProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return dbContext.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                project => project.ProjectId == projectId && !project.IsDeleted,
                cancellationToken);
    }

    public Task<bool> IsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
    }

    public Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.UserId == userId && user.IsActive,
                cancellationToken);
    }

    public Task<TaskItem?> FindByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        // Read-only lookups avoid change-tracking overhead. Mutation lookups below
        // intentionally remain tracked so SaveChanges can persist domain updates.
        return dbContext.TaskItems
            .AsNoTracking()
            .SingleOrDefaultAsync(taskItem => taskItem.Id == taskId, cancellationToken);
    }

    public Task<TaskItem?> FindByIdForUpdateAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return dbContext.TaskItems
            .SingleOrDefaultAsync(taskItem => taskItem.Id == taskId, cancellationToken);
    }

    public Task<TaskResponse?> FindResponseByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return dbContext.TaskItems
            .AsNoTracking()
            .Where(taskItem => taskItem.Id == taskId)
            .Select(taskItem => new TaskResponse(
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
                taskItem.UpdatedAtUtc,
                taskItem.AssignedToUser == null
                    ? null
                    : taskItem.AssignedToUser.FirstName + " " + taskItem.AssignedToUser.LastName,
                taskItem.AssignedToUser == null
                    ? null
                    : taskItem.AssignedToUser.Email,
                taskItem.CreatedByUser == null
                    ? null
                    : taskItem.CreatedByUser.FirstName + " " + taskItem.CreatedByUser.LastName,
                taskItem.CreatedByUser == null
                    ? null
                    : taskItem.CreatedByUser.Email,
                taskItem.Project == null ? null : taskItem.Project.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<TaskResponse>> ListByProjectAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken)
    {
        // Keep this as IQueryable: EF composes filters, sorting, projection, and
        // pagination into SQL instead of loading all tasks into application memory.
        var taskItems = dbContext.TaskItems
            .AsNoTracking()
            .Where(taskItem => taskItem.ProjectId == projectId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLowerInvariant();
            var pattern = $"%{keyword}%";

            taskItems = taskItems.Where(taskItem =>
                EF.Functions.Like(taskItem.Title.ToLower(), pattern) ||
                (taskItem.Description != null &&
                 EF.Functions.Like(taskItem.Description.ToLower(), pattern)));
        }

        if (query.Status.HasValue)
        {
            taskItems = taskItems.Where(taskItem => taskItem.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            taskItems = taskItems.Where(taskItem => taskItem.Priority == query.Priority.Value);
        }

        if (query.AssignedUserId.HasValue)
        {
            taskItems = taskItems.Where(taskItem =>
                taskItem.AssignedToUserId == query.AssignedUserId.Value);
        }

        if (query.DueDateFrom.HasValue)
        {
            taskItems = taskItems.Where(taskItem =>
                taskItem.DueDate >= query.DueDateFrom.Value);
        }

        if (query.DueDateTo.HasValue)
        {
            taskItems = taskItems.Where(taskItem =>
                taskItem.DueDate <= query.DueDateTo.Value);
        }

        var isDescending = string.Equals(
            query.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        // Sort columns are validated against a whitelist before reaching this store.
        // Id is a stable tie-breaker so records do not jump between result pages.
        taskItems = query.SortColumn.ToLowerInvariant() switch
        {
            "title" => isDescending
                ? taskItems.OrderByDescending(taskItem => taskItem.Title)
                    .ThenBy(taskItem => taskItem.Id)
                : taskItems.OrderBy(taskItem => taskItem.Title)
                    .ThenBy(taskItem => taskItem.Id),
            "status" => isDescending
                ? taskItems.OrderByDescending(taskItem => taskItem.Status)
                    .ThenBy(taskItem => taskItem.Id)
                : taskItems.OrderBy(taskItem => taskItem.Status)
                    .ThenBy(taskItem => taskItem.Id),
            "priority" => isDescending
                ? taskItems.OrderByDescending(taskItem => taskItem.Priority)
                    .ThenBy(taskItem => taskItem.Id)
                : taskItems.OrderBy(taskItem => taskItem.Priority)
                    .ThenBy(taskItem => taskItem.Id),
            "duedate" => isDescending
                ? taskItems.OrderByDescending(taskItem => taskItem.DueDate)
                    .ThenBy(taskItem => taskItem.Id)
                : taskItems.OrderBy(taskItem => taskItem.DueDate)
                    .ThenBy(taskItem => taskItem.Id),
            _ => isDescending
                ? taskItems.OrderByDescending(taskItem => taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id)
                : taskItems.OrderBy(taskItem => taskItem.CreatedAtUtc)
                    .ThenBy(taskItem => taskItem.Id)
        };

        // Count the filtered query before applying Skip/Take so pagination metadata
        // represents the complete filtered result set.
        var totalCount = await taskItems.CountAsync(cancellationToken);
        var skip = (long)(query.PageNumber - 1) * query.PageSize;
        var projectedTasks = taskItems.Select(taskItem => new TaskResponse(
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
            taskItem.UpdatedAtUtc,
            taskItem.AssignedToUser == null
                ? null
                : taskItem.AssignedToUser.FirstName + " " + taskItem.AssignedToUser.LastName,
            taskItem.AssignedToUser == null
                ? null
                : taskItem.AssignedToUser.Email,
            taskItem.CreatedByUser == null
                ? null
                : taskItem.CreatedByUser.FirstName + " " + taskItem.CreatedByUser.LastName,
            taskItem.CreatedByUser == null
                ? null
                : taskItem.CreatedByUser.Email,
            taskItem.Project == null ? null : taskItem.Project.Name));

        IReadOnlyCollection<TaskResponse> items = skip > int.MaxValue
            ? []
            : await projectedTasks
                .Skip((int)skip)
                .Take(query.PageSize)
                .ToArrayAsync(cancellationToken);

        return new PagedResult<TaskResponse>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }

    public async Task AddAsync(
        TaskItem taskItem,
        CancellationToken cancellationToken)
    {
        await dbContext.TaskItems.AddAsync(taskItem, cancellationToken);
    }

    public void Remove(TaskItem taskItem)
    {
        dbContext.TaskItems.Remove(taskItem);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
