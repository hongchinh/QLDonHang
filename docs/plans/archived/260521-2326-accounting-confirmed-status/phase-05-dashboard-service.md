# Phase 05 — Dashboard Service Update

**Status:** [ ] pending
**Complexity:** S

## Objective

Cập nhật `QuotationDashboardService` để:
1. Luôn tính `AccountingConfirmedCount` + `AccountingConfirmedRevenue`
2. Áp dụng config `RevenueReportingDateField` cho date range filter

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationDashboardService.cs`

## Tasks

1. **Inject `IAppDbContext` access to `QuotationSystemSettings`** — service đã có `_db`, chỉ cần dùng thêm `_db.QuotationSystemSettings`.

2. **`GetStatsAsync`** — thay thế logic hiện tại bằng logic mới:

   ```csharp
   public async Task<QuotationStatsDto> GetStatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
   {
       var today = DateOnly.FromDateTime(_clock.Now.DateTime);
       var rangeFrom = from ?? new DateOnly(today.Year, today.Month, 1);
       var rangeTo = to ?? today;

       // Load config (Id=1 always exists via seed)
       var settings = await _db.QuotationSystemSettings
           .AsNoTracking()
           .FirstAsync(s => s.Id == 1, ct);
       var dateMode = settings.RevenueReportingDateField;

       var baseQuery = ApplyOwnerScope(_db.Quotations.AsNoTracking().Where(q => !q.IsDeleted));

       // Build date-filtered query based on config.
       // Dùng ToDateTime() thay vì DateOnly.FromDateTime() vì EF/Npgsql không translate
       // DateOnly.FromDateTime(col) sang SQL — sẽ throw InvalidOperationException at runtime.
       var fromDt = rangeFrom.ToDateTime(TimeOnly.MinValue);
       var toDt   = rangeTo.ToDateTime(TimeOnly.MaxValue);

       IQueryable<Quotation> rangeQuery = dateMode switch
       {
           "ConfirmedAt" => baseQuery.Where(q =>
               (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed)
               && q.ConfirmedAt != null
               && q.ConfirmedAt >= fromDt
               && q.ConfirmedAt <= toDt),
           "AccountingConfirmedAt" => baseQuery.Where(q =>
               q.Status == QuotationStatus.AccountingConfirmed
               && q.AccountingConfirmedAt != null
               && q.AccountingConfirmedAt >= fromDt
               && q.AccountingConfirmedAt <= toDt),
           _ => baseQuery.Where(q => q.QuotationDate >= rangeFrom && q.QuotationDate <= rangeTo),
       };

       var grouped = await rangeQuery
           .GroupBy(q => q.Status)
           .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(x => (decimal?)x.Total) ?? 0m })
           .ToListAsync(ct);

       var dto = new QuotationStatsDto { From = rangeFrom, To = rangeTo };

       foreach (var row in grouped)
       {
           dto.TotalCount += row.Count;
           switch (row.Status)
           {
               case QuotationStatus.Draft: dto.DraftCount = row.Count; break;
               case QuotationStatus.Sent: dto.SentCount = row.Count; break;
               case QuotationStatus.Confirmed: dto.ConfirmedCount = row.Count; break;
               case QuotationStatus.AccountingConfirmed:
                   dto.AccountingConfirmedCount = row.Count;
                   dto.AccountingConfirmedRevenue = row.Revenue;
                   break;
               case QuotationStatus.Cancelled: dto.CancelledCount = row.Count; break;
           }
           if (row.Status != QuotationStatus.Cancelled)
               dto.TotalRevenue += row.Revenue;
       }

       // TodayRevenue: theo cùng dateMode nhưng cho "hôm nay" (single day range)
       var todayStart = today.ToDateTime(TimeOnly.MinValue);
       var todayEnd   = today.ToDateTime(TimeOnly.MaxValue);

       IQueryable<Quotation> todayQuery = dateMode switch
       {
           "ConfirmedAt" => baseQuery.Where(q =>
               (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed)
               && q.ConfirmedAt != null
               && q.ConfirmedAt >= todayStart
               && q.ConfirmedAt <= todayEnd),
           "AccountingConfirmedAt" => baseQuery.Where(q =>
               q.Status == QuotationStatus.AccountingConfirmed
               && q.AccountingConfirmedAt != null
               && q.AccountingConfirmedAt >= todayStart
               && q.AccountingConfirmedAt <= todayEnd),
           _ => baseQuery.Where(q => q.QuotationDate == today && q.Status != QuotationStatus.Cancelled),
       };

       dto.TodayRevenue = await todayQuery
           .SumAsync(q => (decimal?)q.Total, ct) ?? 0m;

       return dto;
   }
   ```


## Verification

```bash
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo --filter "Dashboard"
```

## Exit Criteria

- Application build thành công
- `AccountingConfirmedCount` và `AccountingConfirmedRevenue` luôn xuất hiện trong response (0 nếu không có)
- Khi `dateMode = "QuotationDate"`: behavior giống hệt trước thay đổi
- Khi `dateMode = "AccountingConfirmedAt"`: chỉ đếm `AccountingConfirmed` quotations, filter theo `AccountingConfirmedAt`
