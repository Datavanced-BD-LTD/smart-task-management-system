namespace SmartTaskManagement.Application.Features.Ai;

public sealed record ImproveTaskDescriptionRequest(
    string Description);

public sealed record ImproveTaskDescriptionResponse(
    string ImprovedDescription);
