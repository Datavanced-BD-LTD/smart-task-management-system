using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Api.Models;

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(
        HttpContext httpContext,
        T data,
        string message)
    {
        // Every successful endpoint gets the same trace identifier shape, allowing
        // frontend code and support logs to correlate a response consistently.
        return new ApiResponse<T>(
            true,
            message,
            data,
            null,
            httpContext.TraceIdentifier);
    }

    public static ApiResponse<T> Failure<T>(
        HttpContext httpContext,
        string message,
        IReadOnlyCollection<ApiError> errors)
    {
        // Keep errors structured and transport-independent so controllers, middleware,
        // authentication handlers, and rate limiting share one response contract.
        return new ApiResponse<T>(
            false,
            message,
            default,
            errors,
            httpContext.TraceIdentifier);
    }
}
