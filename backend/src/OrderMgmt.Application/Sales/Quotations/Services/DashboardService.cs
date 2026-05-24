using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Options;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Services;

public class DashboardService : IDashboardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly IOptionsMonitor<FeatureOptions> _features;

    public DashboardService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDateTime clock,
        IOptionsMonitor<FeatureOptions> features)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _features = features;
    }

    private IQueryable<Quotation> ApplyScope(Guid? requestedSaleUserId)
    {
        var q = _db.Quotations.AsNoTracking().Where(x => !x.IsDeleted);
        if (!_features.CurrentValue.QuotationOwnerScope)
            return requestedSaleUserId.HasValue
                ? q.Where(x => x.OwnerUserId == requestedSaleUserId.Value)
                : q;
        if (_currentUser.HasPermission(Permissions.Quotations.ViewAll))
            return requestedSaleUserId.HasValue
                ? q.Where(x => x.OwnerUserId == requestedSaleUserId.Value)
                : q;
        var uid = _currentUser.UserId ?? Guid.Empty;
        return q.Where(x => x.OwnerUserId == uid);
    }

    private static decimal? DeltaOf(decimal cur, decimal prev)
        => prev == 0m ? null : Math.Round((cur - prev) / prev * 100m, 2, MidpointRounding.AwayFromZero);

    public async Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, Guid? saleUserId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(_clock.Now.DateTime);
        var rangeFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var rangeTo = to ?? today;
        if (rangeTo < rangeFrom) rangeTo = rangeFrom;
        var rangeLen = rangeTo.DayNumber - rangeFrom.DayNumber + 1;
        var prevTo = rangeFrom.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(rangeLen - 1));

        var curFromDt = rangeFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var curToDt = rangeTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var prevFromDt = prevFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var prevToDt = prevTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayDt = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var tomorrowDt = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var scope = ApplyScope(saleUserId);

        var stats = await scope
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CurRevenue = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= curFromDt && x.ConfirmedAt < curToDt ? x.Total : 0m),
                PrevRevenue = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= prevFromDt && x.ConfirmedAt < prevToDt ? x.Total : 0m),
                TodayRevenue = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= todayDt && x.ConfirmedAt < tomorrowDt ? x.Total : 0m),
                CurTotalCount = g.Sum(x => x.QuotationDate >= rangeFrom && x.QuotationDate <= rangeTo ? 1 : 0),
                PrevTotalCount = g.Sum(x => x.QuotationDate >= prevFrom && x.QuotationDate <= prevTo ? 1 : 0),
                CurCancelled = g.Sum(x =>
                    x.Status == QuotationStatus.Cancelled && x.CancelledAt != null
                    && x.CancelledAt >= curFromDt && x.CancelledAt < curToDt ? 1 : 0),
                PrevCancelled = g.Sum(x =>
                    x.Status == QuotationStatus.Cancelled && x.CancelledAt != null
                    && x.CancelledAt >= prevFromDt && x.CancelledAt < prevToDt ? 1 : 0),
            })
            .FirstOrDefaultAsync(ct);

        var curRevenue = stats?.CurRevenue ?? 0m;
        var prevRevenue = stats?.PrevRevenue ?? 0m;
        var todayRevenue = stats?.TodayRevenue ?? 0m;
        var curTotalCount = stats?.CurTotalCount ?? 0;
        var prevTotalCount = stats?.PrevTotalCount ?? 0;
        var curCancelled = stats?.CurCancelled ?? 0;
        var prevCancelled = stats?.PrevCancelled ?? 0;

        var sparkFromDt = today.AddDays(-6).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var sparkRows = await scope
            .Where(q => (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed) && q.CancelledAt == null && q.ConfirmedAt != null
                        && q.ConfirmedAt >= sparkFromDt && q.ConfirmedAt < tomorrowDt)
            .GroupBy(q => DateOnly.FromDateTime(q.ConfirmedAt!.Value))
            .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.Total), Count = g.Count() })
            .ToListAsync(ct);
        var sparkByDay = sparkRows.ToDictionary(r => r.Date, r => (r.Revenue, r.Count));
        var sparkRevenue = new decimal[7];
        var sparkCount = new decimal[7];
        for (var i = 0; i < 7; i++)
        {
            var d = today.AddDays(-6 + i);
            if (sparkByDay.TryGetValue(d, out var v))
            {
                sparkRevenue[i] = v.Revenue;
                sparkCount[i] = v.Count;
            }
        }

        var funnelRows = await scope
            .Where(q => q.QuotationDate >= rangeFrom && q.QuotationDate <= rangeTo)
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int draft = 0, sent = 0, confirmed = 0, cancelled = 0;
        foreach (var r in funnelRows)
        {
            switch (r.Status)
            {
                case QuotationStatus.Draft: draft = r.Count; break;
                case QuotationStatus.Sent: sent = r.Count; break;
                case QuotationStatus.Confirmed: confirmed = r.Count; break;
                case QuotationStatus.Cancelled: cancelled = r.Count; break;
            }
        }
        decimal? sentRate = draft == 0 ? null : Math.Round((decimal)(sent + confirmed) / draft * 100m, 2, MidpointRounding.AwayFromZero);
        decimal? confirmRate = sent == 0 ? null : Math.Round((decimal)confirmed / sent * 100m, 2, MidpointRounding.AwayFromZero);

        return new DashboardSummaryDto
        {
            From = rangeFrom,
            To = rangeTo,
            PrevFrom = prevFrom,
            PrevTo = prevTo,
            TodayRevenue = new KpiDto { Value = todayRevenue, DeltaPct = null, Spark = Array.Empty<decimal>() },
            RangeRevenue = new KpiDto { Value = curRevenue, DeltaPct = DeltaOf(curRevenue, prevRevenue), Spark = sparkRevenue },
            TotalCount = new KpiDto { Value = curTotalCount, DeltaPct = DeltaOf(curTotalCount, prevTotalCount), Spark = sparkCount },
            CancelledCount = new KpiDto { Value = curCancelled, DeltaPct = DeltaOf(curCancelled, prevCancelled), Spark = Array.Empty<decimal>() },
            Funnel = new FunnelDto
            {
                Draft = draft,
                Sent = sent,
                Confirmed = confirmed,
                Cancelled = cancelled,
                SentRate = sentRate,
                ConfirmRate = confirmRate,
            },
        };
    }

    public async Task<RevenueSeriesDto> GetRevenueSeriesAsync(DateOnly from, DateOnly to, string granularity, Guid? saleUserId, CancellationToken ct)
    {
        if (to < from) to = from;
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var dayRows = await ApplyScope(saleUserId)
            .Where(q => (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed) && q.CancelledAt == null && q.ConfirmedAt != null
                        && q.ConfirmedAt >= fromDt && q.ConfirmedAt < toDt)
            .GroupBy(q => DateOnly.FromDateTime(q.ConfirmedAt!.Value))
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Total), Count = g.Count() })
            .ToListAsync(ct);

        var byDay = dayRows.ToDictionary(r => r.Date, r => (r.Total, r.Count));
        var points = new List<RevenuePointDto>();

        if (string.Equals(granularity, "month", StringComparison.OrdinalIgnoreCase))
        {
            var months = new SortedDictionary<DateOnly, (decimal Total, int Count)>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var key = new DateOnly(d.Year, d.Month, 1);
                byDay.TryGetValue(d, out var v);
                months.TryGetValue(key, out var cur);
                months[key] = (cur.Total + v.Total, cur.Count + v.Count);
            }
            foreach (var kv in months)
                points.Add(new RevenuePointDto { Date = kv.Key, Total = kv.Value.Total, ConfirmedCount = kv.Value.Count });
        }
        else if (string.Equals(granularity, "week", StringComparison.OrdinalIgnoreCase))
        {
            var weeks = new SortedDictionary<DateOnly, (decimal Total, int Count)>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var offset = ((int)d.DayOfWeek + 6) % 7;
                var key = d.AddDays(-offset);
                byDay.TryGetValue(d, out var v);
                weeks.TryGetValue(key, out var cur);
                weeks[key] = (cur.Total + v.Total, cur.Count + v.Count);
            }
            foreach (var kv in weeks)
                points.Add(new RevenuePointDto { Date = kv.Key, Total = kv.Value.Total, ConfirmedCount = kv.Value.Count });
        }
        else
        {
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                byDay.TryGetValue(d, out var v);
                points.Add(new RevenuePointDto { Date = d, Total = v.Total, ConfirmedCount = v.Count });
            }
        }

        return new RevenueSeriesDto { Points = points };
    }

    public async Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct)
    {
        if (limit <= 0) limit = 5;
        if (to < from) to = from;
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await ApplyScope(saleUserId)
            .Where(q => (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed) && q.CancelledAt == null && q.ConfirmedAt != null
                        && q.ConfirmedAt >= fromDt && q.ConfirmedAt < toDt)
            .GroupBy(q => new { q.CustomerId, q.CustomerName })
            .Select(g => new TopCustomerDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                Revenue = g.Sum(x => x.Total),
                QuotationCount = g.Count(),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct)
    {
        if (limit <= 0) limit = 5;
        if (to < from) to = from;
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var quotationIds = ApplyScope(saleUserId)
            .Where(q => (q.Status == QuotationStatus.Confirmed || q.Status == QuotationStatus.AccountingConfirmed) && q.CancelledAt == null && q.ConfirmedAt != null
                        && q.ConfirmedAt >= fromDt && q.ConfirmedAt < toDt)
            .Select(q => q.Id);

        return await _db.QuotationLines
            .AsNoTracking()
            .Where(l => quotationIds.Contains(l.QuotationId))
            .GroupBy(l => new { l.ProductId, l.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Revenue = g.Sum(l => l.LineTotal),
                Quantity = g.Sum(l => l.Quantity),
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ActivityItemDto>> GetRecentActivityAsync(int limit, CancellationToken ct)
    {
        if (limit <= 0) limit = 10;
        var fetchPerSource = Math.Max(limit, 30);
        var scope = ApplyScope(null);

        var createdRows = await scope
            .OrderByDescending(q => q.CreatedAt)
            .Take(fetchPerSource)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.CustomerName,
                At = q.CreatedAt,
                ActorId = q.CreatedBy,
                q.Total,
            })
            .ToListAsync(ct);

        var confirmedRows = await scope
            .Where(q => q.ConfirmedAt != null)
            .OrderByDescending(q => q.ConfirmedAt)
            .Take(fetchPerSource)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.CustomerName,
                At = q.ConfirmedAt!.Value,
                ActorId = q.ConfirmedByUserId,
                q.Total,
            })
            .ToListAsync(ct);

        var cancelledRows = await scope
            .Where(q => q.CancelledAt != null)
            .OrderByDescending(q => q.CancelledAt)
            .Take(fetchPerSource)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.CustomerName,
                At = q.CancelledAt!.Value,
                q.Total,
            })
            .ToListAsync(ct);

        var actorIds = createdRows.Select(r => r.ActorId)
            .Concat(confirmedRows.Select(r => r.ActorId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var nameMap = actorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        string? LookupName(Guid? id) =>
            id.HasValue && nameMap.TryGetValue(id.Value, out var n) ? n : null;

        var created = createdRows.Select(r => new ActivityItemDto
        {
            At = r.At.UtcDateTime,
            Type = "created",
            QuotationId = r.Id,
            Code = r.Code,
            CustomerName = r.CustomerName,
            ActorName = LookupName(r.ActorId),
            Amount = r.Total,
        });

        var confirmed = confirmedRows.Select(r => new ActivityItemDto
        {
            At = r.At,
            Type = "confirmed",
            QuotationId = r.Id,
            Code = r.Code,
            CustomerName = r.CustomerName,
            ActorName = LookupName(r.ActorId),
            Amount = r.Total,
        });

        var cancelled = cancelledRows.Select(r => new ActivityItemDto
        {
            At = r.At,
            Type = "cancelled",
            QuotationId = r.Id,
            Code = r.Code,
            CustomerName = r.CustomerName,
            ActorName = null,
            Amount = r.Total,
        });

        return created.Concat(confirmed).Concat(cancelled)
            .OrderByDescending(a => a.At)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyList<SalesLeaderboardItemDto>> GetSalesLeaderboardAsync(DateOnly from, DateOnly to, int limit, CancellationToken ct)
    {
        if (!_currentUser.HasPermission(Permissions.Quotations.ViewAll))
            throw new ForbiddenException("Bạn không có quyền xem bảng xếp hạng sale.");

        if (limit <= 0) limit = 10;
        if (to < from) to = from;
        var rangeLen = to.DayNumber - from.DayNumber + 1;
        var prevTo = from.AddDays(-1);
        var prevFrom = prevTo.AddDays(-(rangeLen - 1));

        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var prevFromDt = prevFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var prevToDt = prevTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await _db.Quotations
            .AsNoTracking()
            .Where(q => !q.IsDeleted)
            .GroupBy(q => q.OwnerUserId)
            .Select(g => new
            {
                UserId = g.Key,
                Revenue = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= fromDt && x.ConfirmedAt < toDt ? x.Total : 0m),
                ConfirmedCount = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= fromDt && x.ConfirmedAt < toDt ? 1 : 0),
                TotalCount = g.Sum(x => x.QuotationDate >= from && x.QuotationDate <= to ? 1 : 0),
                PrevRevenue = g.Sum(x =>
                    (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed) && x.CancelledAt == null && x.ConfirmedAt != null
                    && x.ConfirmedAt >= prevFromDt && x.ConfirmedAt < prevToDt ? x.Total : 0m),
            })
            .ToListAsync(ct);

        var topUserIds = rows
            .OrderByDescending(r => r.Revenue)
            .Take(limit)
            .Select(r => r.UserId)
            .ToList();

        var names = await _db.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => topUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        var nameMap = names.ToDictionary(n => n.Id, n => n.FullName);

        return rows
            .OrderByDescending(r => r.Revenue)
            .Take(limit)
            .Select(r => new SalesLeaderboardItemDto
            {
                UserId = r.UserId,
                FullName = nameMap.GetValueOrDefault(r.UserId) ?? "—",
                Revenue = r.Revenue,
                ConfirmedCount = r.ConfirmedCount,
                ConversionRate = r.TotalCount == 0
                    ? null
                    : Math.Round((decimal)r.ConfirmedCount / r.TotalCount * 100m, 2, MidpointRounding.AwayFromZero),
                DeltaPct = DeltaOf(r.Revenue, r.PrevRevenue),
            })
            .ToList();
    }
}
