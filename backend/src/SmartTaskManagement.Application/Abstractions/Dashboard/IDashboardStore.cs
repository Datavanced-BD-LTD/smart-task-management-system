using SmartTaskManagement.Application.Features.Dashboard;

namespace SmartTaskManagement.Application.Abstractions.Dashboard;

public interface IDashboardStore
{
    Task<DashboardAggregate> GetSummaryAsync(
        DashboardScope scope,
        DateTime utcNow,
        int upcomingDays,
        CancellationToken cancellationToken);
}

public sealed record DashboardScope(
    Guid? ProjectManagerId,
    Guid? MemberUserId,
    Guid? AssignedToUserId);
