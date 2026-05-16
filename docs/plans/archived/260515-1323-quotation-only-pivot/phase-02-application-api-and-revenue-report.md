# Phase 02 — Application service, API, sales revenue report

**Status:** [x] complete
**Complexity:** L

## Objective

Cập nhật `QuotationService` để snapshot `ConfirmedAt`/`CancelledAt` khi transition và check `CancelConfirmed` permission. Dọn các tham chiếu `ConvertedToOrder` còn sót. Build sales revenue report service + endpoint. Cập nhật `DbSeeder` permission/role mapping.

## Files

### Modify
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationDashboardService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationStatsDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Validators/UpdateLockAtRequestValidator.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs`
- `backend/src/OrderMgmt.WebApi/Program.cs` (đăng ký service mới — chỉ thêm 1 dòng)

### Create
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Services/SalesRevenueReportService.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Validators/SalesRevenueReportRequestValidator.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`

## Tasks

### A. Sửa `QuotationService.cs`

1. Trong `Transitions` static dictionary: không cần đổi (vẫn cover Draft→Sent, Draft→Cancel, Sent→Confirm, Sent→Cancel, Confirmed→Cancel) — chỉ kiểm tra không có entry tham chiếu `ConvertedToOrder` (đã không có).
2. Trong method `CompareStatus`, xóa case `QuotationStatus.ConvertedToOrder => 3`. Order mới: `Draft=0, Sent=1, Confirmed=2`. Cập nhật comment ở trên cho khớp.
3. Trong `TransitionAsync`, thay block `quotation.Status = next; await _db.SaveChangesAsync(ct);` bằng:
   ```csharp
   if (action == QuotationAction.Cancel
       && quotation.Status == QuotationStatus.Confirmed
       && !_currentUser.HasPermission(Permissions.Quotations.CancelConfirmed))
   {
       throw new ForbiddenException("Bạn không có quyền hủy báo giá đã xác nhận.");
   }

   quotation.Status = next;
   if (next == QuotationStatus.Confirmed)
   {
       quotation.ConfirmedAt = _clock.UtcNow.UtcDateTime;
       quotation.ConfirmedByUserId = _currentUser.UserId;
   }
   else if (next == QuotationStatus.Cancelled)
   {
       quotation.CancelledAt = _clock.UtcNow.UtcDateTime;
   }

   await _db.SaveChangesAsync(ct);
   ```
   (Nếu `_clock.UtcNow` trả `DateTimeOffset` thì dùng `.UtcDateTime`; xác nhận theo `IDateTime` interface.)

### B. Sửa `QuotationDashboardService.cs` & `QuotationStatsDto.cs`

4. Trong `QuotationStatsDto.cs`: xóa property `public int ConvertedCount { get; set; }`.
5. Trong `QuotationDashboardService.GetStatsAsync`, xóa case `QuotationStatus.ConvertedToOrder: dto.ConvertedCount = row.Count; break;`.

### C. Sửa `QuotationDto.cs`

6. Trong `QuotationDto`, thêm sau property `Status`:
   ```csharp
   public DateTime? ConfirmedAt { get; set; }
   public Guid? ConfirmedByUserId { get; set; }
   public string? ConfirmedByName { get; set; }
   public DateTime? CancelledAt { get; set; }
   ```
7. Trong `QuotationListItemDto`, thêm:
   ```csharp
   public DateTime? ConfirmedAt { get; set; }
   ```
8. Trong `QuotationService.MapToDto` (cuối file `QuotationService.cs`), gán thêm:
   ```csharp
   ConfirmedAt = q.ConfirmedAt,
   ConfirmedByUserId = q.ConfirmedByUserId,
   CancelledAt = q.CancelledAt,
   ```
   `ConfirmedByName` sẽ được set ở `GetAsync` (truyền vào MapToDto qua optional param mới hoặc một second resolve giống `ownerFullName`). Phương án tối thiểu: thêm param `string? confirmedByName = null` vào `MapToDto`, và trong `GetAsync` query thêm tên user nếu `ConfirmedByUserId.HasValue`.
9. Trong `QuotationService.ListAsync` projection, thêm `ConfirmedAt = q.ConfirmedAt,` vào anonymous select cho `QuotationListItemDto`.

### D. Sửa `UpdateLockAtRequestValidator.cs`

10. Xóa `QuotationStatus.ConvertedToOrder,` khỏi `AllowedLockAt` HashSet.
11. Cập nhật `WithMessage` thành: `"LockAtStatus chỉ chấp nhận: null | Sent | Confirmed."`

### E. Sales Revenue Report

12. Tạo `Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs`:
    ```csharp
    using OrderMgmt.Application.Common.Models;

    namespace OrderMgmt.Application.Reports.SalesRevenue.Models;

    public class SalesRevenueReportRequest
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Guid? SaleUserId { get; set; }
    }

    public class SalesRevenueReportItem
    {
        public Guid SaleUserId { get; set; }
        public string SaleName { get; set; } = default!;
        public bool IsSaleDeleted { get; set; }
        public int QuotationCount { get; set; }
        public decimal TotalRevenueGross { get; set; }
        public decimal TotalRevenueNet { get; set; }
    }

    public class SalesRevenueReportDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<SalesRevenueReportItem> Items { get; set; } = new();
        public int TotalQuotationCount { get; set; }
        public decimal GrandTotalGross { get; set; }
        public decimal GrandTotalNet { get; set; }
    }
    ```
13. Tạo `Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs`:
    ```csharp
    using OrderMgmt.Application.Reports.SalesRevenue.Models;

    namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

    public interface ISalesRevenueReportService
    {
        Task<SalesRevenueReportDto> GetAsync(SalesRevenueReportRequest request, CancellationToken ct = default);
    }
    ```
14. Tạo `Reports/SalesRevenue/Services/SalesRevenueReportService.cs`:
    - Inject `IAppDbContext`.
    - Query `_db.Quotations.AsNoTracking().Where(q => !q.IsDeleted && q.Status == QuotationStatus.Confirmed && q.CancelledAt == null && q.ConfirmedAt >= request.From && q.ConfirmedAt < request.To.AddDays(1))`.
    - Nếu `SaleUserId.HasValue` → filter `OwnerUserId == request.SaleUserId.Value`.
    - Group by `OwnerUserId`, select `{ OwnerUserId, Count = g.Count(), Gross = g.Sum(x => x.Total), Net = g.Sum(x => x.Subtotal - x.Discount) }`.
    - Join với `_db.Users.IgnoreQueryFilters()` để lấy `FullName` và `IsDeleted` flag.
    - Tính `GrandTotal*` và `TotalQuotationCount` từ items.
    - Sort items theo `TotalRevenueGross` desc.
15. Tạo `Reports/SalesRevenue/Validators/SalesRevenueReportRequestValidator.cs`:
    ```csharp
    using FluentValidation;
    using OrderMgmt.Application.Reports.SalesRevenue.Models;

    namespace OrderMgmt.Application.Reports.SalesRevenue.Validators;

    public class SalesRevenueReportRequestValidator : AbstractValidator<SalesRevenueReportRequest>
    {
        public SalesRevenueReportRequestValidator()
        {
            RuleFor(x => x.From).LessThanOrEqualTo(x => x.To)
                .WithMessage("From phải <= To.");
            RuleFor(x => x).Must(x => (x.To - x.From).TotalDays <= 366)
                .WithMessage("Khoảng thời gian tối đa 366 ngày.");
        }
    }
    ```

### F. ReportsController

16. Tạo `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`:
    ```csharp
    using FluentValidation;
    using Microsoft.AspNetCore.Mvc;
    using OrderMgmt.Application.Common.Models;
    using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
    using OrderMgmt.Application.Reports.SalesRevenue.Models;
    using OrderMgmt.Domain.Constants;
    using OrderMgmt.WebApi.Authorization;

    namespace OrderMgmt.WebApi.Controllers;

    public class ReportsController : ApiControllerBase
    {
        private readonly ISalesRevenueReportService _salesRevenue;
        private readonly IValidator<SalesRevenueReportRequest> _validator;

        public ReportsController(
            ISalesRevenueReportService salesRevenue,
            IValidator<SalesRevenueReportRequest> validator)
        {
            _salesRevenue = salesRevenue;
            _validator = validator;
        }

        [HttpGet("sales-revenue")]
        [HasPermission(Permissions.Reports.Revenue)]
        public async Task<ActionResult<ApiResponse<SalesRevenueReportDto>>> SalesRevenue(
            [FromQuery] SalesRevenueReportRequest request, CancellationToken ct)
        {
            await _validator.ValidateAndThrowAsync(request, ct);
            return Success(await _salesRevenue.GetAsync(request, ct));
        }
    }
    ```

### G. DI registration in `Program.cs`

17. Tìm chỗ DI register cho `IQuotationService` (gần `services.AddScoped<IQuotationService, QuotationService>();`). Thêm sau:
    ```csharp
    services.AddScoped<ISalesRevenueReportService, SalesRevenueReportService>();
    ```
    FluentValidation auto-discovery (`services.AddValidatorsFromAssembly(...)`) sẽ tự pick up validator mới — nếu đang dùng explicit registration, thêm `services.AddScoped<IValidator<SalesRevenueReportRequest>, SalesRevenueReportRequestValidator>();`.

### H. DbSeeder

18. Trong `DbSeeder.cs`, sửa `permissionDefs`:
    - Xóa dòng `(Permissions.Quotations.ConvertToOrder, ...)`.
    - Thêm: `(Permissions.Quotations.CancelConfirmed, Permissions.SalesModule, "Hủy báo giá đã xác nhận"),`.
    - Xóa toàn bộ 8 dòng `Permissions.Orders.*`.
    - Cân nhắc giữ `Permissions.Reports.Debt` / `Reports.Delivery` dòng — giữ nguyên (out of scope).
19. Trong `roleDefs`, sửa role `Sales`:
    - Xóa `Permissions.Quotations.ConvertToOrder` khỏi mảng.
    - Xóa `Permissions.Orders.View`, `Orders.Create`, `Orders.Update`, `Orders.Print`.
20. Sửa role `Accountant`:
    - Xóa `Permissions.Orders.View`, `Permissions.Orders.Pay`.
    - Cân nhắc xóa `Permissions.Reports.Debt` (out of scope) — giữ để dành.
21. Sửa role `Warehouse`:
    - Xóa `Permissions.Orders.View`, `Orders.Deliver`, `Orders.Print`.
    - Vì sau khi xóa, role `Warehouse` chỉ còn `Customers.View`, `Products.View`. Note: role này sẽ trống về tác vụ — chấp nhận (theo brainstorm, bàn giao đã out of scope).
22. Role `Manager` và `Admin` đã = allPermissions → tự động cover `CancelConfirmed`. Không sửa.

## Verification

- Compile sạch:
  - `dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj`
  - `dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj`
- Chạy unit/integration test hiện tại để chắc không regress:
  - `dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~Quotation" --no-build` — sẽ apply migration (Testcontainers) → snapshot bug nếu còn.
  - `QuotationStateMachineTests` cũ nên vẫn pass (Cancel từ Confirmed sẽ hỏi permission → admin user trong test có hết permission nên OK).
- KHÔNG restart WebApi đang chạy dev (theo memory). Nếu cần verify endpoint mới qua Swagger thì user sẽ tự restart sau khi review code.

## Exit Criteria

- Tất cả file Application/Infrastructure/WebApi build sạch.
- Không còn reference `ConvertedToOrder` hay `Permissions.Orders.*` trong backend code (`grep` không match).
- `ISalesRevenueReportService` + endpoint hoạt động (không cần test live ở phase này — Phase 04 sẽ test).
- Tests Quotation hiện có vẫn pass.
