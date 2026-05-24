using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Entities.Sales;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Reports.Common;

public static class RevenueFilterHelper
{
    public static async Task<string> GetDateModeAsync(IAppDbContext db, CancellationToken ct)
    {
        var mode = await db.QuotationSystemSettings
            .AsNoTracking()
            .Where(s => s.Id == 1)
            .Select(s => s.RevenueReportingDateField)
            .FirstOrDefaultAsync(ct);
        return mode ?? RevenueDateField.QuotationDate;
    }

    public static IQueryable<Quotation> ApplyRevenueFilter(
        IQueryable<Quotation> q, string dateMode, DateOnly from, DateOnly to)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDt = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return dateMode switch
        {
            RevenueDateField.AccountingConfirmedAt => q.Where(x =>
                x.Status == QuotationStatus.AccountingConfirmed
                && x.CancelledAt == null
                && x.AccountingConfirmedAt >= fromDt
                && x.AccountingConfirmedAt < toDt),
            RevenueDateField.ConfirmedAt => q.Where(x =>
                (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed)
                && x.CancelledAt == null
                && x.ConfirmedAt >= fromDt
                && x.ConfirmedAt < toDt),
            _ => q.Where(x =>
                (x.Status == QuotationStatus.Confirmed || x.Status == QuotationStatus.AccountingConfirmed)
                && x.CancelledAt == null
                && x.QuotationDate >= from
                && x.QuotationDate <= to),
        };
    }
}
