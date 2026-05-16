using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Reports.SalesRevenue.Services;

public class SalesRevenueReportService : ISalesRevenueReportService
{
    private readonly IAppDbContext _db;

    public SalesRevenueReportService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<SalesRevenueReportDto> GetAsync(SalesRevenueReportRequest request, CancellationToken ct = default)
    {
        var fromUtc = DateTime.SpecifyKind(request.From.Date, DateTimeKind.Utc);
        var toExclusiveUtc = DateTime.SpecifyKind(request.To.Date.AddDays(1), DateTimeKind.Utc);

        var quotations = _db.Quotations.AsNoTracking()
            .Where(q => q.Status == QuotationStatus.Confirmed
                && q.CancelledAt == null
                && q.ConfirmedAt != null
                && q.ConfirmedAt >= fromUtc
                && q.ConfirmedAt < toExclusiveUtc);

        if (request.SaleUserId.HasValue)
            quotations = quotations.Where(q => q.OwnerUserId == request.SaleUserId.Value);

        var grouped = await quotations
            .GroupBy(q => q.OwnerUserId)
            .Select(g => new
            {
                OwnerUserId = g.Key,
                Count = g.Count(),
                Gross = g.Sum(x => x.Total),
                Net = g.Sum(x => x.Subtotal - x.Discount),
            })
            .ToListAsync(ct);

        var ownerIds = grouped.Select(x => x.OwnerUserId).ToList();
        var owners = await _db.Users.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.IsDeleted })
            .ToListAsync(ct);
        var ownerMap = owners.ToDictionary(o => o.Id);

        var items = grouped
            .Select(g =>
            {
                ownerMap.TryGetValue(g.OwnerUserId, out var owner);
                return new SalesRevenueReportItem
                {
                    SaleUserId = g.OwnerUserId,
                    SaleName = owner?.FullName ?? "(không xác định)",
                    IsSaleDeleted = owner?.IsDeleted ?? false,
                    QuotationCount = g.Count,
                    TotalRevenueGross = g.Gross,
                    TotalRevenueNet = g.Net,
                };
            })
            .OrderByDescending(i => i.TotalRevenueGross)
            .ToList();

        return new SalesRevenueReportDto
        {
            From = request.From.Date,
            To = request.To.Date,
            Items = items,
            TotalQuotationCount = items.Sum(i => i.QuotationCount),
            GrandTotalGross = items.Sum(i => i.TotalRevenueGross),
            GrandTotalNet = items.Sum(i => i.TotalRevenueNet),
        };
    }
}
