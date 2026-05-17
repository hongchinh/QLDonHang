using OrderMgmt.Application.Branding.Models;

namespace OrderMgmt.Application.Branding.Interfaces;

public interface IBrandingService
{
    Task<BrandingDto> GetMetaAsync(CancellationToken ct = default);
    Task<LogoStreamResult?> GetLogoAsync(string variant, CancellationToken ct = default);
    Task<BrandingDto> UpdateAsync(LogoUpload? logoFull, LogoUpload? logoMark, CancellationToken ct = default);
}
