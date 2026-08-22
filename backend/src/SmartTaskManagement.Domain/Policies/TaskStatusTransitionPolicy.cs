using SmartTaskManagement.Domain.Exceptions;
using TaskStatusEnum = SmartTaskManagement.Domain.Enums.TaskStatus;

namespace SmartTaskManagement.Domain.Policies;

/// <summary>
/// Defines the allowed Phase 3 task status workflow:
/// same-status updates are allowed; ToDo can move to InProgress or Cancelled;
/// InProgress can move to Completed or Cancelled; Completed and Cancelled are terminal.
/// </summary>
public static class TaskStatusTransitionPolicy
{
    public static void EnsureAllowed(
        TaskStatusEnum currentStatus,
        TaskStatusEnum requestedStatus)
    {
        if (currentStatus == requestedStatus || IsAllowed(currentStatus, requestedStatus))
        {
            return;
        }

        throw new InvalidTaskStatusTransitionException(currentStatus, requestedStatus);
    }

    private static bool IsAllowed(
        TaskStatusEnum currentStatus,
        TaskStatusEnum requestedStatus)
    {
        return currentStatus switch
        {
            TaskStatusEnum.ToDo => requestedStatus is
                TaskStatusEnum.InProgress or TaskStatusEnum.Cancelled,
            TaskStatusEnum.InProgress => requestedStatus is
                TaskStatusEnum.Completed or TaskStatusEnum.Cancelled,
            TaskStatusEnum.Completed or TaskStatusEnum.Cancelled => false,
            _ => false
        };
    }
}
