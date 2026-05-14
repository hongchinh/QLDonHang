using OrderMgmt.Application.Identity.UserSettings.Models;

namespace OrderMgmt.Application.Identity.UserSettings.Interfaces;

public interface IUserQuotationSettingsService
{
    Task<UserQuotationSettingsDto> GetForCurrentUserAsync(CancellationToken ct = default);
    Task<UserQuotationSettingsDto> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserQuotationSettingsDto> SetLockAtAsync(Guid userId, UpdateLockAtRequest request, CancellationToken ct = default);
    Task<UserQuotationSettingsDto> UploadTemplateAsync(UploadedFile file, CancellationToken ct = default);
    Task<UserQuotationSettingsDto> DeleteTemplateAsync(CancellationToken ct = default);
    Task<(Stream Stream, string FileName)?> GetCurrentUserTemplateStreamAsync(CancellationToken ct = default);
}
