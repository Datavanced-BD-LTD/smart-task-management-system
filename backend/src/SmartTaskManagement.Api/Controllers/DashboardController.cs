using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Abstractions.Dashboard;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Dashboard;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(
    IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(
        [FromQuery] DashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(
            query,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            summary,
            "Dashboard summary retrieved successfully."));
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
