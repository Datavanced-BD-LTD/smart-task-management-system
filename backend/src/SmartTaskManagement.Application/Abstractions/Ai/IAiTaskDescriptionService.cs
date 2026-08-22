using SmartTaskManagement.Application.Features.Ai;

namespace SmartTaskManagement.Application.Abstractions.Ai;

public interface IAiTaskDescriptionService
{
    Task<ImproveTaskDescriptionResponse> ImproveAsync(
        ImproveTaskDescriptionRequest request,
        CancellationToken cancellationToken);
}
