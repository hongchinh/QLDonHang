using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Notifications.Interfaces;
using OrderMgmt.Application.Notifications.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Notifications;

namespace OrderMgmt.Application.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;

    public NotificationService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, int limit, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.Link, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var entity = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
            ?? throw new NotFoundException(nameof(Notification), notificationId);
        if (!entity.IsRead)
        {
            entity.IsRead = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);
        if (unread.Count == 0) return;
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}
