# Execution Report — PWA Logo Icon từ Logo Branding

**Plan:** `docs/plans/260528-1000-pwa-logo-icon/SUMMARY.md`
**Executed:** 2026-05-28

## Phases Completed

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 01 — Backend | ✅ PASS | All 4 integration tests pass, build 0 errors |
| Phase 02 — Frontend | ✅ PASS | typecheck 0 errors, build successful |

## Files Changed

**New files:**
- `backend/src/OrderMgmt.Application/Branding/Interfaces/IPwaIconRenderer.cs`
- `backend/src/OrderMgmt.Infrastructure/Branding/SkiaSharpPwaIconRenderer.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Settings/BrandingIconTests.cs`

**Modified files:**
- `backend/src/OrderMgmt.Application/Branding/Interfaces/IBrandingService.cs` — added `GetPwaIconAsync`
- `backend/src/OrderMgmt.Application/Branding/Services/BrandingService.cs` — injected `IPwaIconRenderer`, implemented `GetPwaIconAsync`
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` — registered `SkiaSharpPwaIconRenderer`
- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` — added `SkiaSharp` + `SkiaSharp.NativeAssets.Win32` packages
- `backend/src/OrderMgmt.WebApi/Controllers/SettingsController.cs` — added `GET /api/settings/branding/icon/{size}` endpoint
- `frontend/vite.config.ts` — manifest icons → API URLs
- `frontend/index.html` — favicon → API URL

## Verification Commands Run

| Command | Outcome |
|---------|---------|
| `dotnet build src/OrderMgmt.WebApi` | ✅ 0 errors, 0 warnings |
| `dotnet test --filter "BrandingIconTests"` (TEST_DB_CONNECTION=qldonhang_integration) | ✅ 4/4 passed |
| `npm run typecheck` | ✅ 0 errors |
| `npm run build` | ✅ successful |

## Deviations from Plan

1. **SkiaSharp instead of ImageSharp** — Plan specified `SixLabors.ImageSharp`. ImageSharp 4.x requires a commercial license (build-time check). ImageSharp 3.x is free but `SixLabors.ImageSharp.Drawing` (needed for the `Fill` method) is incompatible with net9.0. Switched to `SkiaSharp` (MIT licensed, .NET 9 compatible). Interface `IPwaIconRenderer` is unchanged; class renamed to `SkiaSharpPwaIconRenderer`.

2. **`BadRequest()` instead of `ApiResponse.Fail("INVALID_SIZE", ...)`** — `ApiResponse.Fail` takes an `ApiError` object, not `(string, string)`. Used `BadRequest()` consistent with existing `GetLogo` action in the same controller.

3. **No `maskable` icon** — Plan's `vite.config.ts` comment explains this intentionally (as per the plan itself): `ResizeMode.Pad` doesn't guarantee the 60% safe zone required for maskable icons on Android.

4. **Integration test pattern adapted** — Plan used `IClassFixture<AppFactory>` and `factory.CreateAnonymousClient()` which don't exist in this codebase. Adapted to use `[Collection(nameof(PostgresCollection))]` + `IAsyncLifetime` pattern consistent with all other integration tests.

## Residual Risks / Follow-ups

- **SkiaSharp on Linux/Docker**: If the deployment target moves to Linux, add `SkiaSharp.NativeAssets.Linux` or use `SkiaSharp.NativeAssets.Linux.NoDependencies`. Currently only Windows native assets are installed.
- **PWA cache**: Browsers that already installed the PWA will keep old static icons until their service worker updates. Not breaking — resolves on next SW update cycle.
- **ETag cache 1 hour**: After upload, favicon/PWA icon will show stale version for up to 1 hour on clients that cached it.
