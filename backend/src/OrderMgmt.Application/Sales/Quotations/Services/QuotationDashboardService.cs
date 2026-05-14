using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Options;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Services;

public class QuotationDashboardService : IQuotationDashboardService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTime _clock;
    private readonly IOptionsMonitor<FeatureOptions> _features;

    public QuotationDashboardService(
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

    public async Task<QuotationStatsDto> GetStatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_clock.Now.DateTime);
        var rangeFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var rangeTo = to ?? today;

        var query = ApplyOwnerScope(_db.Quotations.AsNoTracking().Where(q => !q.IsDeleted))
            .Where(q => q.QuotationDate >= rangeFrom && q.QuotationDate <= rangeTo);

        var grouped = await query
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(x => (decimal?)x.Total) ?? 0m })
            .ToListAsync(ct);

        var dto = new QuotationStatsDto
        {
            From = rangeFrom,
            To = rangeTo,
        };

        foreach (var row in grouped)
        {
            dto.TotalCount += row.Count;
            switch (row.Status)
            {
                case QuotationStatus.Draft: dto.DraftCount = row.Count; break;
                case QuotationStatus.Sent: dto.SentCount = row.Count; break;
                case QuotationStatus.Confirmed: dto.ConfirmedCount = row.Count; break;
                case QuotationStatus.ConvertedToOrder: dto.ConvertedCount = row.Count; break;
                case QuotationStatus.Cancelled: dto.CancelledCount = row.Count; break;
            }
            if (row.Status != QuotationStatus.Cancelled)
                dto.TotalRevenue += row.Revenue;
        }

        dto.TodayRevenue = await ApplyOwnerScope(_db.Quotations.AsNoTracking().Where(q => !q.IsDeleted))
            .Where(q => q.QuotationDate == today && q.Status != QuotationStatus.Cancelled)
            .SumAsync(q => (decimal?)q.Total, ct) ?? 0m;

        return dto;
    }

    private IQueryable<Quotation> ApplyOwnerScope(IQueryable<Quotation> query)
    {
        if (!_features.CurrentValue.QuotationOwnerScope) return query;
        if (_currentUser.HasPermission(Permissions.Quotations.ViewAll)) return query;
        var uid = _currentUser.UserId ?? Guid.Empty;
        return query.Where(x => x.OwnerUserId == uid);
    }
}
