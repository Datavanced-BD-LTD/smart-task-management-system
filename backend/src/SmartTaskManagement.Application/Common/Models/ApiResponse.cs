namespace SmartTaskManagement.Application.Common.Models;

public sealed record ApiError(
    string Code,
    string Message,
    string? Field = null);

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyCollection<ApiError>? Errors,
    string TraceId);
