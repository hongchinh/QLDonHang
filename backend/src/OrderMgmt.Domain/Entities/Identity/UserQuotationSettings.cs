using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Domain.Entities.Identity;

public class UserQuotationSettings : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public QuotationStatus? LockAtStatus { get; set; }

    public string? TemplateFileName { get; set; }
    public string? TemplateOriginalName { get; set; }
    public DateTimeOffset? TemplateUploadedAt { get; set; }

    public string? HandoverWithPriceTemplateFileName { get; set; }
    public string? HandoverWithPriceTemplateOriginalName { get; set; }
    public DateTimeOffset? HandoverWithPriceTemplateUploadedAt { get; set; }

    public string? HandoverNoPriceTemplateFileName { get; set; }
    public string? HandoverNoPriceTemplateOriginalName { get; set; }
    public DateTimeOffset? HandoverNoPriceTemplateUploadedAt { get; set; }
}
