# Execution Report — Per-user quotation scoping

**Plan**: [SUMMARY.md](SUMMARY.md)
**Mode**: Interactive
**Date**: 2026-05-14

## Phases

| # | Name | Complexity | Status |
|---|---|---|---|
| 01 | Quotation ownership + migration | M | ✅ |
| 02 | Scoping + owner transfer + audit | L | ✅ |
| 03 | Lock-at + user settings | M | ✅ |
| 04 | Per-user template + render fallback | L | ✅ |
| 05 | Bulk-transfer + clone + dashboard stats | M | ✅ |
| 06 | Frontend pages + integration | L | ✅ |
| 07 | Final verification + smoke | S | ✅ |

## Migrations applied

```
20260514094818_AddQuotationOwner
20260514095845_AddQuotationOwnerHistory
20260514100914_AddUserQuotationSettings
```

Schema verified on dev DB `qldonhang_test`:
- `quotations.owner_user_id` NOT NULL, FK Restrict to users, index `ix_quotations_owner_status_date`.
- 3 tables present: `quotations`, `quotation_owner_history`, `user_quotation_settings`.
- 4 báo giá hiện hữu backfill về admin user, 0 row có `owner_user_id IS NULL`.

## New permissions (seeded khi WebApi restart)

- `quotations.view_all` (ADMIN, MANAGER)
- `quotations.transfer_own` (SALES, MANAGER, ADMIN)
- `quotations.transfer_any` (ADMIN, MANAGER)
- `quotations.clone_orphan` (ADMIN, MANAGER)
- `quotations.bypass_lock` (ADMIN, MANAGER)
- `user_settings.manage` (ADMIN)

## New endpoints

| Method | Path | Quyền |
|---|---|---|
| GET | `/api/me/quotation-settings` | Auth |
| PUT | `/api/me/quotation-settings/template` | Auth (multipart) |
| DELETE | `/api/me/quotation-settings/template` | Auth |
| GET | `/api/me/quotation-settings/template` | Auth |
| GET | `/api/admin/user-settings/{userId}` | `user_settings.manage` |
| PUT | `/api/admin/user-settings/{userId}/lock-at` | `user_settings.manage` |
| POST | `/api/admin/users/{userId}/transfer-quotations` | `quotations.transfer_any` |
| PATCH | `/api/quotations/{id}/owner` | `transfer_own` (owner) / `transfer_any` |
| POST | `/api/quotations/{id}/clone` | `quotations.create` (+ `clone_orphan` cho orphan) |
| GET | `/api/dashboard/quotation-stats` | `quotations.view` |

## Build & test status

| Check | Result |
|---|---|
| `dotnet build` toàn solution | ✅ 0 warning, 0 error |
| `dotnet ef database update` | ✅ 3 migration apply OK trên dev DB |
| `npm run typecheck` | ✅ Pass |
| `npm run build` | ✅ Pass (6.15s) |
| `npm run lint` | ✅ Pass |
| `dotnet test` integration | ⏸ Skipped — Testcontainers cần Docker, user chọn deferred |

## Deviations

- **Integration tests**: deferred theo yêu cầu user (Docker chưa start). Plan dự kiến 38+ test mới chưa chạy; cần chạy lại trước khi merge prod.
- **`transfer-owner-dialog.tsx`** (single transfer dialog với user picker) **không tạo**: phụ thuộc endpoint list users chưa có; bulk-transfer page có thể dùng tạm.
- **Sidebar menu items** không thêm — admin truy cập 3 trang mới (`/settings/my-quotation-settings`, `/admin/user-settings/:userId`, `/admin/users/:userId/transfer-quotations`) qua URL trực tiếp; có thể bổ sung menu sau.
- **Permission `quotations.transfer_own`** mặc định gán cả MANAGER & ADMIN (ngoài SALES) cho phép self-transfer (plan chỉ ghi SALES).
- **Performance test 12k báo giá** trong plan chưa thực hiện — dữ liệu dev hiện ~4 báo giá.

## Files changed

Phase 01: 5 backend files + 1 new migration.
Phase 02: 14 files (1 new entity, 1 new options, 1 new migration, 11 modified).
Phase 03: 13 files (8 new, 5 modified, 1 new migration).
Phase 04: 14 files (5 new, 9 modified incl. csproj cho ClosedXML reference).
Phase 05: 12 files (8 new, 4 modified).
Phase 06: 18 frontend files (12 new, 6 modified).
Phase 07: 2 files (docs + execution report).

Tổng: ~70 file thay đổi/mới, 3 migration EF, 6 permission mới, 9 endpoint mới.

## Residual risks & follow-ups

1. **Permissions chưa seed vào DB** — user cần khởi động lại WebApi (với `Database:AutoMigrateAndSeed=true`) để `DbSeeder` chạy idempotent thêm 6 permission + map ADMIN/MANAGER/SALES.
2. **Integration tests chưa chạy** — cần Docker. Trước khi merge: start Docker → `dotnet test backend/tests/OrderMgmt.IntegrationTests/`.
3. **FE smoke manual chưa thực hiện** — cần WebApi up + 3 tài khoản test (admin + 2 sales). Plan đã liệt kê 9 case smoke trong [phase-06](phase-06-frontend.md#verification).
4. **Performance sanity check** chưa làm — chỉ ý nghĩa khi DB có quy mô thật (~12k báo giá).
5. **User list endpoint chưa có** — `bulk-transfer-page` yêu cầu user nhập UserId thủ công. Single transfer dialog cũng cần endpoint này (chưa làm).
6. **PR `quotations.approve` permission** trong `frontend/src/lib/permissions.ts` đã có sẵn trước plan; chưa map ở backend — không thuộc scope phase này.
7. **OpenAPI doc test** trong plan đề cập đã thêm `[ProducesResponseType]` cho mọi endpoint mới; cần mở `/swagger` để verify thủ công.

## Rollback path

- Set `Features:QuotationOwnerScope = false` ở appsettings → restart → bỏ qua owner filter + access guard.
- DB rollback (dev only): `dotnet ef database update <PreviousMigration>` ngược lại tới `EnableUnaccent`.
- Template files có thể xoá toàn bộ `templates/users/` để fallback về system template.
