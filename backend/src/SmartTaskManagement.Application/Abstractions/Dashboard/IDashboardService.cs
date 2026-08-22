using SmartTaskManagement.Application.Features.Dashboard;

namespace SmartTaskManagement.Application.Abstractions.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        DashboardSummaryQuery query,
        Guid currentUserId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);
}
