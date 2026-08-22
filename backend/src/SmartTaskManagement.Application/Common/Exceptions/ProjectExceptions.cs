namespace SmartTaskManagement.Application.Common.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message);

public sealed class ProjectNotFoundException(Guid projectId)
    : Exception($"Project '{projectId}' was not found.");

public sealed class InvalidProjectManagerException()
    : Exception("The selected project manager is invalid or inactive.");

public sealed class InvalidProjectMemberException()
    : Exception("The selected project member is invalid, inactive, or is not a Team Member.");

public sealed class ProjectMemberAlreadyExistsException()
    : Exception("The selected user is already a member of this project.");

public sealed class ProjectMemberNotFoundException(Guid projectId, Guid userId)
    : Exception($"User '{userId}' is not a member of project '{projectId}'.");
