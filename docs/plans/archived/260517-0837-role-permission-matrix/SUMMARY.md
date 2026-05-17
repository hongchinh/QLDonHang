# Role × Permission Matrix Management

## Goal

Cung cấp màn hình `/admin/roles` cho user có permission `roles.manage` (mặc định ADMIN + MANAGER) để quản lý ma trận Role × Permission và CRUD custom role. Backend giữ nguyên data model (Role / Permission / RolePermission đã có sẵn), thêm `AdminRolesController` + `IAdminRoleService`, sửa `DbSeeder.SeedRolesAsync` để KHÔNG ghi đè permission đã chỉnh của system role khác ngoài ADMIN. Frontend dùng matrix table 2 chiều (permissions theo module × roles), save per-role diff bằng `Promise.allSettled` (partial-failure tolerance — 1 role lỗi không nuốt update của các role còn lại). UI tái dùng `ConfirmDialog` + native `<input type="checkbox">` để không thêm dep radix mới.

## Scope

**In scope**:
- Backend service + 6 endpoint REST + validators + integration tests.
- DbSeeder behavior change cho `SeedRolesAsync` + test phòng regression.
- Frontend feature `admin-roles` (api/hooks/types/keys) + matrix page + dialogs (create/rename/delete).
- Wire route `/admin/roles` (protected bằng `roles.view`), thêm vào sidebar.
- Thêm `roles.view` / `roles.manage` vào frontend `lib/permissions.ts` PERMISSIONS const.
- Cập nhật `docs/architecture/system-architecture.md` section Authorization với note DbSeeder behavior change.

**Out of scope**:
- Per-user permission override (RBAC thuần).
- Multi-role per user (không động `AdminUserService.UpdateAsync`).
- Force-logout user khi đổi role permission (chờ token refresh ~60p — đã verify `RefreshTokenService.RotateAsync` re-load từ DB).
- Đổi `Code` system role / xoá system role.
- Migration database.

## Assumptions

- `RefreshTokenService.RotateAsync` re-load `RolePermissions` từ DB mỗi refresh (đã verify [backend/src/OrderMgmt.Infrastructure/Identity/RefreshTokenService.cs:64](../../../backend/src/OrderMgmt.Infrastructure/Identity/RefreshTokenService.cs#L64)) → không cần backend đổi gì cho live-update path.
- Permission `roles.view` và `roles.manage` đã được seed (cả 2 hiện gán cho ADMIN và MANAGER theo [DbSeeder.cs:115,134](../../../backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs#L115)).
- `RolePermission` là pure join entity (không `BaseEntity` / `ISoftDeletable`) → cascade soft-delete của `AppDbContext` **bỏ qua** nó (verified [AppDbContext.cs:130](../../../backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs#L130): `if (!typeof(ISoftDeletable).IsAssignableFrom(targetType)) continue;`). Vì vậy `DeleteAsync` của role phải **hard-delete** các `RolePermission` rows trước khi soft-delete role.
- Quotation owner scope không ảnh hưởng (role không phải resource có owner).
- `Permission.Module` chỉ có 4 giá trị: `system`, `catalog`, `sales`, `report` (đã hardcode trong [Permissions.cs:5-8](../../../backend/src/OrderMgmt.Domain/Constants/Permissions.cs#L5-L8)).
- Team admin nhỏ → last-write-wins khi concurrent edit là chấp nhận được (không thêm optimistic concurrency).
- Frontend chưa có shadcn `<Checkbox>`, `<Tooltip>`, `<AlertDialog>` và chưa cài radix tương ứng → dùng native `<input type="checkbox">` + tái dùng [ConfirmDialog](../../../frontend/src/components/ui/confirm-dialog.tsx) cho destructive prompts; ADMIN column note thay tooltip bằng caption dưới header.

## Risks

| Risk | Mitigation |
|---|---|
| Admin lỡ bỏ `roles.manage` khỏi role của chính mình | UI hiện warning toast trước save; ADMIN role luôn full quyền nên vẫn có đường cứu |
| Deploy lên DB cũ → seeder reset permission đã chỉnh | Phase 01 đổi `SeedRolesAsync` + thêm test verify permission custom KHÔNG bị reset |
| Permission code khoá cứng `RoleCodes.*` (Admin/Sales/...) bị admin trùng khi tạo custom | Validator reject Code trùng (case-insensitive) với 5 system codes |
| Concurrent edits 2 admin cùng role | Last-write-wins; chấp nhận trong scope hiện tại |
| Xoá role đang được user dùng | Service throw `ConflictException` với message "Role này đang được gán cho N user..." |

## Phases

- [x] Phase 01 — Backend service + API + seeder + tests (L) — [phase-01-backend.md](phase-01-backend.md) — _build pass; integration tests deferred (Docker unavailable)_
- [x] Phase 02 — Frontend feature module + matrix page + tests (L) — [phase-02-frontend-feature.md](phase-02-frontend-feature.md)
- [x] Phase 03 — Wire route + sidebar + doc + smoke (S) — [phase-03-wire-route-doc.md](phase-03-wire-route-doc.md)

## Final Verification

```powershell
# Backend
dotnet build backend/OrderMgmt.sln
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~AdminRoles|FullyQualifiedName~DbSeederUpgrade"

# Frontend
cd frontend
npm run lint
npx tsc -b
npm test -- --run route-permissions roles-matrix-page

# Manual smoke (dev server, login as admin):
# 1. /admin/roles loads matrix, ADMIN column disabled
# 2. Toggle SALES.quotations.delete → save → reload → vẫn còn
# 3. Tạo custom role "TEST_LEAD", gán vài permission → save
# 4. Tạo user gán role TEST_LEAD → user đó login thấy menu phù hợp
# 5. Try delete TEST_LEAD khi còn user → expect 409
# 6. Transfer user về role khác → delete TEST_LEAD OK
# 7. Login user thường (SALES không có roles.view) → /admin/roles ẩn khỏi sidebar và 403 nếu force navigate
```

## Rollback / Recovery

- Phase nào fail thì revert chỉ commit của phase đó (mỗi phase tự đứng).
- DbSeeder change ở Phase 01 là backward compatible (chỉ thay đổi logic skip thay vì add) — rollback bằng revert.
- Không có migration database → rollback không cần touch DB.
- Frontend route mới: nếu cần ẩn tạm, comment block `<Route path="admin/roles" ...>` trong `App.tsx` + comment nav item.
