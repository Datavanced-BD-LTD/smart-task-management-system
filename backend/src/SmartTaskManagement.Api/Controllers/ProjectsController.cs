using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController(
    ProjectService projectService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { projectId = project.ProjectId },
            ApiResponseFactory.Success(HttpContext, project, "Project created successfully."));
    }

    [HttpPut("{projectId:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> Update(
        Guid projectId,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.UpdateAsync(
            projectId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, project, "Project updated successfully."));
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await projectService.DeleteAsync(
            projectId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success<object?>(
            HttpContext,
            null,
            "Project deleted successfully."));
    }

    [HttpGet("{projectId:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetById(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(
            projectId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, project, "Project retrieved successfully."));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ProjectResponse>>>> List(
        [FromQuery] ProjectListQuery query,
        CancellationToken cancellationToken)
    {
        var projects = await projectService.ListAsync(
            query,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, projects, "Projects retrieved successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("The access token does not contain a valid user ID.");
        }

        return userId;
    }

    private IReadOnlyCollection<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
