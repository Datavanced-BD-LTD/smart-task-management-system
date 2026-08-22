using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Abstractions.Ai;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Ai;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public sealed class AiController(
    IAiTaskDescriptionService aiTaskDescriptionService) : ControllerBase
{
    [HttpPost("improve-task-description")]
    public async Task<ActionResult<ApiResponse<ImproveTaskDescriptionResponse>>> ImproveTaskDescription(
        ImproveTaskDescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await aiTaskDescriptionService.ImproveAsync(
            request,
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            response,
            "Task description improved successfully."));
    }
}
