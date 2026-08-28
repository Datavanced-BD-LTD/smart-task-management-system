namespace SmartTaskManagement.Application.Common.Exceptions;

public sealed class ManagedUserNotFoundException(Guid userId)
    : Exception($"The user '{userId}' could not be found.");

public sealed class ProtectedUserException()
    : Exception("The Admin account cannot be changed through this operation.");

public sealed class InvalidManagedUserRoleException()
    : Exception("The requested user role is not supported.");
