# Phase 07 — Final verification + smoke

**Status:** [x] complete
**Complexity:** S

## Objective

Chạy toàn bộ test suite, smoke test end-to-end, kiểm tra feature flag rollback, đảm bảo OpenAPI/Swagger phản ánh đầy đủ endpoint mới.

## Files

- (không sửa file production — chỉ chạy verify)
- `docs/plans/260514-1614-per-user-quotation-scoping/EXECUTION-REPORT.md` (new, viết khi execute-plan hoàn thành)

## Tasks

1. **Build & full test sweep**:
   ```powershell
   dotnet build backend/OrderMgmt.sln -c Debug
   dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build -c Debug
   npm --prefix frontend run typecheck
   npm --prefix frontend run lint
   npm --prefix frontend run build
   ```
   Tất cả phải pass 0 fail.
2. **Migration apply check**:
   - Start clean Postgres container; run app với `Database:AutoMigrateAndSeed = true` → 3 migration apply không lỗi vì không có báo giá cũ cần admin fallback.
   - Với DB đã có báo giá cũ: chạy preflight `COUNT(*) WHERE created_by IS NULL`; nếu > 0 thì tạo/đảm bảo user `admin` tồn tại trước khi apply `AddQuotationOwner`.
   - Verify schema:
   ```sql
   \d quotations           -- có owner_user_id NOT NULL + ix_quotations_owner_status_date
   \d user_quotation_settings
   \d quotation_owner_history
   SELECT code FROM permissions WHERE module = 'sales' AND code LIKE 'quotations.%';  -- có đủ 12 dòng
   ```
3. **Smoke FE manual** (xem checklist phase 06 — 9 case). Ghi lại screenshot vào `EXECUTION-REPORT.md`.
4. **Smoke OpenAPI**: mở `/swagger` → kiểm xuất hiện đầy đủ:
   - `PATCH /api/quotations/{id}/owner`
   - `POST /api/quotations/{id}/clone`
   - `GET/PUT/DELETE /api/me/quotation-settings` + `/template`
   - `GET/PUT /api/admin/user-settings/{userId}` + `/lock-at`
   - `POST /api/admin/users/{userId}/transfer-quotations`
   - `GET /api/dashboard/quotation-stats`
   - Mỗi endpoint có response shape đúng (do `[ProducesResponseType]` đã add).
5. **Feature flag rollback test**:
   - Set `Features:QuotationOwnerScope = false` trong `appsettings.Development.json`.
   - Restart app.
   - SALES2 login → list báo giá → thấy báo giá của SALES1; GET detail/print cũng qua owner guard nếu SALES2 có permission action tương ứng.
   - Đặt lại `true` → SALES2 không thấy nữa.
6. **Performance sanity check** (quy mô 1k báo giá/tháng × 12 tháng = ~12k):
   - Seed thủ công 1000 báo giá cho SALES1 + 1000 cho SALES2 (dùng script trong test fixture hoặc psql).
   - GET list với filter scope → response < 300 ms.
   - GET dashboard stats → < 500 ms.
   - Kiểm `EXPLAIN ANALYZE` cho query list xem có dùng index `ix_quotations_owner_status_date` không.
7. **Documentation update**:
   - Cập nhật `docs/architecture/system-architecture.md` mục Authorization với 6 permission mới.
   - Cập nhật `docs/codebase/directory-structure.md` thêm `templates/users/` và bảng `user_quotation_settings`, `quotation_owner_history`.
8. **Viết EXECUTION-REPORT.md** với:
   - Summary 6 phase đã chạy + status.
   - Migration applied list.
   - Test count: BE pass / FE typecheck pass.
   - Open issues (nếu có) + workaround.

## Verification

```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build
```

Đếm test mới (so với baseline trước plan):
- `QuotationOwnerScopeTests`: 8 case
- `QuotationLockAtTests`: 9 case
- `QuotationTemplateTests`: 10 case
- `QuotationBulkCloneDashboardTests`: 11 case
→ **Tối thiểu 38 test mới pass**.

## Exit Criteria

- `dotnet test` 0 fail; `npm run build` 0 lỗi.
- 3 migration đã apply trên DB sạch.
- 9 endpoint mới + 12 permission visible trong `/swagger`.
- Smoke FE 9 case + rollback flag test pass.
- Docs architecture + directory-structure cập nhật.
- EXECUTION-REPORT.md tồn tại với summary đầy đủ.
