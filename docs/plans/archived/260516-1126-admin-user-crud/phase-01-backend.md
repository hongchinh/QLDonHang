# Phase 01 — Backend CRUD

**Status:** [x] complete
**Complexity:** M

## Objective
Thêm 5 endpoint CRUD cho user dưới `/api/admin/users`, mở rộng `AdminUserService` và `IRefreshTokenService` để hỗ trợ revoke session khi reset password / disable user / soft-delete. DTOs + validators tuân theo pattern hiện có (Quotation/Customer).

**Lưu ý thực thi quan trọng** (rút từ review):
- `UserStatus.Disabled = 0`, `Active = 1` → field `Status` trong DTO phải explicit default `= UserStatus.Active`, nếu không user tạo mới với field vắng mặt sẽ là Disabled.
- `RefreshTokenService` đã có sẵn `private RevokeFamilyAsync(Guid userId, string reason, ...)` (xem `RefreshTokenService.cs:134`). Refactor thành public qua interface thay vì viết method mới song song.
- Cascade soft-delete trên `User` chỉ chạm `RefreshTokens` (có nav + `ISoftDeletable`). `UserRoles` không phải `ISoftDeletable` (chỉ bị query-filter ẩn). `UserQuotationSettings` không có nav từ `User` → phải xử lý thủ công trong `SoftDeleteAsync`.

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
2. `CreateUserRequest`: `Username, Email, FullName, PhoneNumber?, RoleCode, Password, Status`.
   - **Quan trọng**: khai báo `public UserStatus Status { get; set; } = UserStatus.Active;` (explicit default). Lý do: `Disabled = 0` là default của int → field vắng mặt trong JSON sẽ deserialize ra `Disabled`. `JsonStringEnumConverter` không xử lý case field missing.
3. `UpdateUserRequest`: `FullName, Email, PhoneNumber?, RoleCode, Status` (Username không có).
   - `Status` bắt buộc client phải gửi (frontend luôn fill từ `useAdminUserDetail`); validator dùng `IsInEnum<UserStatus>()` là đủ vì cả 2 giá trị 0/1 đều hợp lệ — không cần default.
4. `ResetPasswordRequest`: `NewPassword (string)`.
5. `SetUserStatusRequest`: `Status (UserStatus)`.

### Validators (4 file mới — không cần validator cho `AdminUserDetailDto`)

6. `CreateUserRequestValidator`:
   - `Username`: `NotEmpty`, length 3..50, regex `^[a-zA-Z0-9._-]+$`. (DB column hiện là 100 — chấp nhận stricter ở app layer; nếu muốn sync DB xuống 50 cần migration → out of scope.)
   - `Email`: `NotEmpty`, `EmailAddress`, max 255.
   - `FullName`: `NotEmpty`, max 200. (DB là 255 — app layer stricter.)
   - `PhoneNumber`: optional, max 20. (DB là 30 — app layer stricter.)
   - `RoleCode`: `NotEmpty`, max 50.
   - `Password`: `NotEmpty`, min 8, regex `(?=.*[A-Za-z])(?=.*\d)`.
   - `Status`: `IsInEnum<UserStatus>()`.
7. `UpdateUserRequestValidator`: giống Create nhưng bỏ `Username` và `Password`.
8. `ResetPasswordRequestValidator`: `NewPassword` rule giống `Password` ở Create.
9. `SetUserStatusRequestValidator`: `Status.IsInEnum<UserStatus>()`.

### IRefreshTokenService extension (refactor, KHÔNG duplicate)

10. `RefreshTokenService.cs:134` đã có `private async Task RevokeFamilyAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken ct)` được gọi từ `RotateAsync` (path reuse-detection, save chung với caller).
    - **Refactor**: đổi signature thành public và đưa lên interface:
      ```csharp
      // IRefreshTokenService
      Task<int> RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken ct = default);
      ```
    - Implementation public mới: query `_db.RefreshTokens.Where(rt => rt.UserId == userId && rt.RevokedAt == null)` (giữ filter giống bản hiện tại — KHÔNG thêm `ExpiresAt > now`; token hết hạn cũng có thể revoke để có audit trail, hoặc kệ — chọn 1 đường, mặc định KHÔNG filter expired để giữ đồng bộ với bản private cũ).
    - Set `RevokedAt = _clock.UtcNow`, `RevokedReason = reason`. `await _db.SaveChangesAsync(ct)`. Return count.
    - **Refactor lại `RotateAsync`** (call site cũ): vì bản public auto-save, nhưng `RotateAsync` đang gom save vào cuối. Có 2 lựa chọn:
       - (A) Giữ `RevokeFamilyAsync` private (no-save) và thêm public `RevokeAllActiveForUserAsync` gọi private rồi save. Ít tác động.
       - (B) Bỏ private, public method auto-save, `RotateAsync` gọi public rồi tiếp tục thay đổi state khác (EF gộp lệnh trong cùng transaction OK nhưng round-trip thừa 1 lần).
    - **Chọn (A)** — minimal-risk: private helper giữ nguyên, public wrapper save xong return count.
    - Dùng `_clock` (`IDateTime`) đã inject sẵn ở constructor (xem `RefreshTokenService.cs:25`).

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
    - Load user (with `UserRoles.ThenInclude(ur => ur.Role)`) — KHÔNG `IgnoreQueryFilters` (không cho update user đã soft-delete) → null → `NotFoundException`.
    - Check email trùng nếu thay đổi: `AnyAsync(u => u.Id != id && u.Email == req.Email)` → 409.
    - Capture `wasAdmin = user.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin)` và `wasActive = user.Status == UserStatus.Active` **trước khi** mutate.
    - Set `FullName`, `Email`, `PhoneNumber`, `Status`.
    - Đổi role: nếu `req.RoleCode != currentRoleCode` → resolve role mới (404 nếu không có); xoá `UserRoles` hiện tại (`_db.UserRoles.RemoveRange(user.UserRoles)`) rồi add `new UserRole { UserId = user.Id, RoleId = role.Id }`. **Không** chạm UserRoles nếu role không đổi (tránh EF tracking xung đột với composite key `(UserId, RoleId)` không đổi).
    - **Last-admin guard cho update**: nếu `wasAdmin && wasActive` và (`req.RoleCode != RoleCodes.Admin` hoặc `req.Status == UserStatus.Disabled`) → gọi `EnsureNotLastActiveAdminAsync(excludedId: id, ct)` (xem helper #19a).
    - Nếu user vừa bị set Status = Disabled (`wasActive && req.Status == UserStatus.Disabled`) → `await _refreshTokenService.RevokeAllActiveForUserAsync(id, "USER_DISABLED", ct)`.
    - `SaveChangesAsync`. Return `await GetAsync(id, ct)`.

17. **`ResetPasswordAsync`**:
    - Load user (no IgnoreQueryFilters) → 404.
    - `user.PasswordHash = hasher.Hash(req.NewPassword)`.
    - `await _refreshTokenService.RevokeAllActiveForUserAsync(id, "PASSWORD_RESET", ct)`.
    - `SaveChangesAsync`.

18. **`SetStatusAsync`**:
    - Self-guard: nếu `id == _currentUser.UserId` và `req.Status == UserStatus.Disabled` → `throw new ForbiddenException("Không thể tự khoá tài khoản đang đăng nhập.")`.
    - Load user (with `UserRoles.ThenInclude(ur => ur.Role)`) → 404.
    - Last-admin guard: nếu chuyển từ Active → Disabled và user có role ADMIN → `await EnsureNotLastActiveAdminAsync(excludedId: id, ct)`.
    - Set `Status`. Nếu Disabled → `await _refreshTokenService.RevokeAllActiveForUserAsync(id, "USER_DISABLED", ct)`.
    - `SaveChangesAsync`.

19. **`SoftDeleteAsync`**:
    - Self-guard: `id == _currentUser.UserId` → `ForbiddenException("Không thể tự xoá tài khoản đang đăng nhập.")`.
    - Load user (default filter — đã xoá thì NotFound), include `UserRoles.ThenInclude(ur => ur.Role)`.
    - Đếm báo giá owner đang active: `var ownedCount = await _db.Quotations.CountAsync(q => q.OwnerUserId == id && q.Status != QuotationStatus.Cancelled, ct)` (default query filter đã loại `IsDeleted`).
    - Nếu `ownedCount > 0` → `throw new ConflictException($"Người dùng còn {ownedCount} báo giá đang sở hữu, vui lòng chuyển nhượng trước khi xoá.")`.
    - Last-admin guard: nếu user đang xoá có role ADMIN và đang Active → `await EnsureNotLastActiveAdminAsync(excludedId: id, ct)`.
    - **Soft-delete UQS thủ công** (vì `User` entity không có nav `UserQuotationSettings` → cascade trong `AppDbContext.SaveChangesAsync` không touch UQS):
      ```csharp
      var uqs = await _db.UserQuotationSettings.FirstOrDefaultAsync(s => s.UserId == id, ct);
      if (uqs is not null) uqs.IsDeleted = true;
      ```
    - Revoke refresh tokens để có audit (`RevokedReason = "USER_DELETED"`) thay vì dựa side-effect của cascade soft-delete RefreshToken (cascade chỉ set `IsDeleted`, không set `RevokedAt`/`RevokedReason`):
      ```csharp
      await _refreshTokenService.RevokeAllActiveForUserAsync(id, "USER_DELETED", ct);
      ```
    - Set `user.IsDeleted = true`. Cascade `AppDbContext.SaveChanges` sẽ tự lan `IsDeleted` xuống `RefreshTokens` (collection nav + `ISoftDeletable`). `UserRoles` không cần cascade — query filter `!x.User.IsDeleted && !x.Role.IsDeleted` đã ẩn join row khỏi mọi query.
    - `SaveChangesAsync`.

19a. **Helper `EnsureNotLastActiveAdminAsync(Guid excludedId, CancellationToken ct)`** (private trong `AdminUserService`):
    ```csharp
    private async Task EnsureNotLastActiveAdminAsync(Guid excludedId, CancellationToken ct)
    {
        var otherActiveAdmins = await _db.Users
            .CountAsync(u => u.Id != excludedId
                && u.Status == UserStatus.Active
                && u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Admin), ct);
        if (otherActiveAdmins == 0)
            throw new ConflictException("Không thể thao tác — đây là quản trị viên Active duy nhất của hệ thống.");
    }
    ```
    Gọi từ `SoftDeleteAsync`, `SetStatusAsync` (Active→Disabled trên admin), `UpdateAsync` (admin đổi role khỏi ADMIN hoặc bị Disable).

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
- `CreateUserRequest.Status` có explicit default `= UserStatus.Active`. (Verify: tạo user với JSON thiếu field `status` → user tạo ra ở trạng thái Active.)
- `IAdminUserService` có 6 method mới + helper `EnsureNotLastActiveAdminAsync`; `AdminUserService` implement đầy đủ với inject `IPasswordHasher`, `IRefreshTokenService`, `ICurrentUser`.
- `IRefreshTokenService.RevokeAllActiveForUserAsync` thêm vào interface, public implement trong `RefreshTokenService` gọi private `RevokeFamilyAsync` rồi save. Không duplicate logic revoke.
- `AdminUsersController` có 6 action (1 cũ + 5 mới); permission gating đúng (`Users.View` cho list+get, `Users.Create/Update/Delete` tương ứng).
- `SoftDeleteAsync` xử lý `UserQuotationSettings` thủ công (soft-delete) và revoke refresh tokens với reason `USER_DELETED`.
- Build pass cả 3 project; existing test `AdminUsersListTests` vẫn xanh sau khi đổi permission `UserSettings.Manage → Users.View`.
- Swagger spot-check 3 endpoint chính (create, update, delete) trả status code đúng.
