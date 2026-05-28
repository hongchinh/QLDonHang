# Phase 01 — Backend

**Status:** [ ] pending
**Complexity:** M

## Objective

Thêm `IPwaIconRenderer` interface + `ImageSharpPwaIconRenderer` implementation, mở rộng `IBrandingService` với `GetPwaIconAsync`, và expose endpoint `GET /api/settings/branding/icon/{size}` (AllowAnonymous).

## Files

- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj`
- `backend/src/OrderMgmt.Application/Branding/Interfaces/IPwaIconRenderer.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/Branding/ImageSharpPwaIconRenderer.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`
- `backend/src/OrderMgmt.Application/Branding/Interfaces/IBrandingService.cs`
- `backend/src/OrderMgmt.Application/Branding/BrandingService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/SettingsController.cs`

## Tasks

### Task 1 — Cài ImageSharp NuGet

```powershell
cd backend
dotnet add src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj package SixLabors.ImageSharp
```

Verify: `OrderMgmt.Infrastructure.csproj` có `<PackageReference Include="SixLabors.ImageSharp" .../>`.

### Task 2 — Viết failing integration test cho endpoint

Mở `backend/tests/OrderMgmt.IntegrationTests/Settings/BrandingIconTests.cs` (file mới).

```csharp
using System.Net;
using FluentAssertions;
using OrderMgmt.IntegrationTests.Helpers;

namespace OrderMgmt.IntegrationTests.Settings;

public class BrandingIconTests(AppFactory factory) : IClassFixture<AppFactory>
{
    [Theory]
    [InlineData(192)]
    [InlineData(512)]
    public async Task GetIcon_NoLogoUploaded_Returns200Png(int size)
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"/api/settings/branding/icon/{size}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(0)]
    public async Task GetIcon_InvalidSize_Returns400(int size)
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"/api/settings/branding/icon/{size}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

Chạy test — Expected: FAIL (404 NotFound vì endpoint chưa có):

```powershell
cd backend
dotnet test tests/OrderMgmt.IntegrationTests --filter "BrandingIconTests" --logger "console;verbosity=normal"
```

### Task 3 — Tạo `IPwaIconRenderer`

Tạo file `backend/src/OrderMgmt.Application/Branding/Interfaces/IPwaIconRenderer.cs`:

```csharp
namespace OrderMgmt.Application.Branding.Interfaces;

public interface IPwaIconRenderer
{
    Task<byte[]> RenderAsync(byte[]? sourceImage, string? sourceContentType, int size, CancellationToken ct = default);
}
```

### Task 4 — Implement `ImageSharpPwaIconRenderer`

Tạo file `backend/src/OrderMgmt.Infrastructure/Branding/ImageSharpPwaIconRenderer.cs`:

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using OrderMgmt.Application.Branding.Interfaces;

namespace OrderMgmt.Infrastructure.Branding;

public class ImageSharpPwaIconRenderer : IPwaIconRenderer
{
    private static readonly Rgba32 PlaceholderColor = new(0x1e, 0x40, 0xaf);

    public async Task<byte[]> RenderAsync(byte[]? sourceImage, string? sourceContentType, int size, CancellationToken ct = default)
    {
        using var image = TryLoadSource(sourceImage, sourceContentType)
            ?? CreatePlaceholder(size);

        if (image.Width != size || image.Height != size)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Pad,
                PadColor = Color.White,
            }));
        }

        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder(), ct);
        return ms.ToArray();
    }

    private static Image? TryLoadSource(byte[]? sourceImage, string? contentType)
    {
        if (sourceImage is null || sourceImage.Length == 0) return null;
        if (contentType?.Contains("svg", StringComparison.OrdinalIgnoreCase) == true) return null;
        try { return Image.Load(sourceImage); }
        catch { return null; }
    }

    private static Image<Rgba32> CreatePlaceholder(int size)
    {
        var img = new Image<Rgba32>(size, size);
        img.Mutate(x => x.Fill(PlaceholderColor));
        return img;
    }
}
```

### Task 5 — Đăng ký dependency

Mở `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`, thêm trong block đăng ký branding services (hoặc sau dòng đăng ký `BrandingService`):

```csharp
services.AddSingleton<IPwaIconRenderer, ImageSharpPwaIconRenderer>();
```

### Task 6 — Mở rộng `IBrandingService`

Mở `backend/src/OrderMgmt.Application/Branding/Interfaces/IBrandingService.cs`, thêm method:

```csharp
Task<LogoStreamResult> GetPwaIconAsync(int size, CancellationToken ct = default);
```

### Task 7 — Implement `GetPwaIconAsync` trong `BrandingService`

Mở `backend/src/OrderMgmt.Application/Branding/BrandingService.cs`:

1. Inject `IPwaIconRenderer _iconRenderer` vào constructor.
2. Thêm method:

```csharp
public async Task<LogoStreamResult> GetPwaIconAsync(int size, CancellationToken ct = default)
{
    var branding = await _db.SystemBrandings.FirstOrDefaultAsync(ct);
    var etag = branding is null
        ? $"\"default-{size}\""
        : $"\"{branding.UpdatedAt.ToUnixTimeMilliseconds()}-{size}\"";

    var png = await _iconRenderer.RenderAsync(
        branding?.LogoMark,
        branding?.LogoMarkContentType,
        size,
        ct);

    return new LogoStreamResult(png, "image/png", etag);   // property: Content, không phải Data
}
```

> `LogoStreamResult` đã có sẵn trong codebase — dùng lại, không tạo mới.

### Task 8 — Thêm endpoint vào `SettingsController`

Mở `backend/src/OrderMgmt.WebApi/Controllers/SettingsController.cs`, thêm action:

```csharp
[HttpGet("branding/icon/{size:int}")]
[AllowAnonymous]
public async Task<IActionResult> GetPwaIcon(int size, CancellationToken ct)
{
    if (size != 192 && size != 512)
        return BadRequest(ApiResponse.Fail("INVALID_SIZE", "Size must be 192 or 512."));

    var result = await _brandingService.GetPwaIconAsync(size, ct);

    var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
    if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == result.ETag)
        return StatusCode(StatusCodes.Status304NotModified);

    Response.Headers.ETag = result.ETag;
    Response.Headers.CacheControl = "public, max-age=3600";
    return File(result.Content, result.ContentType);
}
```

### Task 9 — Chạy test để verify

```powershell
cd backend
dotnet test tests/OrderMgmt.IntegrationTests --filter "BrandingIconTests" --logger "console;verbosity=normal"
```

Expected: PASS (2 tests: 200 PNG + 400 invalid size).

### Task 10 — Build toàn bộ backend

```powershell
cd backend
dotnet build src/OrderMgmt.WebApi
```

Expected: 0 errors.

### Task 11 — Commit

```powershell
git add backend/
git commit -m "feat: add PWA icon endpoint GET /api/settings/branding/icon/{size}"
```

## Verification

```powershell
# Endpoint trả PNG (placeholder vì chưa upload logo)
curl -I http://localhost:5050/api/settings/branding/icon/192
# Expected: 200 OK, Content-Type: image/png, Cache-Control: public, max-age=3600

curl -I http://localhost:5050/api/settings/branding/icon/100
# Expected: 400 Bad Request
```

## Exit Criteria

- `dotnet build src/OrderMgmt.WebApi` — 0 errors
- Integration tests `BrandingIconTests` — PASS
- `GET /api/settings/branding/icon/192` và `/512` → 200 OK, Content-Type: image/png
- `GET /api/settings/branding/icon/100` → 400 Bad Request
- ETag header có mặt trong response
