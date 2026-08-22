using SmartTaskManagement.Domain.Enums;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Application.Features.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? AssignedToUserId,
    TaskStatusEnum Status,
    TaskPriority Priority,
    DateTime? DueDate);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? AssignedToUserId,
    TaskStatusEnum Status,
    TaskPriority Priority,
    DateTime? DueDate);

public sealed record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    Guid? AssignedToUserId,
    Guid CreatedByUserId,
    TaskStatusEnum Status,
    TaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
