# Quotation List — Owner Filter (Admin / Manager / Accountant)

## Goal
Thêm bộ lọc multi-select "Chủ sở hữu" trên màn hình `quotation-list-page.tsx` cho user có quyền `quotations.view_all` (Admin, Manager, Accountant). Filter áp theo `OwnerUserId`, danh sách dropdown chỉ chứa user đã từng có báo giá (gồm user đã nghỉ để xử lý orphan). Cấp `quotations.view_all` cho role Accountant trong seeder.

## Scope
**In scope**
- Backend: thêm field `OwnerUserIds` (CSV) vào `QuotationListRequest` + áp filter trong `QuotationService.ListAsync` (guard sau `HasPermission(ViewAll)`). Endpoint mới `GET /quotations/owners?includeDeleted=true`. Helper `OwnerIdListParser`. Validator mở rộng. Seed Accountant role.
- Frontend: thêm type `QuotationOwnerOption`, mở rộng `QuotationListParams` với `ownerUserIds`, hook `useQuotationOwners`, render `<MultiSelect>` "Chủ sở hữu" trong list page. URL param `ownerUserIds=id1,id2` (cùng key với BE binding — end-to-end nhất quán).
- Tests: mở rộng `QuotationListFilterTests.cs` (5 cases mới) + tạo `QuotationOwnersEndpointTests.cs` (5 cases mới); test pure parse util frontend.

**Out of scope**
- Filter theo `CreatedBy` (đã quyết: chỉ owner).
- Permission `quotations.filter_by_owner` tách riêng (đã quyết: dùng `ViewAll`).
- Group owner theo role trong dropdown.
- SQL migration thủ công (seeder tự chạy on startup).
- Search box bên trong dropdown owner (datasets nhỏ).

## Assumptions
- Seeder `DbSeeder.SeedRolesAsync` chạy on startup ở mọi môi trường (dev/staging/prod) và là idempotent — thêm `RolePermission` thiếu cho Accountant không cần thao tác thủ công. (Đã confirm với user.)
- `<MultiSelect>` hiện có không cần search box — dataset owner thực tế ~10-30 user. Nếu vượt 50, sẽ thêm search box trong follow-up.
- Backend giữ convention "silently ignore" filter param khi caller không có quyền (không 403) để URL shared không gãy. Pattern này nhất quán với cách `ApplyOwnerScope` xử lý view_all.
- Sale user **không** gửi `ownerUserIds` từ FE (UI không render); nếu họ forge URL, BE ignore silently.
- React-query cache key `['quotation-owners', { includeDeleted }]`, staleTime 5 phút là chấp nhận được — owner list ít thay đổi.

## Risks
- **Aggregates regression**: `aggregates` ở [QuotationService.cs:148-161](../../backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs#L148-L161) phải reflect đúng subset đã filter owner. Phase 2 task có dedicated test.
- **Owner đã xóa**: query phải `IgnoreQueryFilters()` khi join `Users` để không drop orphan quotation khỏi dropdown.
- **Forged param từ sale**: re-check `HasPermission(ViewAll)` trong `ListAsync` ngay tại điểm áp filter — không dựa vào FE.
- **Permission cache trên client cũ**: Accountant đã login trước khi deploy seed mới sẽ không có `view_all` trong JWT cho đến khi refresh token. Acceptable — đã có refresh-token flow.

## Phases
- [x] Phase 01 — Backend: DTO, parser, validator, service filter (M) — `phase-01-backend-filter.md`
- [x] Phase 02 — Backend: endpoint owners + seed Accountant (M) — `phase-02-backend-owners-endpoint.md`
- [x] Phase 03 — Backend: integration tests (M) — `phase-03-backend-tests.md`
- [x] Phase 04 — Frontend: types, API, hook (S) — `phase-04-frontend-api.md`
- [x] Phase 05 — Frontend: UI integration + URL state + tests (M) — `phase-05-frontend-ui.md`

## Pre-execution checklist
- **WebApi restart (Phase 02)**: Phase 02 thêm route mới + chạy seeder mới → cần restart WebApi process một lần (trái với memory rule "không restart"). Trước khi bắt đầu Phase 02, xác nhận với user:
  - Option A — chấp nhận bounce WebApi sau khi build Phase 02 xong (chọn thời điểm).
  - Option B — chạy WebApi dưới `dotnet watch` để restart tự động.
- **Cache invalidation follow-up (out of scope)**: khi admin xóa user qua user CRUD page, dropdown owner sẽ stale tối đa 5 phút (staleTime). Nếu khó chịu, thêm `qc.invalidateQueries({ queryKey: ['quotations', 'owners'] })` vào delete-user mutation — không block phase này.

## Final Verification
Chạy theo thứ tự, tại root repo:

```powershell
# Backend (chỉ build các project thay đổi — KHÔNG restart WebApi đang chạy)
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj

# Backend tests (cần Postgres test fixture)
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj `
  --filter "FullyQualifiedName~QuotationListFilterTests|FullyQualifiedName~QuotationOwnersEndpointTests"

# Frontend
cd frontend
npm run typecheck
npm run lint
npm test -- --run src/features/quotations src/pages/quotations
```

Manual smoke (browser, WebApi đang chạy):
1. Login `admin / Admin@123` → /quotations: thấy filter "Chủ sở hữu", chọn 2 owner → URL có `?ownerUserIds=...`, list lọc đúng, footer totals khớp.
2. Login với account Sales (tạo qua admin UI) → /quotations: KHÔNG thấy filter "Chủ sở hữu".
3. Tạo orphan: tạo sale → tạo báo giá → admin xóa sale → reload /quotations → dropdown owner hiển thị tên sale với badge "(đã nghỉ)" cuối list, vẫn lọc ra được báo giá orphan.
4. Reload trang với URL `?ownerUserIds=<guid>` đã saved → state restored, filter active.
5. Login account Accountant (sau khi seed re-run) → có thể thấy filter và list báo giá của mọi sale.

## Rollback / Recovery
- **Backend**: revert commit. `OwnerUserIds` là optional nên không có migration DB. Seed thêm permission cho Accountant không xóa được bằng rollback code; nếu cần revoke:
  ```sql
  DELETE FROM "RolePermissions"
  WHERE "RoleId" = (SELECT "Id" FROM "Roles" WHERE "Code"='ACCOUNTANT')
    AND "PermissionId" = (SELECT "Id" FROM "Permissions" WHERE "Code"='quotations.view_all');
  ```
- **Frontend**: revert commit. URL param `?ownerUserIds=...` để lại trong bookmark sẽ bị ignore (không gãy UI).
- **Forward-fix**: nếu filter có bug nhưng cần giữ permission Accountant, comment-out chỗ render `<MultiSelect>` trong list page (1 dòng) — backend không phụ thuộc FE.
