using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Notifications.Interfaces;
using WebPush;

namespace OrderMgmt.Infrastructure.Notifications;

public class PushSenderService : IPushSender
{
    private readonly IAppDbContext _db;
    private readonly IOptions<VapidOptions> _vapid;
    private readonly ILogger<PushSenderService> _logger;

    public PushSenderService(IAppDbContext db, IOptions<VapidOptions> vapid, ILogger<PushSenderService> logger)
    {
        _db = db;
        _vapid = vapid;
        _logger = logger;
    }

    public async Task SendAsync(Guid userId, string title, string body, string url, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_vapid.Value.PrivateKey)) return;

        var subscriptions = await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

        if (subscriptions.Count == 0) return;

        var client = new WebPushClient();
        var vapidDetails = new VapidDetails(
            _vapid.Value.Subject,
            _vapid.Value.PublicKey,
            _vapid.Value.PrivateKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body, url });

        var toDelete = new List<Domain.Notifications.PushSubscription>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload, vapidDetails, ct);
            }
            catch (WebPushException ex) when ((int)ex.StatusCode == 410)
            {
                toDelete.Add(sub);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send push notification to endpoint {Endpoint}", sub.Endpoint);
            }
        }

        if (toDelete.Count > 0)
        {
            _db.PushSubscriptions.RemoveRange(toDelete);
            await _db.SaveChangesAsync(ct);
        }
    }
}
