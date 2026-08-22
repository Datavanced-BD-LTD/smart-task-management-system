namespace SmartTaskManagement.Application.Abstractions.Common;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}
