# Phase 03 — Integration tests

**Status:** [ ] pending
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
   - DELETE `/api/admin/users/{id}` → assert 409, body error message chứa số "1" và từ "báo giá".

7. **Test 6 — Delete chính mình → 403**
   - Đang login admin → lấy admin id từ DB → DELETE chính id đó → assert `HttpStatusCode.Forbidden`.

8. **Test 7 — SetStatus Disabled chính mình → 403** (nhỏ, cùng pattern):
   - Admin → POST `/api/admin/users/{adminId}/status` body `{ status: Disabled }` → 403.

9. **Test 8 — SetStatus Disabled user khác → user không login được**
   - Tạo user "ut_disable" role SALES (login OK trước).
   - Admin POST status Disabled → 200.
   - Login lại với "ut_disable" → 401.
   - DB: refresh tokens `RevokedReason == "USER_DISABLED"`.

## Verification

```powershell
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUserCrudTests" --logger "console;verbosity=normal"
```

Expect: tất cả 8 test pass. Nếu fail, log của xUnit + Postgres fixture đủ chi tiết để debug.

## Exit Criteria

- File `AdminUserCrudTests.cs` tồn tại với 8 test.
- `dotnet test` filter trên: 8 passed, 0 failed.
- Existing test `AdminUsersListTests` vẫn pass (regression check).
- Không thêm test "admin cuối cùng" vì test infra default seed chỉ có 1 admin → khó dựng case "admin cuối còn nhiều user khác active". Nếu muốn cover, cần seed thêm admin thứ 2 trong test → out of scope phase này; bổ sung sau nếu user yêu cầu.
