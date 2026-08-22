using SmartTaskManagement.Application.Abstractions.Common;

namespace SmartTaskManagement.Infrastructure.Authentication;

public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
