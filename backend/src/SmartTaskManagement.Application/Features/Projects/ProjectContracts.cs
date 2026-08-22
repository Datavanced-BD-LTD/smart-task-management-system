namespace SmartTaskManagement.Application.Features.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? Description,
    Guid? ProjectManagerId = null);

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    Guid? ProjectManagerId = null);

public sealed record AddProjectMemberRequest(Guid UserId);

public sealed record ProjectMemberResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    Guid AddedByUserId,
    DateTime AddedAtUtc);

public sealed record ProjectListQuery(
    string? Search = null,
    string SortBy = "createdAt",
    string SortDirection = "desc",
    int Page = 1,
    int PageSize = 20);

public sealed record ProjectResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    Guid ProjectManagerId,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
