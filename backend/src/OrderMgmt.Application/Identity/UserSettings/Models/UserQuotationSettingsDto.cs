using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.UserSettings.Models;

public class UserQuotationSettingsDto
{
    public Guid UserId { get; set; }
    public string? UserFullName { get; set; }
    public QuotationStatus? LockAtStatus { get; set; }
    public string? TemplateFileName { get; set; }
    public string? TemplateOriginalName { get; set; }
    public DateTimeOffset? TemplateUploadedAt { get; set; }
}

public class UpdateLockAtRequest
{
    public QuotationStatus? LockAtStatus { get; set; }
}
