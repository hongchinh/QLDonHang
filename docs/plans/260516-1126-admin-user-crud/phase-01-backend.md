# Phase 01 — Backend CRUD

**Status:** [ ] pending
**Complexity:** M

## Objective
Thêm 5 endpoint CRUD cho user dưới `/api/admin/users`, mở rộng `AdminUserService` và `IRefreshTokenService` để hỗ trợ revoke session khi reset password / disable user. DTOs + validators tuân theo pattern hiện có (Quotation/Customer).

## Files

### New
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/AdminUserDetailDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/CreateUserRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/UpdateUserRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/ResetPasswordRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/SetUserStatusRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/CreateUserRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/UpdateUserRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/ResetPasswordRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/SetUserStatusRequestValidator.cs`

### Modified
- `backend/src/OrderMgmt.Application/Identity/Admin/Interfaces/IAdminUserService.cs` — thêm 6 method (Get + 5 mutate).
- `backend/src/OrderMgmt.Application/Identity/Admin/Services/AdminUserService.cs` — implement.
- `backend/src/OrderMgmt.Application/Identity/Interfaces/IRefreshTokenService.cs` — thêm `RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken ct)`.
- `backend/src/OrderMgmt.Infrastructure/Identity/RefreshTokenService.cs` — implement method mới.
- `backend/src/OrderMgmt.WebApi/Controllers/AdminUsersController.cs` — thêm 5 action method.

## Tasks

### DTO files (5 file mới)

1. `AdminUserDetailDto`: `Id (Guid)`, `Username (string)`, `Email (string)`, `FullName (string)`, `PhoneNumber (string?)`, `RoleCode (string?)`, `Status (UserStatus)`, `IsDeleted (bool)`, `LastLoginAt (DateTimeOffset?)`, `CreatedAt (DateTimeOffset)`, `UpdatedAt (DateTimeOffset?)`.
2. `CreateUserRequest`: `Username, Email, FullName, PhoneNumber?, RoleCode, Password, Status` (mặc định `UserStatus.Active`).
3. `UpdateUserRequest`: `FullName, Email, PhoneNumber?, RoleCode, Status` (Username không có).
4. `ResetPasswordRequest`: `NewPassword (string)`.
5. `SetUserStatusRequest`: `Status (UserStatus)`.

### Validators (4 file mới — không cần validator cho `AdminUserDetailDto`)

6. `CreateUserRequestValidator`:
   - `Username`: `NotEmpty`, length 3..50, regex `^[a-zA-Z0-9._-]+$`.
   - `Email`: `NotEmpty`, `EmailAddress`, max 255.
   - `FullName`: `NotEmpty`, max 200.
   - `PhoneNumber`: optional, max 20.
   - `RoleCode`: `NotEmpty`, max 50.
   - `Password`: `NotEmpty`, min 8, regex `(?=.*[A-Za-z])(?=.*\d)`.
   - `Status`: `IsInEnum<UserStatus>()`.
7. `UpdateUserRequestValidator`: giống Create nhưng bỏ `Username` và `Password`.
8. `ResetPasswordRequestValidator`: `NewPassword` rule giống `Password` ở Create.
9. `SetUserStatusRequestValidator`: `Status.IsInEnum<UserStatus>()`.

### IRefreshTokenService extension

10. Thêm signature vào `IRefreshTokenService`:
    ```csharp
    Task<int> RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken ct = default);
    ```
11. Implement trong `RefreshTokenService` (Infrastructure):
    - Query `_db.RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > now)`.
    - Đặt `RevokedAt = _dateTime.UtcNow`, `RevokedReason = reason`.
    - `await _db.SaveChangesAsync(ct)`. Return số lượng đã revoke.
    - Dùng `ICurrentUser` / `IDateTime` đã có sẵn trong service constructor (kiểm tra signature hiện tại trước khi viết).

### IAdminUserService extension (interface)

12. Thêm vào `IAdminUserService`:
    ```csharp
    Task<AdminUserDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<AdminUserDetailDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default);
    Task<AdminUserDetailDto> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest req, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, SetUserStatusRequest req, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    ```

### AdminUserService implementation

13. Inject thêm: `IPasswordHasher`, `IRefreshTokenService`, `ICurrentUser`. (Đã có `IAppDbContext`.)

14. **`GetAsync`**:
    - `IgnoreQueryFilters()`, include `UserRoles.ThenInclude(ur => ur.Role)`.
    - `FirstOrDefaultAsync(u => u.Id == id)` → null → `throw new NotFoundException("User", id)`.
    - Map sang `AdminUserDetailDto` (RoleCode = first role code).

15. **`CreateAsync`**:
    - Check trùng `Username` (chưa soft-delete): `_db.Users.AnyAsync(u => u.Username == req.Username)` (default query filter loại IsDeleted) → throw `ConflictException("Username '{req.Username}' đã tồn tại.")`.
    - Check trùng `Email` tương tự.
    - Resolve role: `_db.Roles.FirstOrDefaultAsync(r => r.Code == req.RoleCode)` → null → `throw new ConflictException("Role '{req.RoleCode}' không tồn tại.")`.
    - Tạo `User` entity với `PasswordHash = hasher.Hash(req.Password)`, `Status = req.Status`, `UserRoles = new List<UserRole> { new() { RoleId = role.Id } }`.
    - `db.Users.Add(user)` → `SaveChangesAsync`.
    - Return `await GetAsync(user.Id, ct)`.

16. **`UpdateAsync`**:
    - Load user (with UserRoles) — KHÔNG `IgnoreQueryFilters` (không cho update user đã soft-delete) → null → `NotFoundException`.
    - Check email trùng nếu thay đổi: `AnyAsync(u => u.Id != id && u.Email == req.Email)` → 409.
    - Set `FullName`, `Email`, `PhoneNumber`, `Status`.
    - Đổi role: nếu `req.RoleCode != currentRoleCode` → resolve role mới (404 nếu không có); xoá tất cả `UserRoles` hiện tại (`_db.UserRoles.RemoveRange(user.UserRoles)`); add `new UserRole { UserId = user.Id, RoleId = role.Id }`.
    - Nếu user vừa bị set Status = Disabled → `await _refreshTokenService.RevokeAllActiveForUserAsync(id, "USER_DISABLED", ct)`.
    - `SaveChangesAsync`. Return `await GetAsync(id, ct)`.

17. **`ResetPasswordAsync`**:
    - Load user (no IgnoreQueryFilters) → 404.
    - `user.PasswordHash = hasher.Hash(req.NewPassword)`.
    - `await _refreshTokenService.RevokeAllActiveForUserAsync(id, "PASSWORD_RESET", ct)`.
    - `SaveChangesAsync`.

18. **`SetStatusAsync`**:
    - Self-guard: nếu `id == _currentUser.UserId` và `req.Status == UserStatus.Disabled` → `throw new ForbiddenException("Không thể tự khoá tài khoản đang đăng nhập.")`.
    - Load user → 404.
    - Set `Status`. Nếu Disabled → revoke refresh tokens (reason `"USER_DISABLED"`).
    - `SaveChangesAsync`.

19. **`SoftDeleteAsync`**:
    - Self-guard: `id == _currentUser.UserId` → `ForbiddenException("Không thể tự xoá tài khoản đang đăng nhập.")`.
    - Load user (default filter — đã xoá thì NotFound).
    - Đếm báo giá owner đang active: `var ownedCount = await _db.Quotations.CountAsync(q => q.OwnerUserId == id, ct)` (default query filter đã loại IsDeleted; cần loại Cancelled? — theo brainstorm "chưa cancelled, chưa soft-delete" → `q.Status != QuotationStatus.Cancelled`).
    - Nếu `ownedCount > 0` → `throw new ConflictException($"Người dùng còn {ownedCount} báo giá đang sở hữu, vui lòng chuyển nhượng trước khi xoá.")`.
    - Đếm admin còn active: nếu user đang xoá có role ADMIN, kiểm tra số admin active còn lại:
      ```csharp
      var adminCount = await _db.Users
          .CountAsync(u => u.Id != id
              && u.Status == UserStatus.Active
              && u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin), ct);
      if (userIsAdmin && adminCount == 0)
          throw new ConflictException("Không thể xoá admin cuối cùng của hệ thống.");
      ```
    - Set `user.IsDeleted = true` (cascade qua `AppDbContext.SaveChanges` sẽ tự lo `UserRoles`, `RefreshTokens`, `UserQuotationSettings`).
    - `SaveChangesAsync`.

### Controller

20. Sửa `AdminUsersController.cs`:
    - Inject service đã có (không đổi DI).
    - List endpoint hiện tại: **đổi `[HasPermission(UserSettings.Manage)]` → `[HasPermission(Users.View)]`** để nhất quán (UserSettings.Manage giữ riêng cho `/api/admin/user-settings/*`).
    - Thêm 5 action:
      ```csharp
      [HttpGet("/api/admin/users/{id:guid}")] [HasPermission(Permissions.Users.View)]
      Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Get(Guid id, CancellationToken ct);

      [HttpPost("/api/admin/users")] [HasPermission(Permissions.Users.Create)]
      Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Create([FromBody] CreateUserRequest req, CancellationToken ct);

      [HttpPut("/api/admin/users/{id:guid}")] [HasPermission(Permissions.Users.Update)]
      Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct);

      [HttpPost("/api/admin/users/{id:guid}/reset-password")] [HasPermission(Permissions.Users.Update)]
      Task<ActionResult<ApiResponse>> ResetPassword(Guid id, [FromBody] ResetPasswordRequest req, CancellationToken ct);

      [HttpPost("/api/admin/users/{id:guid}/status")] [HasPermission(Permissions.Users.Update)]
      Task<ActionResult<ApiResponse>> SetStatus(Guid id, [FromBody] SetUserStatusRequest req, CancellationToken ct);

      [HttpDelete("/api/admin/users/{id:guid}")] [HasPermission(Permissions.Users.Delete)]
      Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct);
      ```
    - Mỗi action: gọi service, trả `Success(...)` hoặc `Success()` cho void op.

21. **Verify** existing tests `AdminUsersListTests` vẫn pass sau khi đổi permission từ `UserSettings.Manage` sang `Users.View` (admin có cả 2 nên OK; sales test giả định 403 vẫn 403 vì sales không có `Users.View`).

## Verification

```powershell
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --filter "FullyQualifiedName~AdminUsersListTests"
```

Manual: mở Swagger `/swagger`, gọi `POST /api/admin/users` với body hợp lệ → 200 + dto. Gọi lại với username trùng → 409. Gọi `DELETE /api/admin/users/{adminId}` (id của user đang login) → 403.

## Exit Criteria

- 5 file DTO + 4 file validator được tạo, validator được auto-register qua `AddValidatorsFromAssembly` (đã có trong `Application/DependencyInjection.cs`).
- `IAdminUserService` có 6 method mới; `AdminUserService` implement đầy đủ với inject `IPasswordHasher`, `IRefreshTokenService`, `ICurrentUser`.
- `IRefreshTokenService.RevokeAllActiveForUserAsync` thêm vào và implement xong.
- `AdminUsersController` có 6 action (1 cũ + 5 mới); permission gating đúng.
- Build pass cả 3 project; existing test `AdminUsersListTests` vẫn xanh.
- Swagger spot-check 3 endpoint chính (create, update, delete) trả status code đúng.
