using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Common.Exceptions;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Domain.Exceptions;

namespace SmartTaskManagement.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception for {RequestMethod} {RequestPath}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            traceId);

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(error => new ApiError(
                    "VALIDATION_ERROR",
                    error.ErrorMessage,
                    error.PropertyName))
                .ToArray();
            var response = ApiResponseFactory.Failure<object?>(
                httpContext,
                "One or more validation errors occurred.",
                errors);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }

        var statusCode = exception switch
        {
            DuplicateEmailException => StatusCodes.Status409Conflict,
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            InvalidRefreshTokenException => StatusCodes.Status401Unauthorized,
            UserNotFoundException => StatusCodes.Status401Unauthorized,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ProjectNotFoundException => StatusCodes.Status404NotFound,
            TaskNotFoundException => StatusCodes.Status404NotFound,
            ProjectMemberNotFoundException => StatusCodes.Status404NotFound,
            InvalidProjectManagerException => StatusCodes.Status400BadRequest,
            InvalidProjectMemberException => StatusCodes.Status400BadRequest,
            InvalidTaskAssigneeException => StatusCodes.Status400BadRequest,
            TaskAssigneeNotProjectMemberException => StatusCodes.Status400BadRequest,
            InvalidTaskStatusTransitionException => StatusCodes.Status400BadRequest,
            ProjectMemberAlreadyExistsException => StatusCodes.Status409Conflict,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var message = statusCode switch
        {
            StatusCodes.Status401Unauthorized => "Authentication failed.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status400BadRequest => "The request is invalid.",
            StatusCodes.Status409Conflict => "The request conflicts with existing data.",
            _ => "An unexpected error occurred."
        };

        var errorMessage = exception is AuthenticationException or
            InvalidTaskAssigneeException or
            TaskAssigneeNotProjectMemberException
            or InvalidTaskStatusTransitionException
            ? exception.Message
            : message;
        var errorCode = statusCode switch
        {
            StatusCodes.Status401Unauthorized => "AUTHENTICATION_FAILED",
            StatusCodes.Status403Forbidden => "FORBIDDEN",
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status400BadRequest => "INVALID_REQUEST",
            StatusCodes.Status409Conflict => "CONFLICT",
            _ => "INTERNAL_SERVER_ERROR"
        };
        var failureResponse = ApiResponseFactory.Failure<object?>(
            httpContext,
            message,
            [new ApiError(errorCode, errorMessage)]);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(failureResponse, cancellationToken);

        return true;
    }
}
