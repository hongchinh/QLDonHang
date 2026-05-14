# Phase 02 — Scoping + owner transfer + audit

**Status:** [x] complete
**Complexity:** L

## Objective

Thêm 6 permission code mới. `QuotationService` áp scope theo `OwnerUserId` và guard truy cập. Endpoint `PATCH /api/quotations/{id}/owner` cho self-transfer & admin-transfer. Bảng audit `QuotationOwnerHistory` ghi lại mọi lần chuyển nhượng. Feature flag `Features:QuotationOwnerScope` để rollback.

## Files

- `backend/src/OrderMgmt.Domain/Constants/Permissions.cs` (modify — thêm static class `UserSettings`, thêm code mới vào `Quotations`)
- `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationOwnerHistory.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` (modify — thêm `QuotationOwnerHistoryConfiguration`)
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` (modify — expose `DbSet<QuotationOwnerHistory>`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` (modify — DbSet)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/<auto>_AddQuotationOwnerHistory.cs` (new, EF)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs` (modify — seed 6 permission + map role)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (modify — `ApplyOwnerScope`, `EnsureCanAccess`, populate `CanEdit`, `CanClone`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` (modify — thêm `TransferOwnerAsync`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/TransferOwnerRequest.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (modify — `CanEdit`, `CanClone`, `IsOwnerDeleted`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationListItemDto.cs` (modify — `CanClone`, `IsOwnerDeleted`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/TransferOwnerRequestValidator.cs` (new)
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` (modify — endpoint `PatchOwner`)
- `backend/src/OrderMgmt.WebApi/appsettings.json` & `appsettings.Development.json` (modify — thêm `Features:QuotationOwnerScope = true`)
- `backend/src/OrderMgmt.WebApi/Program.cs` (modify — bind FeatureOptions)
- `backend/src/OrderMgmt.Application/Common/Options/FeatureOptions.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationOwnerScopeTests.cs` (new)

## Tasks

### Domain & permissions

1. Trong [Permissions.cs](../../../backend/src/OrderMgmt.Domain/Constants/Permissions.cs), bên trong `static class Quotations`, thêm:
   ```csharp
   public const string ViewAll = "quotations.view_all";
   public const string TransferOwn = "quotations.transfer_own";
   public const string TransferAny = "quotations.transfer_any";
   public const string CloneOrphan = "quotations.clone_orphan";
   public const string BypassLock = "quotations.bypass_lock";
   ```
   Bên ngoài `Quotations`, thêm `static class UserSettings { public const string Manage = "user_settings.manage"; }`.
2. Tạo entity `QuotationOwnerHistory : BaseEntity`:
   ```csharp
   public Guid QuotationId { get; set; }
   public Guid? OldOwnerUserId { get; set; }
   public Guid NewOwnerUserId { get; set; }
   public Guid ActorUserId { get; set; }
   public string? Reason { get; set; }
   public DateTimeOffset ChangedAt { get; set; }
   ```
3. Trong `SalesConfiguration.cs`, thêm `QuotationOwnerHistoryConfiguration`:
   - Table `quotation_owner_history`.
   - Index `(QuotationId, ChangedAt)`.
   - `Reason` max length 500.
   - Không `HasQueryFilter` (audit log không soft-delete).
4. Expose `DbSet<QuotationOwnerHistory> QuotationOwnerHistory` trong `IAppDbContext` + `AppDbContext`.
5. Migration: `dotnet ef migrations add AddQuotationOwnerHistory --project ... --startup-project ...`.

### Seed permissions + role mapping

6. Trong `DbSeeder.SeedPermissionsAsync` thêm 6 dòng vào `permissionDefs`:
   ```csharp
   (Permissions.Quotations.ViewAll, Permissions.SalesModule, "Xem mọi báo giá (bypass owner)"),
   (Permissions.Quotations.TransferOwn, Permissions.SalesModule, "Chuyển báo giá của mình cho user khác"),
   (Permissions.Quotations.TransferAny, Permissions.SalesModule, "Chuyển báo giá của bất kỳ user nào"),
   (Permissions.Quotations.CloneOrphan, Permissions.SalesModule, "Clone báo giá của user đã nghỉ"),
   (Permissions.Quotations.BypassLock, Permissions.SalesModule, "Bypass khoá trạng thái báo giá"),
   (Permissions.UserSettings.Manage, Permissions.SystemModule, "Cấu hình thiết lập của user khác"),
   ```
7. Trong `DbSeeder.SeedRolesAsync` cập nhật role:
   - `Sales`: thêm `Permissions.Quotations.TransferOwn`.
   - `Manager`: đã `allPermissions` → không cần thay đổi (đã có hết).
   - `Admin`: đã `allPermissions`.
   - `Accountant`/`Warehouse`: không thêm.

### Service: scoping + access guard + transfer

8. Thêm `FeatureOptions` class:
   ```csharp
   public class FeatureOptions
   {
       public const string SectionName = "Features";
       public bool QuotationOwnerScope { get; set; } = true;
   }
   ```
   Trong `Program.cs`: `builder.Services.Configure<FeatureOptions>(builder.Configuration.GetSection(FeatureOptions.SectionName));`.
9. Inject `IOptionsMonitor<FeatureOptions>` vào `QuotationService`.
10. Thêm private helper:
    ```csharp
    private IQueryable<Quotation> ApplyOwnerScope(IQueryable<Quotation> q)
    {
        if (!_features.CurrentValue.QuotationOwnerScope) return q;
        if (_currentUser.HasPermission(Permissions.Quotations.ViewAll)) return q;
        var uid = _currentUser.UserId ?? Guid.Empty;
        return q.Where(x => x.OwnerUserId == uid);
    }

    private void EnsureCanAccess(Quotation quotation)
    {
        // Feature flag is an app-level rollback for owner scoping. When false,
        // owner mismatch is ignored for list/detail/print/update/delete/transition;
        // action-level permissions still apply through controller attributes.
        if (!_features.CurrentValue.QuotationOwnerScope) return;
        if (_currentUser.HasPermission(Permissions.Quotations.ViewAll)) return;
        if (quotation.OwnerUserId != _currentUser.UserId)
            throw new ForbiddenException("Bạn không có quyền truy cập báo giá này.");
    }
    ```
11. Áp `ApplyOwnerScope` vào `ListAsync` query gốc; áp `EnsureCanAccess` ngay sau load entity trong `GetAsync`, `UpdateAsync`, `DeleteAsync`, `TransitionAsync`, `RenderExcelAsync`, `RenderPdfAsync`.
12. Tính `CanEdit` trong `MapToDto`:
    - false nếu `Status == Cancelled`.
    - false nếu `IsOwnerDeleted == true` (orphan). Owner lookup phải dùng `IgnoreQueryFilters()` vì query filter của `User` ẩn soft-deleted owner.
    - (chưa kiểm `LockAtStatus` ở phase này — Phase 03 thêm).
    - true otherwise.
    `CanClone = IsOwnerDeleted == true` (orphan có thể clone). Thêm cùng logic cho `QuotationListItemDto` để FE list hiển thị badge/nút clone mà không phải gọi detail.

### Transfer endpoint

13. Tạo `TransferOwnerRequest`:
    ```csharp
    public class TransferOwnerRequest
    {
        public Guid NewOwnerUserId { get; set; }
        public string? Reason { get; set; }
    }
    ```
14. `TransferOwnerRequestValidator`: `NewOwnerUserId` không empty; `Reason` <= 500.
15. Thêm `Task<QuotationDto> TransferOwnerAsync(Guid id, TransferOwnerRequest req, CancellationToken ct)` vào `IQuotationService` + implement:
    - Load quotation (kèm `Owner`).
    - Kiểm quyền: nếu `quotation.OwnerUserId == _currentUser.UserId` thì cần `TransferOwn`; nếu khác thì cần `TransferAny`. Không có → `ForbiddenException`.
    - Cấm transfer nếu owner lookup bằng `IgnoreQueryFilters()` trả `IsDeleted=true` (orphan dùng bulk-transfer, không cho single).
    - Validate `NewOwnerUserId` tồn tại + không bị disabled/deleted.
    - Ghi `QuotationOwnerHistory { QuotationId, OldOwnerUserId, NewOwnerUserId, ActorUserId = _currentUser.UserId, Reason, ChangedAt = _clock.UtcNow }`.
    - Update `quotation.OwnerUserId = req.NewOwnerUserId`.
    - SaveChanges. Return `GetAsync`.
16. Trong `QuotationsController`, thêm:
    ```csharp
    [HttpPatch("{id:guid}/owner")]
    [ProducesResponseType(typeof(ApiResponse<QuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> TransferOwner(
        Guid id, [FromBody] TransferOwnerRequest request,
        [FromServices] IValidator<TransferOwnerRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Success(await _quotations.TransferOwnerAsync(id, request, ct));
    }
    ```
    *Không* gắn `[HasPermission(...)]` trên action vì check theo logic động (own vs any); kiểm quyền trong service.

### Config

17. Thêm `"Features": { "QuotationOwnerScope": true }` vào `appsettings.json` & `.Development.json`.

### Tests

18. Tạo `QuotationOwnerScopeTests` (extends `QuotationTestBase`):
    - **SALES1 tạo Q, SALES2 list không thấy**: tạo 2 user role Sales, đăng nhập SALES1 tạo, đăng nhập SALES2 GET list → response không chứa.
    - **ADMIN list thấy tất cả**: admin GET list → có Q.
    - **SALES2 GET Q của SALES1 → 403**.
    - **Self-transfer**: SALES1 PATCH owner Q của mình → SALES2 → 200; SALES1 GET Q → 403; SALES2 GET Q → 200.
    - **Cross-user transfer denied**: SALES1 PATCH owner Q của SALES2 → 403.
    - **Admin transfer-any**: ADMIN PATCH owner Q của SALES1 → SALES2 → 200.
    - **Audit log ghi đúng**: PATCH → query `QuotationOwnerHistory` → 1 dòng đúng ActorId/Old/New.
    - **Feature flag tắt**: set `QuotationOwnerScope=false` qua `WebAppFactory` config override → SALES2 thấy Q của SALES1 trong list và GET detail được nếu có `quotations.view`.

## Verification

```powershell
dotnet build backend/OrderMgmt.sln -c Debug
dotnet ef database update --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~QuotationOwnerScope"
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Quotation"
```

## Exit Criteria

- 6 permission mới được seed; ADMIN/MANAGER có `ViewAll`, SALES có `TransferOwn`.
- SALES chỉ thấy/sửa báo giá của mình; ADMIN/MANAGER bypass.
- PATCH owner endpoint hoạt động với cả self-transfer và admin transfer.
- `QuotationOwnerHistory` ghi 1 dòng cho mỗi lần chuyển.
- `Features:QuotationOwnerScope = false` bypass owner filter + owner mismatch guard (rollback path verified); controller `[HasPermission]` vẫn chặn theo action permission.
- 8 test mới trong `QuotationOwnerScopeTests` pass.
