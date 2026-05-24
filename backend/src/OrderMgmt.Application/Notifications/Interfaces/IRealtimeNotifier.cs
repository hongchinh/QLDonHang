namespace OrderMgmt.Application.Notifications.Interfaces;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, CancellationToken ct = default);
}
