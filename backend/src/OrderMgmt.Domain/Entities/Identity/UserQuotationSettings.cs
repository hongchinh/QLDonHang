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
}
