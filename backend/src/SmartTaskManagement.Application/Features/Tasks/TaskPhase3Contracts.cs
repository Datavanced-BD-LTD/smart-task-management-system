using SmartTaskManagement.Domain.Enums;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Application.Features.Tasks;

public sealed record AssignTaskRequest(Guid? AssignedUserId);

public sealed record UpdateTaskStatusRequest(TaskStatusEnum Status);

public sealed record UpdateTaskPriorityRequest(TaskPriority Priority);
