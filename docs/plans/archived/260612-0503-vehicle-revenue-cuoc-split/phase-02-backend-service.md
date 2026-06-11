# Phase 02 — Backend Service rewrite

**Status:** [ ] pending
**Complexity:** M

## Objective

Rewrite `VehicleRevenueReportService` để query ở cấp `QuotationLine`, filter `ProductCode.ToLower() == "cuoc"`, và tổng hợp theo dấu của `LineTotal`. Làm cho tất cả integration tests từ Phase 01 pass.

## Files

- `backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Services/VehicleRevenueReportService.cs`

## Tasks

### Task 1 — Chạy tests để xác nhận chúng đang FAIL

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~VehicleRevenueReportTests" \
  --no-build
```
Expected: FAIL (service trả dữ liệu sai hoặc exception do DTO cũ).

### Task 2 — Rewrite `VehicleRevenueReportService.cs`

Thay toàn bộ nội dung thành:

```csharp
using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Reports.Common;
using OrderMgmt.Application.Reports.VehicleRevenue.Interfaces;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Reports.VehicleRevenue.Services;

public class VehicleRevenueReportService : IVehicleRevenueReportService
{
    private const string DefaultVehicleNumber = "Xe khác";
    private const string CuocCode = "cuoc";
    private readonly IAppDbContext _db;

    public VehicleRevenueReportService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<VehicleRevenueReportDto> GetAsync(VehicleRevenueReportRequest request, CancellationToken ct = default)
    {
        var dateMode = await RevenueFilterHelper.GetDateModeAsync(_db, ct);
        var from = request.From!.Value;
        var to = request.To!.Value;

        var scope = _db.Quotations.AsNoTracking();

        // ── Summary table ─────────────────────────────────────────────────────
        var summaryRows = await RevenueFilterHelper.ApplyRevenueFilter(scope, dateMode, from, to)
            .SelectMany(q => q.Lines
                .Where(l => l.ProductCode != null && l.ProductCode.ToLower() == CuocCode)
                .Select(l => new
                {
                    VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                        ? DefaultVehicleNumber
                        : q.TransportVehicleNumber.Trim(),
                    l.LineTotal,
                }))
            .GroupBy(x => x.VehicleNumber)
            .Select(g => new VehicleRevenueReportItem
            {
                VehicleNumber = g.Key,
                CompanyVehicleRevenue = g.Where(x => x.LineTotal > 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                ExternalVehicleRevenue = g.Where(x => x.LineTotal < 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
            })
            .OrderByDescending(x => x.CompanyVehicleRevenue)
            .ThenBy(x => x.VehicleNumber)
            .ToListAsync(ct);

        // ── Monthly chart series ──────────────────────────────────────────────
        // Chart range khác summary range: chart tính ngược `Months` tháng từ tháng chứa `to`,
        // bất kể `from` của request là ngày nào. Bảng tóm tắt dùng `from/to` của request.
        // Đây là intentional: chart hiển thị xu hướng, bảng hiển thị kỳ được lọc.
        var chartEndMonth = new DateOnly(to.Year, to.Month, 1);
        var chartStartMonth = chartEndMonth.AddMonths(-(request.Months - 1));
        var chartTo = chartEndMonth.AddMonths(1).AddDays(-1);

        var chartFiltered = RevenueFilterHelper.ApplyRevenueFilter(scope, dateMode, chartStartMonth, chartTo);

        List<(int Year, int Month, decimal CompanyTotal, decimal ExternalTotal)> chartAggregates;

        if (dateMode == RevenueDateField.AccountingConfirmedAt)
        {
            chartAggregates = (await chartFiltered
                .SelectMany(q => q.Lines
                    .Where(l => l.ProductCode != null && l.ProductCode.ToLower() == CuocCode)
                    .Select(l => new
                    {
                        Year = q.AccountingConfirmedAt!.Value.Year,
                        Month = q.AccountingConfirmedAt!.Value.Month,
                        l.LineTotal,
                    }))
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    CompanyTotal  = g.Where(x => x.LineTotal > 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                    ExternalTotal = g.Where(x => x.LineTotal < 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                })
                .ToListAsync(ct))
                .Select(r => (r.Year, r.Month, r.CompanyTotal, r.ExternalTotal)).ToList();
        }
        else if (dateMode == RevenueDateField.ConfirmedAt)
        {
            chartAggregates = (await chartFiltered
                .SelectMany(q => q.Lines
                    .Where(l => l.ProductCode != null && l.ProductCode.ToLower() == CuocCode)
                    .Select(l => new
                    {
                        Year = q.ConfirmedAt!.Value.Year,
                        Month = q.ConfirmedAt!.Value.Month,
                        l.LineTotal,
                    }))
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    CompanyTotal  = g.Where(x => x.LineTotal > 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                    ExternalTotal = g.Where(x => x.LineTotal < 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                })
                .ToListAsync(ct))
                .Select(r => (r.Year, r.Month, r.CompanyTotal, r.ExternalTotal)).ToList();
        }
        else // QuotationDate
        {
            chartAggregates = (await chartFiltered
                .SelectMany(q => q.Lines
                    .Where(l => l.ProductCode != null && l.ProductCode.ToLower() == CuocCode)
                    .Select(l => new
                    {
                        Year = q.QuotationDate.Year,
                        Month = q.QuotationDate.Month,
                        l.LineTotal,
                    }))
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    CompanyTotal  = g.Where(x => x.LineTotal > 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                    ExternalTotal = g.Where(x => x.LineTotal < 0).Sum(x => (decimal?)x.LineTotal) ?? 0m,
                })
                .ToListAsync(ct))
                .Select(r => (r.Year, r.Month, r.CompanyTotal, r.ExternalTotal)).ToList();
        }

        var chartLookup = chartAggregates.ToDictionary(x => (x.Year, x.Month));

        var monthlySeries = Enumerable.Range(0, request.Months)
            .Select(offset =>
            {
                var month = chartStartMonth.AddMonths(offset);
                chartLookup.TryGetValue((month.Year, month.Month), out var agg);
                return new VehicleRevenueMonthlyPoint
                {
                    Month = $"{month:yyyy-MM}",
                    CompanyTotal  = agg.CompanyTotal,
                    ExternalTotal = agg.ExternalTotal,
                };
            })
            .ToList();

        return new VehicleRevenueReportDto
        {
            From = from,
            To = to,
            Months = request.Months,
            Items = summaryRows,
            MonthlySeries = monthlySeries,
            GrandTotalCompany  = summaryRows.Sum(x => x.CompanyVehicleRevenue),
            GrandTotalExternal = summaryRows.Sum(x => x.ExternalVehicleRevenue),
        };
    }
}
```

**Điểm quan trọng:**
- `(decimal?)x.LineTotal ?? 0m` trong `Sum` — tránh `null` khi không có dòng thỏa điều kiện trong group.
- `chartLookup.TryGetValue(...)` trả về default tuple `(0, 0, 0m, 0m)` nếu tháng không có data — `agg.CompanyTotal` và `agg.ExternalTotal` đều `= 0m`.
- Constant `CuocCode = "cuoc"` để tránh magic string lặp lại.

### Task 3 — Build Application layer

```bash
dotnet build backend/src/OrderMgmt.Application
```
Expected: BUILD SUCCEEDED.

### Task 4 — Build WebApi layer

```bash
dotnet build backend/src/OrderMgmt.WebApi
```
Expected: BUILD SUCCEEDED. (Controller không thay đổi vì endpoint URL và service interface giữ nguyên.)

### Task 5 — Run integration tests

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~VehicleRevenueReportTests"
```
Expected: tất cả 9 test PASS.

Nếu có test fail vì SQL translation (EF Core không dịch được expression), xem lỗi cụ thể và điều chỉnh query bằng cách:
- Nếu `.ToLower()` không translate: dùng `EF.Functions.ILike(l.ProductCode, CuocCode)` thay thế
- Nếu `Sum(x => (decimal?)x.LineTotal)` không translate: dùng `.Sum(x => x.LineTotal > 0 ? x.LineTotal : 0m)`

### Task 6 — Commit

```bash
git add backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Services/VehicleRevenueReportService.cs
git commit -m "feat(vehicle-revenue): rewrite service to split revenue by cuoc ProductCode sign"
```

## Verification

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~VehicleRevenueReportTests"
```

## Exit Criteria

- 9/9 integration tests trong `VehicleRevenueReportTests` pass
- `dotnet build backend/src/OrderMgmt.WebApi` không có warning/error mới
