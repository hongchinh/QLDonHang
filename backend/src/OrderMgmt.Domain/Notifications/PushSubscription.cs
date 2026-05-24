namespace OrderMgmt.Domain.Notifications;

public class PushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = default!;
    public string P256DH { get; set; } = default!;
    public string Auth { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
