using SmartTaskManagement.Application.Features.Tasks;

namespace SmartTaskManagement.Application.Abstractions.Tasks;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TaskResponse>> ListByProjectAsync(
        Guid projectId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<TaskResponse> GetByIdAsync(
        Guid taskId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdateAsync(
        Guid taskId,
        UpdateTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<TaskResponse> AssignAsync(
        Guid taskId,
        AssignTaskRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdateStatusAsync(
        Guid taskId,
        UpdateTaskStatusRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task<TaskResponse> UpdatePriorityAsync(
        Guid taskId,
        UpdateTaskPriorityRequest request,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid taskId,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);
}
