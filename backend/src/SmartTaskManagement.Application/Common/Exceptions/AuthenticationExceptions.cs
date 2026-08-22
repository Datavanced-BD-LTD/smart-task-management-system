namespace SmartTaskManagement.Application.Common.Exceptions;

public abstract class AuthenticationException(string message) : Exception(message);

public sealed class DuplicateEmailException()
    : AuthenticationException("An account with this email already exists.");

public sealed class InvalidCredentialsException()
    : AuthenticationException("The email or password is invalid.");

public sealed class InvalidRefreshTokenException()
    : AuthenticationException("The refresh token is invalid or expired.");

public sealed class UserNotFoundException()
    : AuthenticationException("The authenticated user could not be found.");
