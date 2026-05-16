# Phase 01 — Backend list users endpoint

**Status:** [x] complete
**Complexity:** S

## Objective

Bổ sung endpoint `GET /api/admin/users` để FE picker chọn user vào trang per-user-settings và bulk-transfer. Tuân theo pattern Application service + Controller + DTO + Integration test đã có trong repo.

## Files

**New**
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/AdminUserListItemDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/AdminUserListQuery.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Interfaces/IAdminUserService.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Services/AdminUserService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/AdminUsersController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Admin/AdminUsersListTests.cs`

**Modified**
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (nơi đang register `IUserQuotationSettingsService`). Add `AddScoped<IAdminUserService, AdminUserService>()`.

## Tasks

### DTO + Query model

1. Tạo `AdminUserListItemDto`:
   ```csharp
   namespace OrderMgmt.Application.Identity.Admin.Models;

   public class AdminUserListItemDto
   {
       public Guid Id { get; set; }
       public string Username { get; set; } = default!;
       public string FullName { get; set; } = default!;
       public string? RoleCode { get; set; }   // role chính (đầu tiên theo Role.Code asc)
       public bool IsActive { get; set; }       // !IsDeleted && Status == Active
       public DateTimeOffset? LastLoginAt { get; set; }
   }
   ```
   Không trả `Email`, `PhoneNumber`, `PasswordHash`.

2. Tạo `AdminUserListQuery`:
   ```csharp
   public class AdminUserListQuery
   {
       public string? Search { get; set; }    // contains (case-insensitive) username OR fullname
       public bool ActiveOnly { get; set; } = false; // default false để hiện cả inactive
   }
   ```

### Service

3. Tạo `IAdminUserService` interface:
   ```csharp
   public interface IAdminUserService
   {
       Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
   }
   ```

4. Implement `AdminUserService` (file `AdminUserService.cs`):
   - Constructor inject `IAppDbContext _db`.
   - Method `ListAsync`:
     - Query base: `_db.Users.IgnoreQueryFilters().Include(u => u.UserRoles).ThenInclude(ur => ur.Role)`.
     - Nếu `query.ActiveOnly == true`: filter `u => !u.IsDeleted && u.Status == UserStatus.Active`.
     - Nếu `query.Search` non-empty: filter theo username/fullname, case-insensitive và không dấu cho `FullName`, theo pattern `CustomersService`:
       ```csharp
       var pattern = $"%{query.Search.Trim()}%";
       queryable = queryable.Where(u =>
           EF.Functions.ILike(u.Username, pattern)
           || EF.Functions.ILike(EF.Functions.Unaccent(u.FullName), EF.Functions.Unaccent(pattern)));
       ```
     - OrderBy `u.Username`.
     - Project tới DTO: `IsActive = !u.IsDeleted && u.Status == UserStatus.Active`, `RoleCode = u.UserRoles.OrderBy(ur => ur.Role.Code).Select(ur => ur.Role.Code).FirstOrDefault()`.
     - `ToListAsync(ct)`.

   *Note*: `CustomersService` đang dùng `EF.Functions.ILike` + `EF.Functions.Unaccent` cho search tiếng Việt; giữ pattern đó cho `FullName`.

   4a. Soát `CustomersService` (hoặc service search nào dùng `Contains`/`ILike`) để xác nhận pattern.

### Controller

5. Tạo `AdminUsersController`:
   ```csharp
   using Microsoft.AspNetCore.Mvc;
   using OrderMgmt.Application.Common.Models;
   using OrderMgmt.Application.Identity.Admin.Interfaces;
   using OrderMgmt.Application.Identity.Admin.Models;
   using OrderMgmt.Domain.Constants;
   using OrderMgmt.WebApi.Authorization;

   namespace OrderMgmt.WebApi.Controllers;

   public class AdminUsersController : ApiControllerBase
   {
       private readonly IAdminUserService _service;
       public AdminUsersController(IAdminUserService service) => _service = service;

       [HttpGet("/api/admin/users")]
       [HasPermission(Permissions.UserSettings.Manage)]
       [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
       public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserListItemDto>>>> List(
           [FromQuery] AdminUserListQuery query,
           CancellationToken ct)
           => Success(await _service.ListAsync(query, ct));
   }
   ```
   Lưu ý: dùng absolute route `/api/admin/users` cho nhất quán với `AdminUserSettingsController`.

### DI registration

6. Mở file đăng ký DI hiện tại của `IUserQuotationSettingsService`:
   - Grep: `rg -n "IUserQuotationSettingsService" backend/src/OrderMgmt.Application backend/src/OrderMgmt.Infrastructure` → xác nhận dòng `AddScoped<IUserQuotationSettingsService, ...>` trong `backend/src/OrderMgmt.Application/DependencyInjection.cs`.
   - Thêm dòng `services.AddScoped<IAdminUserService, AdminUserService>();` ngay sau dòng đó.

### Integration test

7. Tạo `AdminUsersListTests.cs` (kế thừa pattern `QuotationPermissionTests`):
   ```csharp
   [Collection(nameof(PostgresCollection))]
   public class AdminUsersListTests : QuotationTestBase
   {
       public AdminUsersListTests(PostgresFixture pg) : base(pg) { }

       [Fact]
       public async Task Admin_can_list_users_returns_admin_plus_test_users() { ... }

       [Fact]
       public async Task Sales_user_gets_forbidden() { ... }

       [Fact]
       public async Task ActiveOnly_filter_excludes_disabled_user() { ... }

       [Fact]
       public async Task Search_filters_by_username_substring() { ... }

       [Fact]
       public async Task Includes_soft_deleted_user_when_activeOnly_false() { ... }
   }
   ```
   - Đặt file vào folder mới `backend/tests/OrderMgmt.IntegrationTests/Admin/`.
   - Mỗi test: `CreateTestUserAsync("sales1", "Sales@123", RoleCodes.Sales)`. Để test inactive: sau khi seed, set `user.Status = UserStatus.Disabled` qua `db.SaveChangesAsync()`. Để test soft-delete: set `user.IsDeleted = true` (vẫn dùng `db` trực tiếp, không qua API DELETE để giữ test đơn giản).
   - Để authenticate sales: `await AuthenticateAsync("sales1", "Sales@123")` rồi GET `/api/admin/users` → assert `403 Forbidden`.
   - Endpoint trả `ApiResponse<List<AdminUserListItemDto>>` — parse như `QuotationTests` đang làm với `ApiResponse<List<T>>`.

## Verification

```powershell
# Build chỉ project liên quan
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -c Debug

# Test integration cho AdminUsers
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AdminUsersList"
```

Lưu ý: nếu WebApi đang chạy (memory feedback `feedback_build_skip_when_app_running.md`), không restart — chỉ build project Application/Infrastructure khi đã đổi. Nếu cần test thì stop dev server, run test, start lại.

## Exit Criteria

- 5 test trong `AdminUsersListTests` đều pass.
- `dotnet build` project WebApi không có warning mới.
- Endpoint `GET /api/admin/users?activeOnly=true` trả ApiResponse với array DTO, code 200 cho admin, 403 cho sales.
- DTO không chứa field nhạy cảm (`Email`, `PhoneNumber`, `PasswordHash`).
