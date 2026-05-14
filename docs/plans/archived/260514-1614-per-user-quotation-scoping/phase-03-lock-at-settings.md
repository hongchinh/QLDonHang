# Phase 03 — Lock-at + user settings

**Status:** [x] complete
**Complexity:** M

## Objective

Tạo entity `UserQuotationSettings` (1 record/user, `LockAtStatus` nullable, `TemplateFileName` nullable — chỉ dùng phase 04). Implement lock check trong `UpdateAsync` + `TransitionAsync`. 3 endpoint: user self GET, admin GET, admin PUT lock-at. Log Serilog mỗi lần admin đổi lock-at.

## Files

- `backend/src/OrderMgmt.Domain/Entities/Identity/UserQuotationSettings.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/UserConfiguration.cs` (modify — thêm `UserQuotationSettingsConfiguration`)
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` (modify — `DbSet<UserQuotationSettings>`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` (modify — DbSet)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/<auto>_AddUserQuotationSettings.cs` (new, EF)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/UserQuotationSettingsDto.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/UpdateLockAtRequest.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Interfaces/IUserQuotationSettingsService.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/UserQuotationSettingsService.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Validators/UpdateLockAtRequestValidator.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (modify — lock check)
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (modify — register service)
- `backend/src/OrderMgmt.WebApi/Controllers/MeQuotationSettingsController.cs` (new — user self)
- `backend/src/OrderMgmt.WebApi/Controllers/AdminUserSettingsController.cs` (new — admin)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationLockAtTests.cs` (new)

## Tasks

### Entity & migration

1. Tạo `UserQuotationSettings : BaseEntity`:
   ```csharp
   public Guid UserId { get; set; }
   public User? User { get; set; }
   public QuotationStatus? LockAtStatus { get; set; }
   public string? TemplateFileName { get; set; }  // phase 04 dùng
   public string? TemplateOriginalName { get; set; }  // hiển thị FE
   public DateTimeOffset? TemplateUploadedAt { get; set; }
   ```
2. Thêm `UserQuotationSettingsConfiguration` trong `UserConfiguration.cs`:
   - Table `user_quotation_settings`.
   - `b.HasIndex(x => x.UserId).IsUnique().HasFilter("is_deleted = false");`
   - `LockAtStatus` HasConversion<int?>.
   - `TemplateFileName` MaxLength 255.
   - `TemplateOriginalName` MaxLength 255.
   - `b.HasOne(x => x.User).WithOne().HasForeignKey<UserQuotationSettings>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);`
   - Không dùng physical cascade delete; project dùng soft-delete + audit, nên settings phải giữ được khi user bị soft-delete.
3. Expose `DbSet<UserQuotationSettings> UserQuotationSettings` trên `IAppDbContext` + `AppDbContext`.
4. Migration: `dotnet ef migrations add AddUserQuotationSettings`.

### Lock check trong QuotationService

5. Mở rộng helper `EnsureCanModify(Quotation q)` (private trong `QuotationService`):
   ```csharp
   private async Task EnsureCanModifyAsync(Quotation q, CancellationToken ct)
   {
       if (q.Status == QuotationStatus.Cancelled)
           throw new DomainException("CONFLICT", "Báo giá đã hủy không thể chỉnh sửa.");

       var isOwnerDeleted = await _db.Users
           .IgnoreQueryFilters()
           .Where(u => u.Id == q.OwnerUserId)
           .Select(u => u.IsDeleted)
           .FirstOrDefaultAsync(ct);
       if (isOwnerDeleted)
           throw new DomainException("CONFLICT", "Báo giá có chủ sở hữu đã ngừng hoạt động — chỉ có thể clone.");

       if (_currentUser.HasPermission(Permissions.Quotations.BypassLock)) return;

       var settings = await _db.UserQuotationSettings
           .AsNoTracking()
           .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId, ct);
       if (settings?.LockAtStatus is { } threshold && CompareStatus(q.Status, threshold) >= 0)
           throw new DomainException("CONFLICT", $"Báo giá ở trạng thái '{q.Status}' đã bị khoá theo cấu hình của bạn.");
   }

   private static int CompareStatus(QuotationStatus a, QuotationStatus b)
   {
       // Order: Draft(1) < Sent(2) < Confirmed(3) < ConvertedToOrder(4). Cancelled handled separately.
       static int Rank(QuotationStatus s) => s switch
       {
           QuotationStatus.Draft => 0,
           QuotationStatus.Sent => 1,
           QuotationStatus.Confirmed => 2,
           QuotationStatus.ConvertedToOrder => 3,
           _ => -1, // Cancelled không tham gia so sánh lock-at
       };
       return Rank(a).CompareTo(Rank(b));
   }
   ```
6. Gọi `EnsureCanModifyAsync` ở đầu `UpdateAsync` (thay block hiện tại check Cancelled). Gọi cả ở `TransitionAsync` (đối với action gây transition tới status >= threshold). Lưu ý: cho phép `Cancel` action luôn được phép (không check lock-at).
7. Trong `MapToDto`, tính `CanEdit`:
   - false nếu Status == Cancelled hoặc `IsOwnerDeleted == true` (owner lookup dùng `IgnoreQueryFilters()`).
   - false nếu user không có BypassLock và status >= settings.LockAtStatus.
   - Cần load settings cho user hiện tại 1 lần khi build DTO; thêm overload `MapToDto(Quotation, UserQuotationSettings?)`.

### Settings service

8. `UserQuotationSettingsDto` shape:
   ```csharp
   public Guid UserId { get; set; }
   public string? UserFullName { get; set; }
   public QuotationStatus? LockAtStatus { get; set; }
   public string? TemplateFileName { get; set; }
   public string? TemplateOriginalName { get; set; }
   public DateTimeOffset? TemplateUploadedAt { get; set; }
   ```
9. `UpdateLockAtRequest { QuotationStatus? LockAtStatus; }` + validator: chỉ chấp nhận null | Sent | Confirmed | ConvertedToOrder (không Draft, không Cancelled).
10. `IUserQuotationSettingsService` methods:
    ```csharp
    Task<UserQuotationSettingsDto> GetForCurrentUserAsync(CancellationToken ct);
    Task<UserQuotationSettingsDto> GetForUserAsync(Guid userId, CancellationToken ct);
    Task<UserQuotationSettingsDto> SetLockAtAsync(Guid userId, UpdateLockAtRequest req, CancellationToken ct);
    ```
11. Implement `UserQuotationSettingsService`:
    - `GetForCurrentUserAsync`: lazy-create record nếu chưa có (`FirstOrDefault` → tạo mới với defaults → SaveChanges).
    - `GetForUserAsync`: kiểm user tồn tại, lazy-create.
    - `SetLockAtAsync`: lazy-create + set `LockAtStatus`. **Log Serilog Information**:
      ```csharp
      _logger.LogInformation(
          "User settings lock-at changed: TargetUserId={TargetUserId}, OldValue={Old}, NewValue={New}, ActorUserId={ActorUserId}",
          userId, oldValue, req.LockAtStatus, _currentUser.UserId);
      ```
12. Register trong `DependencyInjection.cs`: `services.AddScoped<IUserQuotationSettingsService, UserQuotationSettingsService>();`.

### Controllers

13. `MeQuotationSettingsController` (route `/api/me/quotation-settings`):
    ```csharp
    [Authorize]
    [Route("api/me/quotation-settings")]
    public class MeQuotationSettingsController : ApiControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), 200)]
        public async Task<...> GetMine(IUserQuotationSettingsService svc, CancellationToken ct)
            => Success(await svc.GetForCurrentUserAsync(ct));
    }
    ```
14. `AdminUserSettingsController` (route `/api/admin/user-settings`):
    ```csharp
    [Route("api/admin/user-settings")]
    public class AdminUserSettingsController : ApiControllerBase
    {
        [HttpGet("{userId:guid}")]
        [HasPermission(Permissions.UserSettings.Manage)]
        [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), 200)]
        public async Task<...> Get(Guid userId, [FromServices] IUserQuotationSettingsService svc, CancellationToken ct)
            => Success(await svc.GetForUserAsync(userId, ct));

        [HttpPut("{userId:guid}/lock-at")]
        [HasPermission(Permissions.UserSettings.Manage)]
        public async Task<...> SetLockAt(Guid userId, [FromBody] UpdateLockAtRequest req, ...)
        {
            await validator.ValidateAndThrowAsync(req, ct);
            return Success(await svc.SetLockAtAsync(userId, req, ct));
        }
    }
    ```

### Tests

15. Tạo `QuotationLockAtTests`:
    - **SALES có lock=Sent, sửa Q Draft**: 200.
    - **SALES có lock=Sent, sửa Q Sent**: 400 CONFLICT.
    - **MANAGER bypass**: lock=Sent, sửa Q Sent → 200 (vì MANAGER có `BypassLock`).
    - **ADMIN bypass**: tương tự MANAGER.
    - **Lock after transfer**: A (lock=Sent) tạo Q, A transition Q→Sent, A transfer cho B (lock=Confirmed). B Update Q ở Sent → 200 (lock của B chưa tới).
    - **CanEdit reflects lock**: SALES có lock=Sent, GET Q Sent → response `canEdit = false`.
    - **Lock-at=null bypass**: chưa cấu hình → mọi status (trừ Cancelled) sửa được.
    - **Admin PUT lock-at**: ADMIN PUT cho SALES1 → response data.lockAtStatus đúng; log Serilog ghi 1 dòng (assert qua test sink hoặc skip nếu phức tạp).
    - **Sales không gọi được admin endpoint**: SALES GET `/api/admin/user-settings/{id}` → 403.

## Verification

```powershell
dotnet build backend/OrderMgmt.sln -c Debug
dotnet ef database update --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~QuotationLockAt|FullyQualifiedName~Quotation"
```

## Exit Criteria

- Bảng `user_quotation_settings` tồn tại với unique index trên `user_id`, FK user dùng `Restrict` thay vì physical cascade.
- User chưa có settings → endpoint trả default (`lockAtStatus = null`).
- Admin set `LockAtStatus`; user sửa Q ở status >= threshold → 400.
- ADMIN/MANAGER bypass lock được.
- `QuotationDto.CanEdit` phản ánh đúng cho cả 3 nguyên nhân (Cancelled, Orphan, Lock-at).
- Log Serilog xuất hiện khi admin đổi lock-at.
- 9 test mới pass.
