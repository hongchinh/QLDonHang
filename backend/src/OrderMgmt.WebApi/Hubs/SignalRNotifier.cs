using Microsoft.AspNetCore.SignalR;
using OrderMgmt.Application.Notifications.Interfaces;

namespace OrderMgmt.WebApi.Hubs;

public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task NotifyUserAsync(Guid userId, CancellationToken ct = default)
        => _hub.Clients.Group($"user-{userId}")
               .SendAsync("NewNotification", cancellationToken: ct);
}
