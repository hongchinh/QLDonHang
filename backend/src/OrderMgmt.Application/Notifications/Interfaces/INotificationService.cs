using OrderMgmt.Application.Notifications.Models;

namespace OrderMgmt.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task SendAsync(Guid userId, string type, string title, string? body, string? link, CancellationToken ct = default);
    Task<List<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken ct = default);
    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
