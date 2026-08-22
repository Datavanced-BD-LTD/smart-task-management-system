using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTaskManagement.Api.Models;
using SmartTaskManagement.Application.Abstractions.Tasks;
using SmartTaskManagement.Application.Common.Models;
using SmartTaskManagement.Application.Features.Tasks;

namespace SmartTaskManagement.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class TasksController(
    ITaskService taskService) : ControllerBase
{
    [HttpPost("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Create(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(
            projectId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { taskId = task.Id },
            ApiResponseFactory.Success(HttpContext, task, "Task created successfully."));
    }

    [HttpGet("projects/{projectId:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskResponse>>>> ListByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var tasks = await taskService.ListByProjectAsync(
            projectId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(
            HttpContext,
            tasks,
            "Tasks retrieved successfully."));
    }

    [HttpGet("tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetById(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(
            taskId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, task, "Task retrieved successfully."));
    }

    [HttpPut("tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Update(
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.UpdateAsync(
            taskId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, task, "Task updated successfully."));
    }

    [HttpDelete("tasks/{taskId:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(
            taskId,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success<object?>(
            HttpContext,
            null,
            "Task deleted successfully."));
    }

    [HttpPatch("tasks/{taskId:guid}/assignment")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> Assign(
        Guid taskId,
        AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.AssignAsync(
            taskId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, task, "Task assignment updated successfully."));
    }

    [HttpPatch("tasks/{taskId:guid}/status")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateStatus(
        Guid taskId,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.UpdateStatusAsync(
            taskId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, task, "Task status updated successfully."));
    }

    [HttpPatch("tasks/{taskId:guid}/priority")]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdatePriority(
        Guid taskId,
        UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        var task = await taskService.UpdatePriorityAsync(
            taskId,
            request,
            GetCurrentUserId(),
            GetCurrentUserRoles(),
            cancellationToken);

        return Ok(ApiResponseFactory.Success(HttpContext, task, "Task priority updated successfully."));
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
