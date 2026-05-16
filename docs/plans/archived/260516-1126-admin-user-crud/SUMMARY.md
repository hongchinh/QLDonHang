# Admin User CRUD

## Goal
Cho admin (có quyền `users.{create,update,delete}`) thực hiện trọn vòng đời user qua trang `/admin/users`: tạo mới, sửa profile + đổi role, reset password, soft-delete và bật/tắt trạng thái — không phải động vào DB.

## Scope

**In scope:**
- 5 endpoint mới dưới `/api/admin/users`: GET-detail, POST-create, PUT-update, POST-reset-password, POST-status, DELETE.
- DTOs + FluentValidation validators trong `OrderMgmt.Application/Identity/Admin/Models`.
- Mở rộng `IAdminUserService` + `AdminUserService` (Approach A — single service).
- Thêm `RevokeAllActiveForUserAsync` vào `IRefreshTokenService` để cắt session khi reset password / disable user.
- Frontend: mở rộng `features/admin-users/{api,hooks,types}.ts`, thêm 2 component dialog (`user-form-dialog`, `reset-password-dialog`) + dropdown action menu trên `users-list-page.tsx`. Thêm shadcn primitives `alert-dialog` và `dropdown-menu`.
- 5 integration test cover happy-path + edge case.

**Out of scope:**
- Multi-role per user (1 user = 1 role; service enforce).
- Email invitation / SMTP notify password.
- Audit log riêng cho thao tác user (Serilog là đủ).
- Self-service password change (không đụng `AuthController`).
- Bulk operations / import CSV.
- Migration mới (User entity không đổi).

## Assumptions
- `DbSeeder` đã grant `users.{view,create,update,delete}` cho ADMIN/MANAGER (đã verify line 115/134 — `allPermissions.Select(p => p.Code).ToArray()`). Không cần sửa seeder.
- 1 user gán đúng 1 role: enforce ở service (xoá row cũ trong `UserRoles` rồi add row mới khi đổi role). DB schema vẫn many-to-many nhưng UI/service ràng buộc cardinality = 1.
- Khi xoá user còn owner báo giá chưa cancelled / chưa soft-delete → block bằng `ConflictException` 409 với message nêu số lượng. Không tự động transfer.
- Password policy giản dị: ≥ 8 ký tự, có chữ + số (đồng nhất seed admin hiện tại; không thấy policy phức tạp trong repo).
- UI dialog đặt cùng folder `pages/admin/components/` (folder mới) — không tạo route riêng.

## Risks
- **Reset-password ↔ refresh tokens**: nếu quên revoke refresh tokens, user bị đổi password vẫn duy trì được session qua refresh → giảm bảo mật. Mitigation: refactor `RevokeFamilyAsync` (đang là `private` trong `RefreshTokenService`) thành public qua interface, dùng lại cho cả `ResetPasswordAsync`, `SetStatusAsync(Disabled)`, và `SoftDeleteAsync` (để có audit `RevokedReason` thay vì dựa side-effect).
- **Tự xoá / tự khoá**: admin đang đăng nhập có thể vô tình tự khoá tài khoản. Mitigation: service check `id == ICurrentUser.UserId` → throw `ForbiddenException` cho cả delete và set-status-to-disabled.
- **Mất admin cuối**: hệ thống mất quản trị nếu xoá / disable / đổi role admin cuối. Mitigation: trước mỗi thao tác có thể loại admin cuối (delete, set-status Disabled, update đổi role khỏi ADMIN), đếm số user khác đang Active + chưa soft-delete + còn role ADMIN — nếu = 0 → 409. Extract thành helper `EnsureNotLastActiveAdminAsync(excludedId, ct)` để 3 chỗ gọi chung.
- **Cascade soft-delete không phủ hết các bảng phụ**:
  - `RefreshTokens` cascade tự động (có nav collection trên `User`, là `ISoftDeletable`).
  - `UserRoles` **không** cascade (không phải `ISoftDeletable`) — query filter `!x.User.IsDeleted && !x.Role.IsDeleted` ở `UserConfiguration` chỉ ẩn join row khỏi query, row vẫn còn trong DB. Chấp nhận được vì không tác dụng phụ.
  - `UserQuotationSettings` **không có navigation** trên `User` entity → cascade soft-delete **không touch UQS**. Mitigation: trong `SoftDeleteAsync` load + set `IsDeleted = true` cho UQS thủ công (hoặc thêm nav `UserQuotationSettings?` lên `User` — out of scope vì cần migration).
  - Quotation Owner FK `Restrict` chỉ chặn hard-delete; với soft-delete cần check owner báo giá thủ công như đã ghi.
- **Access token sống tới khi hết TTL**: Disable / delete user chỉ chặn refresh; JWT đang phát hành vẫn dùng được tới khi access-token expire (~15 phút theo `JwtOptions`). Chấp nhận window này; không thêm token blacklist trong phase này.

## Phases
- [x] Phase 01 — Backend CRUD (M) — `phase-01-backend.md`
- [x] Phase 02 — Frontend dialogs + actions (M) — `phase-02-frontend.md`
- [x] Phase 03 — Integration tests (S) — `phase-03-tests.md`

## Final Verification

```powershell
# Backend build (theo memory: chỉ build các project đã đổi, không restart WebApi)
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj

# Tests
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserCrud"

# Frontend
cd frontend; npm run typecheck; npm run lint; npm run build
```

Manual smoke (sau khi WebApi đã hot-reload các DLL):
1. Login admin → `/admin/users` → "Thêm user" → tạo SALES → user mới đăng nhập được.
2. Edit user vừa tạo → đổi sang MANAGER → reload list, badge cập nhật.
3. Reset password → user cũ mất session (gọi `/api/auth/me` trả 401 sau khi token cũ hết hạn refresh).
4. Toggle Disabled → user không login được (tài khoản bị khoá).
5. Tạo 1 báo giá owner = X → xoá X → 409 + nút "Chuyển nhượng ngay" → transfer → xoá lại → 200.
6. Login admin Y → xoá Y → 403.

## Rollback / Recovery
- Không migration mới → rollback chỉ là revert commits.
- Permission đã seed → vẫn dùng được sau revert (không xoá seeder rows).
- Nếu chỉ muốn vô hiệu hoá UI: gỡ permission `users.{create,update,delete}` khỏi role mapping trong `DbSeeder` (sẽ cần re-seed, hoặc xoá rows `role_permissions` trực tiếp).
- Nếu lỗi ở frontend: ẩn các nút mới (gate đã có `<Can>` — chỉ cần sửa role permission).
