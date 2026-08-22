using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Domain.Exceptions;

public sealed class InvalidTaskStatusTransitionException(
    TaskStatusEnum currentStatus,
    TaskStatusEnum requestedStatus)
    : Exception(
        $"Task status cannot transition from '{currentStatus}' to '{requestedStatus}'.");
