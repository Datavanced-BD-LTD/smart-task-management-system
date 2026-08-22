namespace SmartTaskManagement.Application.Common.Exceptions;

public sealed class TaskNotFoundException(Guid taskId)
    : Exception($"Task '{taskId}' was not found.");

public sealed class InvalidTaskAssigneeException()
    : Exception("The selected assignee is invalid or inactive.");

public sealed class TaskAssigneeNotProjectMemberException()
    : Exception("The selected assignee must be a member of the project.");
