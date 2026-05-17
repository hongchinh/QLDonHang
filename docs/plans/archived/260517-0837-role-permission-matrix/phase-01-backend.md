# Phase 01 — Backend service + API + seeder + tests

**Status:** [x] complete (build-pass; integration tests deferred — Docker not available at execute time)
**Complexity:** L

## Objective

Cung cấp đầy đủ backend cho quản lý Role và Role × Permission matrix: 6 endpoint REST, service layer, validators, integration tests; đồng thời sửa `DbSeeder.SeedRolesAsync` để không ghi đè permission của system role khác ngoài ADMIN.

## Files

### New (Application layer — theo pattern `backend/src/OrderMgmt.Application/Identity/Admin/`)

- `backend/src/OrderMgmt.Application/Identity/Admin/Models/RoleListItemDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/RoleDetailDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/PermissionDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/CreateRoleRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/UpdateRoleRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/UpdateRolePermissionsRequest.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/CreateRoleRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/UpdateRoleRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Validators/UpdateRolePermissionsRequestValidator.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Interfaces/IAdminRoleService.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Services/AdminRoleService.cs`

### New (WebApi)

- `backend/src/OrderMgmt.WebApi/Controllers/AdminRolesController.cs`

### Modify

- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — đăng ký `services.AddScoped<IAdminRoleService, AdminRoleService>();`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs` — sửa `SeedRolesAsync` (logic mới phía dưới)

### New (Tests)

- `backend/tests/OrderMgmt.IntegrationTests/Admin/AdminRolesCrudTests.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Admin/AdminRolesPermissionsTests.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Admin/DbSeederUpgradeTests.cs`

## Tasks

### A. DTOs & Requests

1. `PermissionDto`: `Code`, `Name`, `Module`, `Description` (mirror Permission entity, read-only).
2. `RoleListItemDto`: `Id`, `Code`, `Name`, `Description`, `IsSystem`, `PermissionCount`, `UserCount`.
3. `RoleDetailDto`: `Id`, `Code`, `Name`, `Description`, `IsSystem`, `PermissionCodes` (string[]), `UserCount`, `CreatedAt`, `UpdatedAt`.
4. `CreateRoleRequest`: `Code` (string), `Name` (string), `Description` (string?), `PermissionCodes` (string[]).
5. `UpdateRoleRequest`: `Name` (string), `Description` (string?).
6. `UpdateRolePermissionsRequest`: `PermissionCodes` (string[]).

### B. Validators (FluentValidation, pattern theo `CreateUserRequestValidator`)

7. `CreateRoleRequestValidator`:
   - `Code`: NotEmpty, `Matches("^[A-Z_][A-Z0-9_]{1,29}$")` (2–30 ký tự, in hoa/underscore — regex tự cap length, không cần thêm MaxLength) với message tiếng Việt; không nằm trong `{RoleCodes.Admin, Sales, Accountant, Warehouse, Manager}` (case-insensitive — reserved system codes).
   - `Name`: NotEmpty, MaxLength 200. **Soft-check trùng Name** (case-insensitive, accent-insensitive) — service trả `ConflictException("Tên role 'X' đã tồn tại.")` nếu trùng với role chưa xoá (tránh UI rối với 2 role cùng tên).
   - `Description`: MaxLength 500 khi không null.
   - `PermissionCodes`: NotNull, distinct (custom rule via `Must`).
8. `UpdateRoleRequestValidator`:
   - `Name`: NotEmpty, MaxLength 200. Service áp dụng soft-check trùng Name như Create (loại trừ chính nó).
   - `Description`: MaxLength 500 khi không null.
9. `UpdateRolePermissionsRequestValidator`:
   - `PermissionCodes`: NotNull, distinct.

### C. Service interface + implementation

10. `IAdminRoleService`:
    ```csharp
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleListItemDto>> ListAsync(CancellationToken ct = default);
    Task<RoleDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<RoleDetailDto> CreateAsync(CreateRoleRequest req, CancellationToken ct = default);
    Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest req, CancellationToken ct = default);
    Task<RoleDetailDto> UpdatePermissionsAsync(Guid id, UpdateRolePermissionsRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    ```
11. `AdminRoleService` — inject `IAppDbContext`.
    - `ListPermissionsAsync`: trả về tất cả `Permissions` từ DB (theo Module rồi Code, asc).
    - `ListAsync`: trả Role list (kèm `PermissionCount = role.RolePermissions.Count`, `UserCount = role.UserRoles.Count(ur => !ur.User.IsDeleted)`).
    - `GetAsync`: include `RolePermissions.Permission`; throw `NotFoundException` nếu không tìm thấy.
    - `CreateAsync`:
      - Check `Code` trùng (case-insensitive) → `ConflictException("Code role 'X' đã tồn tại.")`.
      - Load permissions theo `PermissionCodes` (validate tất cả tồn tại — nếu missing → `ConflictException("Permission 'X' không tồn tại.")`).
      - Tạo Role với `IsSystem = false`, add `RolePermission` cho từng code.
      - Return GetAsync mới tạo.
    - `UpdateAsync` (rename / description):
      - Load role; nếu `role.IsSystem` → `ForbiddenException("Không thể đổi tên role hệ thống.")`.
      - Update Name/Description, save.
    - `UpdatePermissionsAsync`:
      - Load role + `RolePermissions`.
      - Nếu `role.Code == RoleCodes.Admin` → `ForbiddenException("Không thể chỉnh sửa permission của role ADMIN.")`.
      - Validate tất cả `PermissionCodes` tồn tại (như Create).
      - Compute diff: codes-to-remove = existing - new; codes-to-add = new - existing.
      - Remove `RolePermission` rows, add rows mới. Save.
      - **Audit log** (structured) trước khi return:
        ```csharp
        _logger.LogInformation(
            "Role {RoleCode} permissions updated by user {UserId}. Added: [{Added}]. Removed: [{Removed}].",
            role.Code, _currentUser.UserId, string.Join(",", codesToAdd), string.Join(",", codesToRemove));
        ```
        (Inject `ILogger<AdminRoleService>` + `ICurrentUser` ở constructor.) Tương tự log info ngắn cho Create / Update / Delete.
    - `DeleteAsync`:
      - Load role kèm `RolePermissions`; nếu `IsSystem` → `ForbiddenException("Không thể xoá role hệ thống.")`.
      - Count user đang dùng: `await _db.UserRoles.CountAsync(ur => ur.RoleId == id && !ur.User.IsDeleted, ct)`.
      - Nếu > 0 → `ConflictException($"Role này đang được gán cho {count} user. Vui lòng đổi role các user trước khi xoá.")`.
      - `RolePermission` là pure join entity (không `BaseEntity` / `ISoftDeletable`), cascade soft-delete ([AppDbContext.cs:130](../../../backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs#L130)) bỏ qua → phải **hard-delete** `RolePermissions` rows trước: `_db.RolePermissions.RemoveRange(role.RolePermissions);`.
      - Sau đó soft-delete role (set `IsDeleted = true`). Một `SaveChangesAsync`.

### D. Controller

12. `AdminRolesController : ApiControllerBase`:
    - `GET /api/admin/permissions` `[HasPermission(Permissions.Roles.View)]` → `ApiResponse<IReadOnlyList<PermissionDto>>`.
    - `GET /api/admin/roles` `[HasPermission(Permissions.Roles.View)]` → `ApiResponse<IReadOnlyList<RoleListItemDto>>`.
    - `GET /api/admin/roles/{id:guid}` `[HasPermission(Permissions.Roles.View)]` → `ApiResponse<RoleDetailDto>`.
    - `POST /api/admin/roles` `[HasPermission(Permissions.Roles.Manage)]` → `ApiResponse<RoleDetailDto>`.
    - `PUT /api/admin/roles/{id:guid}` `[HasPermission(Permissions.Roles.Manage)]` → `ApiResponse<RoleDetailDto>`.
    - `PUT /api/admin/roles/{id:guid}/permissions` `[HasPermission(Permissions.Roles.Manage)]` → `ApiResponse<RoleDetailDto>`.
    - `DELETE /api/admin/roles/{id:guid}` `[HasPermission(Permissions.Roles.Manage)]` → `ApiResponse`.

### E. DI registration

13. Thêm vào `DependencyInjection.cs` (sau dòng `IAdminUserService`):
    ```csharp
    services.AddScoped<IAdminRoleService, AdminRoleService>();
    ```

### F. DbSeeder change

14. Sửa `SeedRolesAsync` trong `DbSeeder.cs`:
    - **ADMIN**: re-apply full permission mỗi startup (giữ logic hiện tại cho ADMIN — đảm bảo permission mới được tự gán).
    - **System role khác (SALES/ACCOUNTANT/WAREHOUSE/MANAGER)**: chỉ seed permissions khi `role.RolePermissions.Count == 0` (lần đầu tạo HOẶC fallback nếu DB bị tay người xoá hết). Nếu role đã có ≥1 permission → SKIP, không thêm/bớt.
    - Logic mới (pseudocode):
      ```csharp
      foreach (var (code, name, permCodes) in roleDefs)
      {
          var role = existingRoles.FirstOrDefault(r => r.Code == code);
          if (role is null)
          {
              role = new Role { Code = code, Name = name, IsSystem = true };
              db.Roles.Add(role);
              // first-time create → apply default permissions
              AssignPermissions(role, permCodes, allPermissions);
              continue;
          }

          if (code == RoleCodes.Admin)
          {
              // ADMIN: always re-apply full permissions (đảm bảo permission mới được tự gán)
              AssignPermissions(role, permCodes, allPermissions);
              continue;
          }

          // Defensive fallback: role tồn tại nhưng KHÔNG còn permission nào (DB bị tay xoá / migrate
          // lỗi) → re-seed default. Trong flow bình thường (admin chỉ chỉnh qua UI), role luôn có
          // ≥1 permission nên branch này không kích hoạt.
          if (role.RolePermissions.Count == 0)
          {
              AssignPermissions(role, permCodes, allPermissions);
              continue;
          }

          // else: existing non-Admin system role đã có permissions → skip, tôn trọng chỉnh sửa
          // của admin qua UI.
      }
      ```
    - `AssignPermissions` helper: vẫn loop add row nếu chưa tồn tại; KHÔNG remove (cho phép admin add extra qua UI).

### G. Tests

15. `AdminRolesCrudTests` (collection `PostgresCollection`, kế thừa `QuotationTestBase` cho `AuthenticateAsync` admin):
    - `List_returns_5_system_roles_with_user_and_permission_count`
    - `Get_returns_role_with_permission_codes`
    - `Create_custom_role_succeeds_and_isSystem_false`
    - `Create_with_duplicate_code_returns_409`
    - `Create_with_duplicate_name_returns_409` (test trùng Name case + accent insensitive)
    - `Create_with_reserved_system_code_returns_400` (test `SALES`, `admin` lowercase)
    - `Create_with_invalid_code_format_returns_400` (test `sales`, `1ABC`, `A`)
    - `Create_with_unknown_permission_code_returns_409`
    - `Update_custom_role_name_succeeds`
    - `Update_system_role_name_returns_403`
    - `Delete_custom_role_without_users_succeeds`
    - `Delete_custom_role_with_users_returns_409`
    - `Delete_system_role_returns_403`
    - `Delete_role_hard_removes_role_permission_rows` (RolePermission rows bị hard-delete, Role bị soft-delete với `IsDeleted = true`).

16. `AdminRolesPermissionsTests`:
    - `ListPermissions_returns_all_seeded_permissions`
    - `UpdatePermissions_custom_role_replaces_set`
    - `UpdatePermissions_non_admin_system_role_succeeds` (SALES gain `quotations.delete`)
    - `UpdatePermissions_admin_role_returns_403`
    - `UpdatePermissions_with_invalid_code_returns_409`
    - `User_with_updated_role_sees_new_permissions_after_token_refresh`:
      - Tạo user role SALES, login lấy access+refresh tokens.
      - Verify `permission` claim không có `quotations.delete`.
      - Admin client gọi `PUT /api/admin/roles/{salesId}/permissions` thêm `quotations.delete`.
      - User refresh token → verify access token mới có `quotations.delete`.

17. `DbSeederUpgradeTests`:
    - `Reseeding_does_not_overwrite_modified_system_role_permissions`:
      - Trigger seed → assert SALES có default permissions.
      - Admin client xoá `customers.view` khỏi SALES (qua `PUT /permissions`).
      - Re-run `DbSeeder.SeedAsync(factory.Services)`.
      - Assert SALES vẫn KHÔNG có `customers.view`.
    - `Reseeding_keeps_admin_full_permissions`:
      - Trigger seed → assert ADMIN có toàn bộ permission codes.
      - (Bonus: thêm permission mới giả lập bằng cách add row vào DB rồi reseed → assert ADMIN tự có).
    - `Reseeding_fallback_restores_default_when_system_role_has_zero_permissions`:
      - Trigger seed; xoá hết `RolePermissions` của SALES trực tiếp qua DbContext (giả lập DB bị tay người xoá).
      - Re-run `DbSeeder.SeedAsync`.
      - Assert SALES được restore default permissions (verify fallback branch trong pseudocode).

## Verification

```powershell
dotnet build backend/OrderMgmt.sln
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj `
  --filter "FullyQualifiedName~AdminRolesCrud|FullyQualifiedName~AdminRolesPermissions|FullyQualifiedName~DbSeederUpgrade"
```

Manual API check (Swagger / curl, sau khi `dotnet run`):

```
GET    /api/admin/roles                                  → 200, 5 system roles
POST   /api/admin/roles { Code:"TEST_LEAD", Name:"Test", PermissionCodes:["quotations.view"] }  → 200
PUT    /api/admin/roles/<adminRoleId>/permissions { PermissionCodes:[] }  → 403
DELETE /api/admin/roles/<salesRoleId>                    → 403 (system)
```

## Exit Criteria

- [ ] `dotnet build` xanh, không warning analyzer mới.
- [ ] Tất cả 3 file test mới chạy xanh (>15 test mới).
- [ ] Admin login → `GET /api/admin/roles` trả 5 system roles + custom role đã tạo.
- [ ] DELETE custom role đang có user gán → 409 với message Việt rõ.
- [ ] PUT permissions vào ADMIN role → 403.
- [ ] DbSeeder upgrade test pass: chỉnh permission SALES → reseed → vẫn giữ.
