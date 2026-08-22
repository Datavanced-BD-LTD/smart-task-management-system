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

public sealed record TaskListQuery(
    string? Keyword = null,
    TaskStatusEnum? Status = null,
    TaskPriority? Priority = null,
    Guid? AssignedUserId = null,
    DateTime? DueDateFrom = null,
    DateTime? DueDateTo = null,
    int PageNumber = 1,
    int PageSize = 10,
    string SortColumn = "createdAt",
    string SortDirection = "desc");

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
