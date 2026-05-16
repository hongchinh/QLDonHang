# Phase 03 — Integration tests

**Status:** [x] complete
**Complexity:** S

## Objective
Bổ sung integration test cho 6 endpoint mới, cover happy-path tạo + sửa + reset password + delete + 4 edge case quan trọng. Tận dụng `QuotationTestBase` đã có (sẵn `CreateTestUserAsync`, `AuthenticateAsync`, customer/product test data).

## Files

### New
- `backend/tests/OrderMgmt.IntegrationTests/Admin/AdminUserCrudTests.cs`

## Tasks

1. Tạo file kế thừa `QuotationTestBase` (vì cần seed customer/product cho test "delete user còn báo giá"):
   ```csharp
   [Collection(nameof(PostgresCollection))]
   public class AdminUserCrudTests : QuotationTestBase
   {
       public AdminUserCrudTests(PostgresFixture pg) : base(pg) { }
       // ...
   }
   ```

2. **Test 1 — Happy path: create + retrieve**
   - POST `/api/admin/users` với payload đầy đủ (username "ut_create_ok", role SALES).
   - Assert 200, response body `AdminUserDetailDto` có Username/Email/RoleCode khớp.
   - GET `/api/admin/users/{id}` → 200, dto khớp.

3. **Test 2 — Create trùng username → 409**
   - Tạo user "dup_user" qua `CreateTestUserAsync`.
   - POST tạo lại với username "dup_user" → assert `HttpStatusCode.Conflict`, body error message chứa "tồn tại".

4. **Test 3 — Update đổi role → UserRoles còn đúng 1 row**
   - Create user role SALES (qua endpoint POST).
   - PUT đổi role → MANAGER.
   - Query DB qua `_factory.Services` scope → `db.UserRoles.Where(ur => ur.UserId == id).CountAsync()` = 1, role code = MANAGER.

5. **Test 4 — Reset password → BCrypt verify pass + refresh tokens revoked**
   - Tạo user "ut_reset" + login để issue refresh token (call `/api/auth/login`).
   - DB scope: assert có `RefreshTokens.Count(rt => rt.UserId == ... && rt.RevokedAt == null) == 1`.
   - Authenticate lại làm admin (`AuthenticateAsync("admin", "Admin@123")`).
   - POST `/api/admin/users/{id}/reset-password` newPassword "NewPass@123".
   - Assert 200.
   - Login với password mới → 200; login với password cũ → 401.
   - DB: `RefreshTokens.Count(rt => rt.UserId == ... && rt.RevokedAt == null) == 0` (token cũ đã revoke); `RevokedReason == "PASSWORD_RESET"`.

6. **Test 5 — Delete user còn owner báo giá → 409**
   - Tạo user "ut_owner" role SALES.
   - Login user đó → tạo 1 quotation (dùng `BuildRequest()` từ base).
   - Authenticate lại admin.
   - DELETE `/api/admin/users/{id}` → assert 409, body `Error.Code == "CONFLICT"` (tránh assert substring i18n).

7. **Test 6 — Delete chính mình → 403**
   - Đang login admin → lấy admin id từ DB → DELETE chính id đó → assert `HttpStatusCode.Forbidden`.

8. **Test 7 — SetStatus Disabled chính mình → 403** (nhỏ, cùng pattern):
   - Admin → POST `/api/admin/users/{adminId}/status` body `{ status: Disabled }` → 403.

9. **Test 8 — SetStatus Disabled user khác → user không login được**
   - Tạo user "ut_disable" role SALES (login OK trước).
   - Admin POST status Disabled → 200.
   - Login lại với "ut_disable" → 401.
   - DB: refresh tokens `RevokedReason == "USER_DISABLED"`.

10. **Test 9 — Create với JSON thiếu field `status` → user mặc định Active** (regression cho bug default enum):
    - POST `/api/admin/users` với body bỏ field `status` (gửi JSON object thiếu key).
    - Assert 200, response `Status == UserStatus.Active`. (Confirm DTO explicit default đã đặt đúng.)

11. **Test 10 — Soft-delete user có UQS → UQS bị soft-delete theo**:
    - Tạo user "ut_uqs" role SALES.
    - Tạo UQS row cho user qua DB scope hoặc endpoint `/api/admin/user-settings/...` nếu có.
    - DELETE user → 200.
    - DB scope (qua `IgnoreQueryFilters`): assert `UserQuotationSettings.First(s => s.UserId == id).IsDeleted == true`.

12. **Test 11 — Soft-delete user còn refresh token → token revoked với reason `USER_DELETED`**:
    - Tạo + login user "ut_del_revoke" để có active refresh token.
    - Admin DELETE user → 200.
    - DB scope: `RefreshTokens.IgnoreQueryFilters().First(rt => rt.UserId == id).RevokedReason == "USER_DELETED"`.

## Verification

```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserCrudTests" --logger "console;verbosity=normal"
```

Expect: tất cả 11 test pass. Nếu fail, log của xUnit + Postgres fixture đủ chi tiết để debug.

## Exit Criteria

- File `AdminUserCrudTests.cs` tồn tại với 11 test (8 happy/edge gốc + 3 mới: default-status regression, UQS soft-delete, refresh-token revoke on delete).
- `dotnet test` filter trên: 11 passed, 0 failed.
- Existing test `AdminUsersListTests` vẫn pass (regression check).
- Không thêm test "delete admin cuối cùng → 409" vì test infra default seed chỉ có 1 admin → setup gốc "1 admin còn lại" trùng với case `Self-delete → 403`. Last-admin guard (xoá admin khác trong khi mình cũng là admin) cần seed admin thứ 2 → out of scope phase này; bổ sung sau nếu cần (dùng DB scope tạo admin#2 active, login admin#2, delete admin gốc → expect 409 nếu admin#1 là target last-active sau khi admin#2 bị mocked Inactive). Ghi chú vào backlog.
