using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.UserManagement;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController(
    UserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<ManagedUserResponse>>>> List(
        [FromQuery] AdminUserListQuery query,
        CancellationToken cancellationToken)
    {
        var users = await userManagementService.ListAsync(
            query,
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            users,
            "Users retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ManagedUserResponse>>> Create(
        CreateManagedUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManagementService.CreateAsync(
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponseFactory.Success(
                HttpContext,
                user,
                "User created successfully."));
    }

    [HttpPatch("{userId:guid}/role")]
    public async Task<ActionResult<ApiResponse<ManagedUserResponse>>> UpdateRole(
        Guid userId,
        UpdateManagedUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManagementService.UpdateRoleAsync(
            userId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            user,
            "User role updated successfully."));
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
