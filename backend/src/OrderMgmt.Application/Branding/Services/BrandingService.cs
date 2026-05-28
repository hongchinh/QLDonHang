using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Branding.Interfaces;
using OrderMgmt.Application.Branding.Models;
using OrderMgmt.Domain.Branding;
using OrderMgmt.Domain.Common;

namespace OrderMgmt.Application.Branding.Services;

public class BrandingService : IBrandingService
{
    private const int SingletonId = 1;
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/svg+xml",
    };

    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IPwaIconRenderer _iconRenderer;

    public BrandingService(IAppDbContext db, IDateTime clock, ICurrentUser currentUser, IPwaIconRenderer iconRenderer)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
        _iconRenderer = iconRenderer;
    }

    public async Task<BrandingDto> GetMetaAsync(CancellationToken ct = default)
    {
        var meta = await _db.SystemBranding.AsNoTracking()
            .Where(b => b.Id == SingletonId)
            .Select(b => new BrandingDto
            {
                HasLogoFull = b.LogoFull != null,
                HasLogoMark = b.LogoMark != null,
                UpdatedAt = b.UpdatedAt,
            })
            .FirstOrDefaultAsync(ct);

        return meta ?? new BrandingDto
        {
            HasLogoFull = false,
            HasLogoMark = false,
            UpdatedAt = DateTimeOffset.UnixEpoch,
        };
    }

    public async Task<LogoStreamResult?> GetLogoAsync(string variant, CancellationToken ct = default)
    {
        var row = await _db.SystemBranding.AsNoTracking()
            .Where(b => b.Id == SingletonId)
            .Select(b => new
            {
                b.LogoFull,
                b.LogoFullContentType,
                b.LogoMark,
                b.LogoMarkContentType,
                b.UpdatedAt,
            })
            .FirstOrDefaultAsync(ct);
        if (row is null) return null;

        var (content, contentType) = variant switch
        {
            "full" => (row.LogoFull, row.LogoFullContentType),
            "mark" => (row.LogoMark, row.LogoMarkContentType),
            _ => (null, null),
        };

        if (content is null || string.IsNullOrEmpty(contentType)) return null;

        var etag = $"\"{row.UpdatedAt.ToUnixTimeMilliseconds()}-{variant}\"";
        return new LogoStreamResult(content, contentType, etag);
    }

    public async Task<LogoStreamResult> GetPwaIconAsync(int size, CancellationToken ct = default)
    {
        var branding = await _db.SystemBranding.FirstOrDefaultAsync(ct);
        var etag = branding is null
            ? $"\"default-{size}\""
            : $"\"{branding.UpdatedAt.ToUnixTimeMilliseconds()}-{size}\"";

        var png = await _iconRenderer.RenderAsync(
            branding?.LogoMark,
            branding?.LogoMarkContentType,
            size,
            ct);

        return new LogoStreamResult(png, "image/png", etag);
    }

    public async Task<BrandingDto> UpdateAsync(LogoUpload? logoFull, LogoUpload? logoMark, CancellationToken ct = default)
    {
        if (logoFull is null && logoMark is null)
            throw new DomainException("VALIDATION", "Cần ít nhất 1 file logo.");

        var fullBytes = logoFull is null ? null : await ReadValidatedAsync(logoFull, "logoFull", ct);
        var markBytes = logoMark is null ? null : await ReadValidatedAsync(logoMark, "logoMark", ct);

        var entity = await _db.SystemBranding.FirstOrDefaultAsync(b => b.Id == SingletonId, ct);
        if (entity is null)
        {
            entity = new SystemBranding { Id = SingletonId };
            _db.SystemBranding.Add(entity);
        }

        if (fullBytes is not null)
        {
            entity.LogoFull = fullBytes;
            entity.LogoFullContentType = logoFull!.ContentType;
        }
        if (markBytes is not null)
        {
            entity.LogoMark = markBytes;
            entity.LogoMarkContentType = logoMark!.ContentType;
        }

        entity.UpdatedAt = _clock.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);

        return new BrandingDto
        {
            HasLogoFull = entity.LogoFull != null,
            HasLogoMark = entity.LogoMark != null,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    private static async Task<byte[]> ReadValidatedAsync(LogoUpload file, string field, CancellationToken ct)
    {
        if (file.Length <= 0)
            throw new DomainException("VALIDATION", $"{field}: file rỗng.");
        if (file.Length > MaxBytes)
            throw new DomainException("VALIDATION", $"{field}: file vượt 2MB.");
        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new DomainException("VALIDATION", $"{field}: định dạng không hỗ trợ (chỉ PNG/JPEG/SVG).");

        using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
