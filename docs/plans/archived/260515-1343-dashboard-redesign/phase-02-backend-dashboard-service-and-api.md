# Phase 02 — Backend Dashboard Service + 6 endpoints

**Status:** [ ] pending
**Complexity:** L

## Objective

Xây service tổng hợp data cho dashboard mới và 6 endpoint HTTP. Tất cả tôn trọng scoping theo `ICurrentUser.HasPermission("quotations.view_all")` — không tin client `saleUserId`. Revenue rule: `Status = Confirmed AND CancelledAt IS NULL AND COALESCE(ConfirmedAt, QuotationDate) ∈ [from, to]`.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/DashboardModels.cs` (new) — 6 DTO + nested types
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IDashboardService.cs` (new) — interface tổng
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/DashboardService.cs` (new) — implementation
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (edit) — đăng ký `IDashboardService`
- `backend/src/OrderMgmt.WebApi/Controllers/DashboardController.cs` (edit) — thêm 6 action mới (giữ `quotation-stats` legacy)

## Tasks

### A. DTOs

1. Tạo `DashboardModels.cs` với 6 DTO + types phụ:

```csharp
namespace OrderMgmt.Application.Sales.Quotations.Models;

public sealed class DashboardSummaryDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public DateOnly PrevFrom { get; init; }
    public DateOnly PrevTo { get; init; }
    public KpiDto TodayRevenue { get; init; } = default!;
    public KpiDto RangeRevenue { get; init; } = default!;
    public KpiDto TotalCount { get; init; } = default!;
    public KpiDto CancelledCount { get; init; } = default!;
    public FunnelDto Funnel { get; init; } = default!;
}

public sealed class KpiDto
{
    public decimal Value { get; init; }
    public decimal? DeltaPct { get; init; }       // null nếu previous = 0
    public IReadOnlyList<decimal> Spark { get; init; } = Array.Empty<decimal>();
}

public sealed class FunnelDto
{
    public int Draft { get; init; }
    public int Sent { get; init; }
    public int Confirmed { get; init; }
    public int Cancelled { get; init; }
    public decimal? SentRate { get; init; }       // (Sent+Confirmed)/Draft, null nếu Draft=0
    public decimal? ConfirmRate { get; init; }    // Confirmed/Sent, null nếu Sent=0
}

public sealed class RevenueSeriesDto
{
    public IReadOnlyList<RevenuePointDto> Points { get; init; } = Array.Empty<RevenuePointDto>();
}

public sealed class RevenuePointDto
{
    public DateOnly Date { get; init; }
    public decimal Total { get; init; }
    public int ConfirmedCount { get; init; }
}

public sealed class TopCustomerDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = default!;
    public decimal Revenue { get; init; }
    public int QuotationCount { get; init; }
}

public sealed class TopProductDto
{
    public Guid? ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public decimal Revenue { get; init; }
    public decimal Quantity { get; init; }
}

public sealed class ActivityItemDto
{
    public DateTime At { get; init; }
    public string Type { get; init; } = default!;  // "created" | "sent" | "confirmed" | "cancelled"
    public Guid QuotationId { get; init; }
    public string Code { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public string? ActorName { get; init; }
    public decimal? Amount { get; init; }
}

public sealed class SalesLeaderboardItemDto
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = default!;
    public decimal Revenue { get; init; }
    public int ConfirmedCount { get; init; }
    public decimal? ConversionRate { get; init; }
    public decimal? DeltaPct { get; init; }
}
```

### B. Interface

```csharp
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, Guid? saleUserId, CancellationToken ct);
    Task<RevenueSeriesDto> GetRevenueSeriesAsync(DateOnly from, DateOnly to, string granularity, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<ActivityItemDto>> GetRecentActivityAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<SalesLeaderboardItemDto>> GetSalesLeaderboardAsync(DateOnly from, DateOnly to, int limit, CancellationToken ct);
}
```

### C. Service implementation

1. Tạo `DashboardService` dùng pattern y hệt `QuotationDashboardService` — inject `IAppDbContext`, `ICurrentUser`, `IDateTime`, `IOptionsMonitor<FeatureOptions>`.
2. Helper `ApplyOwnerScope(IQueryable<Quotation> q, Guid? requestedSaleUserId)`:
   - Nếu `!_features.CurrentValue.QuotationOwnerScope` → return as-is.
   - Nếu user có `Permissions.Quotations.ViewAll` → áp `requestedSaleUserId` nếu có (admin filter), không thì return all.
   - Ngược lại → ép `q.Where(x => x.OwnerUserId == currentUserId)` (ignore `requestedSaleUserId`).
3. Helper `RevenueDate(Quotation q) => q.ConfirmedAt != null ? DateOnly.FromDateTime(q.ConfirmedAt.Value) : q.QuotationDate` — không expression-translatable, dùng raw `(q.ConfirmedAt ?? q.UpdatedAt).Date` trong query, hoặc `EF.Functions.Coalesce`. **Đề xuất**: dùng `(q.ConfirmedAt ?? q.QuotationDate.ToDateTime(TimeOnly.MinValue))` trong projection để EF translate được.
4. **GetSummaryAsync**:
   - Default range: tháng hiện tại.
   - Previous range: cùng độ dài, lùi liền kề (`prevTo = from.AddDays(-1)`, `prevFrom = prevTo.AddDays(-(rangeLength-1))`).
   - 1 query lấy current + previous + today bằng `CASE WHEN` để tránh N+1:
     ```csharp
     var stats = await scoped
       .Where(q => q.Status == Confirmed && q.CancelledAt == null)
       .GroupBy(_ => 1)
       .Select(g => new {
         CurRevenue = g.Sum(x => InRange(x, from, to) ? x.Total : 0),
         PrevRevenue = g.Sum(x => InRange(x, prevFrom, prevTo) ? x.Total : 0),
         TodayRevenue = g.Sum(x => OnDate(x, today) ? x.Total : 0),
         CurCount = g.Sum(x => InRange(x, from, to) ? 1 : 0),
         ...
       }).FirstOrDefaultAsync(ct);
     ```
   - Sparkline 7 ngày: query riêng nhỏ, group theo `RevenueDate`:
     ```csharp
     var spark = await scoped
       .Where(q => q.Status == Confirmed && q.CancelledAt == null
              && RevenueDate(q) >= today.AddDays(-6) && RevenueDate(q) <= today)
       .GroupBy(q => RevenueDate(q))
       .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() })
       .ToListAsync(ct);
     ```
     Fill zero cho ngày trống.
   - Funnel: 1 query group by `Status` trong range, scoped, lấy count.
   - Tính `DeltaPct`: `prev = 0 ? null : (cur - prev) / prev * 100`.
5. **GetRevenueSeriesAsync**:
   - `granularity ∈ {"day","week","month"}`. Default `day` nếu range ≤ 31 ngày, `week` nếu ≤ 180, `month` nếu lớn hơn — nhưng tôn trọng query param trước.
   - Group theo expression DateOnly truncate:
     - day: `RevenueDate(q)`
     - week: `RevenueDate(q).AddDays(-(int)RevenueDate(q).DayOfWeek)` (Sunday start) hoặc dùng `EF.Functions.DateTrunc("week", ...)` qua Npgsql function mapping.
     - month: `new DateOnly(year, month, 1)`.
   - Fill zero cho point không có data (server-side loop sau khi `ToListAsync`).
6. **GetTopCustomersAsync** / **GetTopProductsAsync**:
   - Customer: group `CustomerId, CustomerName`, sum `Total`, count quotations, order desc, take `limit`.
   - Product: join với `QuotationLines`, group `ProductId, ProductName`, sum `LineTotal`, sum `Quantity`. Note: revenue product = sum line total trong quotation đã `Confirmed`, không phải quotation total.
7. **GetRecentActivityAsync**:
   - Limit 10 (tham số), không cần range.
   - Union 4 nguồn: created (theo `CreatedAt`), sent (cần `SentAt`? — **không có field** → skip type "sent" hoặc lấy từ audit log nếu có; **đề xuất**: chỉ trả "created" + "confirmed" + "cancelled" trong v1).
   - Sort by timestamp desc.
8. **GetSalesLeaderboardAsync**:
   - Yêu cầu `Permissions.Quotations.ViewAll`. Nếu không có → throw `ForbiddenException` (kiểm tra exception class trong [backend/src/OrderMgmt.Domain/Common/](../../../backend/src/OrderMgmt.Domain/Common/) — có thể dùng `UnauthorizedAccessException` hoặc tự throw `DomainException` 403).
   - Join `Users` (table `users`); group theo `OwnerUserId, User.FullName`; sum `Total` cho confirmed-in-range; count.
   - Previous range delta: tính 2 lần và join trong memory.

### D. Controller

Sửa [DashboardController.cs](../../../backend/src/OrderMgmt.WebApi/Controllers/DashboardController.cs):

```csharp
[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboard;
    private readonly IQuotationDashboardService _legacy;  // giữ /quotation-stats

    public DashboardController(IDashboardService dashboard, IQuotationDashboardService legacy)
    { _dashboard = dashboard; _legacy = legacy; }

    // existing /quotation-stats giữ nguyên

    [HttpGet("summary")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> Summary(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] Guid? saleUserId, CancellationToken ct)
        => Success(await _dashboard.GetSummaryAsync(from, to, saleUserId, ct));

    [HttpGet("revenue-series")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<RevenueSeriesDto>>> RevenueSeries(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] string granularity = "day",
        [FromQuery] Guid? saleUserId = null, CancellationToken ct = default)
        => Success(await _dashboard.GetRevenueSeriesAsync(from, to, granularity, saleUserId, ct));

    [HttpGet("top-customers")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopCustomerDto>>>> TopCustomers(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] int limit = 5, [FromQuery] Guid? saleUserId = null,
        CancellationToken ct = default)
        => Success(await _dashboard.GetTopCustomersAsync(from, to, limit, saleUserId, ct));

    [HttpGet("top-products")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>>> TopProducts(...) { ... }

    [HttpGet("recent-activity")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityItemDto>>>> RecentActivity(
        [FromQuery] int limit = 10, CancellationToken ct = default)
        => Success(await _dashboard.GetRecentActivityAsync(limit, ct));

    [HttpGet("sales-leaderboard")]
    [HasPermission(Permissions.Quotations.ViewAll)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesLeaderboardItemDto>>>> Leaderboard(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to,
        [FromQuery] int limit = 10, CancellationToken ct = default)
        => Success(await _dashboard.GetSalesLeaderboardAsync(from, to, limit, ct));
}
```

### E. DI

Sửa [DependencyInjection.cs](../../../backend/src/OrderMgmt.Application/DependencyInjection.cs) trong Application:
```csharp
services.AddScoped<IDashboardService, DashboardService>();
```

## Verification

```powershell
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
```

WebApi đang chạy sẽ tự reload (hot reload `dotnet watch`) nếu được start bằng watch; nếu không, **đừng restart** — chỉ verify build pass theo memory `feedback_build_skip_when_app_running`. Manual cURL/Postman test ở Phase 06.

Manual API smoke (nếu WebApi đang chạy):
```powershell
$token = "...(login token)..."
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Headers $headers "http://localhost:5000/api/dashboard/summary"
Invoke-RestMethod -Headers $headers "http://localhost:5000/api/dashboard/revenue-series?from=2026-05-01&to=2026-05-15&granularity=day"
Invoke-RestMethod -Headers $headers "http://localhost:5000/api/dashboard/sales-leaderboard?from=2026-05-01&to=2026-05-15"
```

## Exit Criteria

- 6 endpoint mới trả `ApiResponse<T>` đúng schema, status 200 cho user hợp lệ.
- `sales-leaderboard` trả 403 cho user không có `quotations.view_all`.
- SALES gọi `/summary?saleUserId=<other>` vẫn chỉ thấy data riêng (ignored param).
- ADMIN gọi `/summary?saleUserId=<x>` trả đúng data của sale `x`.
- 2 library build pass, không cảnh báo unused/EF translation lỗi.
- Không có N+1: kiểm tra log SQL (Serilog) cho `/summary` → ≤ 3 query (1 stats + 1 spark + 1 funnel hoặc gộp).
