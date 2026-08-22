using Microsoft.EntityFrameworkCore;
using SmartTaskManagement.Application.Abstractions.Tasks;
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

    public async Task<IReadOnlyCollection<TaskItem>> ListByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(taskItem => taskItem.ProjectId == projectId)
            .OrderBy(taskItem => taskItem.CreatedAtUtc)
            .ThenBy(taskItem => taskItem.Id)
            .ToArrayAsync(cancellationToken);
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
