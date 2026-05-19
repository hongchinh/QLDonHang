using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Reports.VehicleRevenue.Interfaces;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Reports.VehicleRevenue.Services;

public class VehicleRevenueReportService : IVehicleRevenueReportService
{
    private const string DefaultVehicleNumber = "Xe khác";
    private readonly IAppDbContext _db;

    public VehicleRevenueReportService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<VehicleRevenueReportDto> GetAsync(VehicleRevenueReportRequest request, CancellationToken ct = default)
    {
        var from = request.From!.Value;
        var to = request.To!.Value;
        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusiveUtc = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var summaryRows = await ConfirmedQuotations(fromUtc, toExclusiveUtc)
            .Select(q => new
            {
                VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                    ? DefaultVehicleNumber
                    : q.TransportVehicleNumber.Trim(),
                q.Total,
                Net = q.Subtotal - q.Discount,
            })
            .GroupBy(x => x.VehicleNumber)
            .Select(g => new VehicleRevenueReportItem
            {
                VehicleNumber = g.Key,
                QuotationCount = g.Count(),
                TotalRevenueGross = g.Sum(x => x.Total),
                TotalRevenueNet = g.Sum(x => x.Net),
            })
            .OrderByDescending(x => x.TotalRevenueGross)
            .ThenBy(x => x.VehicleNumber)
            .ToListAsync(ct);

        var chartEndMonth = new DateOnly(to.Year, to.Month, 1);
        var chartStartMonth = chartEndMonth.AddMonths(-(request.Months - 1));
        var chartFromUtc = DateTime.SpecifyKind(chartStartMonth.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var chartToExclusiveUtc = DateTime.SpecifyKind(chartEndMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        var chartAggregates = await ConfirmedQuotations(chartFromUtc, chartToExclusiveUtc)
            .Select(q => new
            {
                VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                    ? DefaultVehicleNumber
                    : q.TransportVehicleNumber.Trim(),
                Year = q.ConfirmedAt!.Value.Year,
                Month = q.ConfirmedAt!.Value.Month,
                q.Total,
            })
            .GroupBy(x => new { x.VehicleNumber, x.Year, x.Month })
            .Select(g => new
            {
                g.Key.VehicleNumber,
                g.Key.Year,
                g.Key.Month,
                TotalRevenueGross = g.Sum(x => x.Total),
            })
            .ToListAsync(ct);

        var topVehicles = chartAggregates
            .GroupBy(x => x.VehicleNumber)
            .Select(g => new { VehicleNumber = g.Key, Total = g.Sum(x => x.TotalRevenueGross) })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.VehicleNumber)
            .Take(request.TopVehicles)
            .Select(x => x.VehicleNumber)
            .ToList();

        if (chartAggregates.Any(x => x.VehicleNumber == DefaultVehicleNumber)
            && !topVehicles.Contains(DefaultVehicleNumber))
        {
            topVehicles.Add(DefaultVehicleNumber);
        }

        var chartLookup = chartAggregates
            .Where(x => topVehicles.Contains(x.VehicleNumber))
            .ToDictionary(x => (x.VehicleNumber, x.Year, x.Month), x => x.TotalRevenueGross);

        var monthlySeries = Enumerable.Range(0, request.Months)
            .Select(offset =>
            {
                var month = chartStartMonth.AddMonths(offset);
                return new VehicleRevenueMonthlyPoint
                {
                    Month = $"{month:yyyy-MM}",
                    Values = topVehicles
                        .Select(vehicle => new VehicleRevenueMonthlyValue
                        {
                            VehicleNumber = vehicle,
                            TotalRevenueGross = chartLookup.GetValueOrDefault((vehicle, month.Year, month.Month)),
                        })
                        .ToList(),
                };
            })
            .ToList();

        return new VehicleRevenueReportDto
        {
            From = from,
            To = to,
            Months = request.Months,
            TopVehicles = request.TopVehicles,
            Items = summaryRows,
            ChartVehicles = topVehicles,
            MonthlySeries = monthlySeries,
            TotalQuotationCount = summaryRows.Sum(x => x.QuotationCount),
            GrandTotalGross = summaryRows.Sum(x => x.TotalRevenueGross),
            GrandTotalNet = summaryRows.Sum(x => x.TotalRevenueNet),
        };
    }

    private IQueryable<Domain.Entities.Sales.Quotation> ConfirmedQuotations(DateTime fromUtc, DateTime toExclusiveUtc) =>
        _db.Quotations.AsNoTracking()
            .Where(q => q.Status == QuotationStatus.Confirmed
                && q.CancelledAt == null
                && q.ConfirmedAt != null
                && q.ConfirmedAt >= fromUtc
                && q.ConfirmedAt < toExclusiveUtc);
}
