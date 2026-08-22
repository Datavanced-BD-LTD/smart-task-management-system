namespace SmartTaskManagement.Application.Abstractions.Ai;

public interface IAiTaskDescriptionProvider
{
    Task<string> ImproveAsync(
        string description,
        CancellationToken cancellationToken);
}
