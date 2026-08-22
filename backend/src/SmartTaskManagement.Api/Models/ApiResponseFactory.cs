using SmartTaskManagement.Application.Common.Models;

namespace SmartTaskManagement.Api.Models;

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(
        HttpContext httpContext,
        T data,
        string message)
    {
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
        return new ApiResponse<T>(
            false,
            message,
            default,
            errors,
            httpContext.TraceIdentifier);
    }
}
