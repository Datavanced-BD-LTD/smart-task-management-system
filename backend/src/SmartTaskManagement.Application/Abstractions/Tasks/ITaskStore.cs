using SmartTaskManagement.Domain.Entities;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Tasks;

namespace SmartTaskManagement.Application.Abstractions.Tasks;

public interface ITaskStore
{
    Task<Project?> FindProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<bool> IsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> IsActiveUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<TaskItem?> FindByIdAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<TaskItem?> FindByIdForUpdateAsync(
        Guid taskId,
        CancellationToken cancellationToken);

    Task<PagedResult<TaskResponse>> ListByProjectAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(
        TaskItem taskItem,
        CancellationToken cancellationToken);

    void Remove(TaskItem taskItem);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
