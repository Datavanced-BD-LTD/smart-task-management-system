using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Projects;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/members")]
[Authorize]
public sealed class ProjectMembersController(
    ProjectMembershipService membershipService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProjectMemberResponse>>>> List(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var members = await membershipService.ListAsync(
            projectId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            members,
            "Project members retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectMemberResponse>>> Add(
        Guid projectId,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var member = await membershipService.AddAsync(
            projectId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFactory.Success(
                HttpContext,
                member,
                "Project member added successfully."));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Remove(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await membershipService.RemoveAsync(
            projectId,
            userId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success<object?>(
            HttpContext,
            null,
            "Project member removed successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException(
                "The access token does not contain a valid user ID.");
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
