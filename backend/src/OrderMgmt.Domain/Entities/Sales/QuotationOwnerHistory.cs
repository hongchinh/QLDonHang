using OrderMgmt.Domain.Common;

namespace OrderMgmt.Domain.Entities.Sales;

public class QuotationOwnerHistory : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Guid? OldOwnerUserId { get; set; }
    public Guid NewOwnerUserId { get; set; }
    public Guid ActorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}
