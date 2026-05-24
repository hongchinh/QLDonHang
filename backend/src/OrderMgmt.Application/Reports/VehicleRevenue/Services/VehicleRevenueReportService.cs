using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Reports.Common;
using OrderMgmt.Application.Reports.VehicleRevenue.Interfaces;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Entities.Sales;
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
        var dateMode = await RevenueFilterHelper.GetDateModeAsync(_db, ct);
        var from = request.From!.Value;
        var to = request.To!.Value;

        var scope = _db.Quotations.AsNoTracking();

        var summaryRows = await RevenueFilterHelper.ApplyRevenueFilter(scope, dateMode, from, to)
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
        var chartTo = chartEndMonth.AddMonths(1).AddDays(-1);

        var chartFiltered = RevenueFilterHelper.ApplyRevenueFilter(scope, dateMode, chartStartMonth, chartTo);

        List<(string VehicleNumber, int Year, int Month, decimal Total)> chartAggregates;

        if (dateMode == RevenueDateField.AccountingConfirmedAt)
        {
            chartAggregates = (await chartFiltered
                .Select(q => new
                {
                    VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                        ? DefaultVehicleNumber : q.TransportVehicleNumber.Trim(),
                    Year = q.AccountingConfirmedAt!.Value.Year,
                    Month = q.AccountingConfirmedAt!.Value.Month,
                    q.Total,
                })
                .GroupBy(x => new { x.VehicleNumber, x.Year, x.Month })
                .Select(g => new { g.Key.VehicleNumber, g.Key.Year, g.Key.Month, TotalRevenueGross = g.Sum(x => x.Total) })
                .ToListAsync(ct))
                .Select(r => (r.VehicleNumber, r.Year, r.Month, r.TotalRevenueGross)).ToList();
        }
        else if (dateMode == RevenueDateField.ConfirmedAt)
        {
            chartAggregates = (await chartFiltered
                .Select(q => new
                {
                    VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                        ? DefaultVehicleNumber : q.TransportVehicleNumber.Trim(),
                    Year = q.ConfirmedAt!.Value.Year,
                    Month = q.ConfirmedAt!.Value.Month,
                    q.Total,
                })
                .GroupBy(x => new { x.VehicleNumber, x.Year, x.Month })
                .Select(g => new { g.Key.VehicleNumber, g.Key.Year, g.Key.Month, TotalRevenueGross = g.Sum(x => x.Total) })
                .ToListAsync(ct))
                .Select(r => (r.VehicleNumber, r.Year, r.Month, r.TotalRevenueGross)).ToList();
        }
        else // QuotationDate
        {
            chartAggregates = (await chartFiltered
                .Select(q => new
                {
                    VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                        ? DefaultVehicleNumber : q.TransportVehicleNumber.Trim(),
                    Year = q.QuotationDate.Year,
                    Month = q.QuotationDate.Month,
                    q.Total,
                })
                .GroupBy(x => new { x.VehicleNumber, x.Year, x.Month })
                .Select(g => new { g.Key.VehicleNumber, g.Key.Year, g.Key.Month, TotalRevenueGross = g.Sum(x => x.Total) })
                .ToListAsync(ct))
                .Select(r => (r.VehicleNumber, r.Year, r.Month, r.TotalRevenueGross)).ToList();
        }

        var topVehicles = chartAggregates
            .GroupBy(x => x.VehicleNumber)
            .Select(g => new { VehicleNumber = g.Key, Total = g.Sum(x => x.Total) })
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
            .ToDictionary(x => (x.VehicleNumber, x.Year, x.Month), x => x.Total);

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

}
