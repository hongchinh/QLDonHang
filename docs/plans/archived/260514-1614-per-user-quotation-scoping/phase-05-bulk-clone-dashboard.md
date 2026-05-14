# Phase 05 — Bulk-transfer + clone + dashboard stats

**Status:** [x] complete
**Complexity:** M

## Objective

3 endpoint còn lại:
- `POST /api/admin/users/{userId}/transfer-quotations` — admin chuyển toàn bộ báo giá active của 1 user (kể cả khi user soft-deleted) sang user khác. Ghi audit cho mỗi báo giá. Idempotent (gọi lại an toàn).
- `POST /api/quotations/{id}/clone` — clone báo giá thành Draft mới, `OwnerUserId = currentUser`, code tự sinh.
- `GET /api/dashboard/quotation-stats` — count, doanh thu (sum `Total` trừ status `Cancelled`), scope theo owner (Admin/Manager thấy tổng).

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` (modify — thêm `CloneAsync`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (modify — `CloneAsync`)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Interfaces/IQuotationBulkTransferService.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/QuotationBulkTransferService.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/BulkTransferRequest.cs` (new)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/BulkTransferResult.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationStatsDto.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationDashboardService.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationDashboardService.cs` (new)
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` (modify — `Clone`)
- `backend/src/OrderMgmt.WebApi/Controllers/AdminUserSettingsController.cs` (modify — `TransferQuotations`)
- `backend/src/OrderMgmt.WebApi/Controllers/DashboardController.cs` (new — `QuotationStats`)
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (modify — register 2 service mới)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationBulkCloneDashboardTests.cs` (new)

## Tasks

### Clone

1. Thêm `Task<QuotationDto> CloneAsync(Guid id, CancellationToken ct)` vào `IQuotationService`.
2. Implement trong `QuotationService.CloneAsync`:
   - Load source (kèm `Lines`; owner status lookup bằng `_db.Users.IgnoreQueryFilters()` để nhận diện owner đã soft-delete).
   - **Access check**:
     - Nếu owner lookup trả `IsDeleted=true` → cần permission `Quotations.CloneOrphan`. Không có → 403.
     - Ngược lại: dùng `EnsureCanAccess` thông thường (chỉ owner hoặc `ViewAll`).
   - Tạo `Quotation` mới: copy mọi field trừ `Id`, `Code`, `Status` (= Draft), `OwnerUserId` (= current user), audit. Tạo code mới qua `GenerateCodeAsync`.
   - Copy `Lines` (Id mới, giữ SortOrder, không copy audit cũ).
   - Add + SaveChanges (loop retry như `CreateAsync`).
   - Return `GetAsync(newId)`.
3. Endpoint controller:
   ```csharp
   [HttpPost("{id:guid}/clone")]
   [HasPermission(Permissions.Quotations.Create)]
   [ProducesResponseType(typeof(ApiResponse<QuotationDto>), 200)]
   public async Task<...> Clone(Guid id, CancellationToken ct)
       => Success(await _quotations.CloneAsync(id, ct));
   ```

### Bulk-transfer

4. `BulkTransferRequest`:
   ```csharp
   public Guid ToUserId { get; set; }
   public bool IncludeCancelled { get; set; } = false;
   public string? Reason { get; set; }
   ```
5. `BulkTransferResult { int AffectedCount; Guid FromUserId; Guid ToUserId; }`.
6. Implement `QuotationBulkTransferService.TransferAllAsync(Guid fromUserId, BulkTransferRequest req, CancellationToken ct)`:
   - Validate `req.ToUserId != fromUserId`.
   - Validate `ToUser` tồn tại + không bị disabled (cho phép user mới mặc dù chưa từng có quotation).
   - Query: `q => q.OwnerUserId == fromUserId && (req.IncludeCancelled || q.Status != Cancelled)`.
   - Foreach: ghi 1 dòng `QuotationOwnerHistory` (OldOwnerUserId = fromUserId, NewOwnerUserId = ToUserId, ActorUserId = currentUser, Reason); set `OwnerUserId = ToUserId`.
   - SaveChanges trong 1 transaction.
   - **Idempotency**: gọi lần 2 (sau khi đã chuyển hết) — query trả về 0 record → AffectedCount = 0, không ghi history mới. Đảm bảo bằng query thực tế (`OwnerUserId == fromUserId`), không cần dedup logic.
   - Log Serilog: `_logger.LogInformation("Bulk transfer: From={From}, To={To}, Count={Count}, Actor={Actor}", ...);`.
7. Endpoint trên `AdminUserSettingsController`:
   ```csharp
   [HttpPost("/api/admin/users/{userId:guid}/transfer-quotations")]
   [HasPermission(Permissions.Quotations.TransferAny)]
   public async Task<...> Bulk(Guid userId, [FromBody] BulkTransferRequest req,
       [FromServices] IQuotationBulkTransferService svc,
       [FromServices] IValidator<BulkTransferRequest> validator, CancellationToken ct)
   {
       await validator.ValidateAndThrowAsync(req, ct);
       return Success(await svc.TransferAllAsync(userId, req, ct));
   }
   ```

### Dashboard stats

8. `QuotationStatsDto`:
   ```csharp
   public int TotalCount { get; set; }
   public int DraftCount { get; set; }
   public int SentCount { get; set; }
   public int ConfirmedCount { get; set; }
   public int ConvertedCount { get; set; }
   public int CancelledCount { get; set; }
   public decimal TotalRevenue { get; set; }  // sum Total trừ Cancelled
   public decimal TodayRevenue { get; set; }
   public DateOnly From { get; set; }
   public DateOnly To { get; set; }
   ```
9. `IQuotationDashboardService.GetStatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)`:
   - Default: from = đầu tháng hiện tại, to = hôm nay.
   - Áp dụng `ApplyOwnerScope` giống QuotationService (extract helper chung hoặc duplicate đơn giản — dự án nhỏ, không refactor).
   - Single query group-by Status để lấy counts; separate query sum Total where Status != Cancelled.
10. Controller `DashboardController`:
    ```csharp
    [Route("api/dashboard")]
    public class DashboardController : ApiControllerBase
    {
        [HttpGet("quotation-stats")]
        [HasPermission(Permissions.Quotations.View)]
        [ProducesResponseType(typeof(ApiResponse<QuotationStatsDto>), 200)]
        public async Task<...> Stats([FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
            [FromServices] IQuotationDashboardService svc, CancellationToken ct)
            => Success(await svc.GetStatsAsync(from, to, ct));
    }
    ```

### Tests

11. Tạo `QuotationBulkCloneDashboardTests`:
    - **Clone báo giá của mình**: SALES1 clone Q của mình → Q mới có `OwnerUserId = SALES1`, status Draft, code khác.
    - **Clone báo giá orphan**: ADMIN có `CloneOrphan` clone Q của user đã soft-delete → 200; SALES (không `CloneOrphan`) clone Q orphan → 403.
    - **Clone báo giá người khác**: SALES2 clone Q của SALES1 → 403.
    - **Bulk-transfer**: ADMIN bulk-transfer toàn bộ Q của SALES1 (5 cái, 1 Cancelled) → AffectedCount = 4 (mặc định bỏ Cancelled). `QuotationOwnerHistory` có 4 dòng.
    - **Bulk-transfer idempotent**: gọi lần 2 → AffectedCount = 0, không thêm history.
    - **Bulk-transfer include cancelled**: `IncludeCancelled = true` → AffectedCount = 5.
    - **Bulk-transfer ToUser bằng FromUser**: 400 VALIDATION.
    - **Bulk-transfer non-admin**: SALES gọi → 403.
    - **Dashboard SALES**: SALES1 có 3 Q (1 Cancelled), GET stats → TotalCount = 3, CancelledCount = 1, TotalRevenue = sum 2 Q kia.
    - **Dashboard ADMIN**: ADMIN GET → totalCount của toàn bộ.
    - **Cancelled excluded từ revenue**: tạo Q rồi cancel → revenue giảm.

## Verification

```powershell
dotnet build backend/OrderMgmt.sln -c Debug
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~QuotationBulkClone"
```

## Exit Criteria

- 3 endpoint hoạt động đúng quyền.
- Bulk-transfer idempotent + audit history đầy đủ.
- Clone orphan hoạt động cho ADMIN/MANAGER; sales bị chặn.
- Dashboard scope đúng theo permission `ViewAll`; revenue loại Cancelled.
- 11 test mới pass.
