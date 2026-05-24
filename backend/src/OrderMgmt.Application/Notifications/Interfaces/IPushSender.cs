namespace OrderMgmt.Application.Notifications.Interfaces;

public interface IPushSender
{
    Task SendAsync(Guid userId, string title, string body, string url, CancellationToken ct = default);
}
