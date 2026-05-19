using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Domain.Entities.Sales;

public class QuotationActivity : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public QuotationActivityAction Action { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Description { get; set; } = default!;
    public string? MetadataJson { get; set; }
}
