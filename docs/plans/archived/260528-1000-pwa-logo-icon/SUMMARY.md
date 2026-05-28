# PWA Logo Icon từ Logo Branding

## Goal

Thêm endpoint `GET /api/settings/branding/icon/{size}` (AllowAnonymous) để serve `LogoMark` được resize thành PNG 192×192 hoặc 512×512. Cập nhật PWA manifest để dùng URL này thay vì file tĩnh — khi admin upload "Logo vuông" qua Settings, icon PWA sẽ tự cập nhật trên tất cả thiết bị lần cài tiếp theo.

Hệ thống branding (entity, controller, upload UI) đã có sẵn — chỉ cần thêm resize + endpoint + cập nhật manifest.

## Scope

**In scope:**
- `IPwaIconRenderer` interface (Application) + `ImageSharpPwaIconRenderer` implementation (Infrastructure)
- `IBrandingService.GetPwaIconAsync(int size)` + implement trong `BrandingService`
- `GET /api/settings/branding/icon/{size}` trong `SettingsController` (AllowAnonymous, size ∈ {192, 512})
- Update `vite.config.ts` PWA manifest icons → API URLs
- Update `index.html` favicon → API URL

**Out of scope:**
- Upload UI mới (đã có `BrandingTab` với "Logo vuông")
- Resize logo khi upload (resize khi serve, không pre-generate)
- Hỗ trợ SVG source cho PWA icon (SVG không resize được bằng ImageSharp — fallback sang placeholder màu xanh)
- Thay đổi màu/text placeholder (dùng `#1e40af` solid blue)

## Assumptions

- `LogoMark` trong DB là nguồn dùng cho PWA icon
- SVG `LogoMark` → serve placeholder màu xanh (#1e40af), không lỗi
- ImageSharp không có sẵn — cần cài `SixLabors.ImageSharp` vào `OrderMgmt.Infrastructure`
- `GET /api/settings/branding/icon/{size}` là public (không cần auth) — cùng pattern với `GET /api/settings/branding/logo`
- Resize on-the-fly khi serve (không pre-generate và lưu DB) — phù hợp với traffic thấp của admin app
- ETag cache `public, max-age=3600` (1 giờ) để tránh resize lặp lại

## Risks

- **ImageSharp ResizeMode**: Nếu `LogoMark` không vuông, cần chọn `ResizeMode.Pad` (thêm viền trắng) hay `ResizeMode.Crop` (cắt). Plan dùng `ResizeMode.Pad` với background trắng — an toàn cho mọi tỉ lệ.
- **PWA manifest cache**: Sau khi cập nhật manifest, browser đã cài PWA sẽ cần cập nhật SW mới để nhận icon mới. Không breaking nhưng icon cũ có thể tồn tại đến lần update tiếp.

## Phases

- [x] Phase 01 — Backend: IPwaIconRenderer + GetPwaIconAsync + Controller endpoint (M) — `phase-01-backend.md`
- [x] Phase 02 — Frontend: Update manifest + favicon (S) — `phase-02-frontend.md`

## Final Verification

```bash
# Backend build
cd backend && dotnet build src/OrderMgmt.WebApi

# Integration tests
cd backend && dotnet test tests/OrderMgmt.IntegrationTests --logger "console;verbosity=normal"

# Frontend typecheck
cd frontend && npm run typecheck

# Frontend build
cd frontend && npm run build
```

**Manual smoke test:**
1. `npm run dev` + `dotnet run` — mở `http://localhost:5173`
2. `curl -I http://localhost:5050/api/settings/branding/icon/192` → 200 OK, Content-Type: image/png
3. Upload "Logo vuông" qua Settings → Admin Settings
4. `curl http://localhost:5050/api/settings/branding/icon/192 -o icon.png` → verify PNG là logo vừa upload
5. Chrome DevTools → Application → Manifest → Icons hiển thị đúng

## Rollback / Recovery

- Phase 01: `git revert` các commit, xóa 3 file mới (`IPwaIconRenderer.cs`, `ImageSharpPwaIconRenderer.cs`), restore `IBrandingService.cs`, `BrandingService.cs`, `SettingsController.cs`, `DependencyInjection.cs`
- Phase 02: `git revert` → manifest quay về `/icons/icon-*.png` static, `index.html` quay về `/vite.svg`
- Không có DB migration → rollback hoàn toàn bằng `git revert`
