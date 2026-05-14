# Plan — Per-user quotation scoping, lock-at, per-user template

**Brainstorm gốc**: [docs/brainstorms/260514-1547-per-user-quotation-scoping/SUMMARY.md](../../brainstorms/260514-1547-per-user-quotation-scoping/SUMMARY.md)

## Goal

Triển khai 3 năng lực cho module Báo giá để bảo mật doanh thu giữa các sales (~30 user, ~1k báo giá/tháng):

1. **Owner thực thụ** trên báo giá — mỗi user chỉ xem báo giá của mình; Admin/Manager xem hết; cho phép chuyển nhượng (single + bulk khi nghỉ) và clone báo giá orphan.
2. **Ngưỡng khoá sửa** (`LockAtStatus`) theo từng user — admin cấu hình, Admin/Manager bypass.
3. **Template Excel** riêng cho từng user — user tự upload, fallback về template hệ thống, render dùng template của owner hiện tại của báo giá.

## Scope

**In scope**
- Backend: entity `Quotation.OwnerUserId`, `UserQuotationSettings`, `QuotationOwnerHistory`; 6 permission mới; scoping + lock logic; 9 endpoint mới; render Excel dùng template owner.
- Frontend: trang "Cài đặt báo giá của tôi", trang admin user-settings, bulk-transfer UI; cập nhật list/form/dashboard.
- Migration EF: 3 migration forward-only theo thứ tự `AddQuotationOwner` → `AddQuotationOwnerHistory` → `AddUserQuotationSettings`; migration đầu có SQL backfill `OwnerUserId = COALESCE(CreatedBy, <admin-seed-id>)`.
- Integration tests: 13 test case (xem brainstorm § Kiểm thử).
- OpenAPI/Swagger: bổ sung `[ProducesResponseType]` cho controller mới (Swashbuckle đã sẵn).

**Out of scope**
- Scope dữ liệu Customer / Order (giữ nguyên).
- Lưu chữ ký, số tài khoản, metadata khác trên user.
- Versioning template / cron dọn template orphan.
- Optimistic locking (last-write-wins chấp nhận).
- ClamAV / virus scan template.
- Refactor `IOwnedEntityScopeService` chung — để khi build Order module.

## Assumptions

- Production chưa có dữ liệu báo giá; dev có dữ liệu → migration `AddQuotationOwner` phải có SQL backfill, không bỏ qua.
- Với DB đã có báo giá và có dòng `CreatedBy IS NULL`, phải chạy preflight đảm bảo user `username = admin` tồn tại **trước** khi apply migration `AddQuotationOwner`. Lưu ý `DbSeeder` hiện chạy `MigrateAsync()` trước khi seed admin, nên không được dựa vào auto-seed để tạo admin cho migration này.
- Backfill query lấy `admin.Id` runtime để fill các báo giá có `CreatedBy IS NULL`; nếu không tìm thấy admin trong trường hợp cần fallback thì migration fail-fast.
- Swashbuckle đã được cấu hình ở [Program.cs](../../../backend/src/OrderMgmt.WebApi/Program.cs#L126); chỉ cần thêm `[ProducesResponseType]` trên endpoint mới.
- `ICurrentUser.HasPermission(code)` đã có (đọc từ JWT claim). Không cần DB hit để check permission.
- `[HasPermission(...)]` attribute đã hoạt động qua `PermissionAuthorizationHandler`.
- LibreOffice + template_baogia.xlsx hiện hành đã được cấu hình; user templates lưu cùng cấu trúc.

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Migration backfill fail khi `CreatedBy IS NULL` và không tìm thấy admin user | Cao | Migration kiểm tra admin tồn tại; nếu không có thì throw và fail-fast. Document chạy seed trước migration. |
| Sales bất ngờ không thấy báo giá cũ sau deploy | Trung | Feature flag `Features:QuotationOwnerScope` để rollback nhanh; truyền thông trước khi deploy. |
| Template upload độc hại (zip-bomb / Excel injection) | Trung | Magic bytes + ClosedXML parse + size cap (5 MB) + size unzipped cap (50 MB) + deny .xlsm/.xls. |
| Concurrent edit gây mất dữ liệu | Thấp | Last-write-wins chấp nhận (scope owner đã giới hạn). Document rõ trong constraint. |
| `Quotation.OwnerUserId` orphan khi user soft-delete | Trung | Owner lookup/projection phải dùng `IgnoreQueryFilters()` để đọc được user đã soft-delete; DTO list/detail trả `IsOwnerDeleted`; UI hiển thị badge "Owner đã nghỉ"; cấm sửa; admin có bulk-transfer. |
| Test fixture chưa hỗ trợ multi-user login | Thấp | Mở rộng `QuotationTestBase.CreateTestUserAsync` + `AuthenticateAsync` cho test owner-scoping. |

## Phases

- [ ] Phase 01 — Quotation ownership + migration (M) — [phase-01-quotation-ownership.md](phase-01-quotation-ownership.md)
- [ ] Phase 02 — Scoping + owner transfer + audit (L) — [phase-02-scoping-and-transfer.md](phase-02-scoping-and-transfer.md)
- [ ] Phase 03 — Lock-at + user settings (M) — [phase-03-lock-at-settings.md](phase-03-lock-at-settings.md)
- [ ] Phase 04 — Per-user template + render fallback (L) — [phase-04-per-user-template.md](phase-04-per-user-template.md)
- [ ] Phase 05 — Bulk-transfer + clone + dashboard stats (M) — [phase-05-bulk-clone-dashboard.md](phase-05-bulk-clone-dashboard.md)
- [ ] Phase 06 — Frontend pages + integration (L) — [phase-06-frontend.md](phase-06-frontend.md)
- [ ] Phase 07 — Final verification + smoke (S) — [phase-07-final-verification.md](phase-07-final-verification.md)

## Final Verification

Sau khi toàn bộ phase pass, chạy ở thư mục `backend/`:

```powershell
dotnet build OrderMgmt.sln -c Debug
dotnet test tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build -c Debug
```

Smoke (manual) ở frontend với 3 tài khoản: ADMIN, SALES1, SALES2.

| Action | Expected |
|---|---|
| SALES1 tạo BG → SALES2 list | SALES2 không thấy BG |
| ADMIN list | thấy tất cả + cột "Chủ sở hữu" |
| SALES1 self-transfer cho SALES2 | SALES1 mất quyền, SALES2 thấy |
| ADMIN bulk-transfer toàn bộ của SALES1 cho SALES2 | toàn bộ chuyển; lần 2 idempotent (0 affected) |
| SALES1 upload template → render Excel | file Excel dùng template SALES1 |
| ADMIN đặt lock-at = Sent cho SALES1 → SALES1 sửa BG ở Sent | 400 CONFLICT |
| ADMIN soft-delete SALES1 → ADMIN list | BG hiện badge "Owner đã nghỉ", chỉ clone được |
| ADMIN flip flag `Features:QuotationOwnerScope = false` | SALES2 thấy lại và GET/print được BG của SALES1 nếu có permission action tương ứng (rollback test owner-guard full bypass) |

## Rollback / Recovery

- **App-level rollback**: set `Features:QuotationOwnerScope = false` trong appsettings → bỏ qua cả owner filter (`ApplyOwnerScope`) và owner mismatch guard (`EnsureCanAccess`) cho list/detail/print/update/delete/transition. Vẫn giữ `[HasPermission]`, lock-at, cancelled và orphan guards.
- **DB rollback**: 3 migration forward-only theo thứ tự `AddQuotationOwner` → `AddQuotationOwnerHistory` → `AddUserQuotationSettings`. Nếu cần rollback DB, dùng `dotnet ef database update <PreviousMigration>` (chỉ áp dụng môi trường dev/staging). Production không rollback DB — chỉ rollback code + feature flag.
- **Template file dọn dẹp**: nếu cần xoá toàn bộ user templates, xoá thư mục `templates/users/`. Service tự fallback về template chung.
- **Hot-fix path**: nếu phát hiện regression nghiêm trọng, revert PR FE trước (user không thấy UI mới nhưng backend vẫn hoạt động); BE giữ lại vì migration đã chạy.
