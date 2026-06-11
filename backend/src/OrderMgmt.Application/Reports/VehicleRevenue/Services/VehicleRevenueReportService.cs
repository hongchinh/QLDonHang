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
        var rawLines = await RevenueFilterHelper.ApplyRevenueFilter(scope, dateMode, from, to)
            .SelectMany(q => q.Lines
                .Where(l => l.ProductCode != null && l.ProductCode.ToLower() == CuocCode)
                .Select(l => new
                {
                    VehicleNumber = q.TransportVehicleNumber == null || q.TransportVehicleNumber.Trim() == string.Empty
                        ? DefaultVehicleNumber
                        : q.TransportVehicleNumber.Trim(),
                    l.LineTotal,
                    QuotationId = q.Id,
                }))
            .ToListAsync(ct);

        // Aggregation in-memory: Distinct().Count() theo từng nhóm dương/âm không translate được trong EF Core GroupBy
        var summaryRows = rawLines
            .GroupBy(x => x.VehicleNumber)
            .Select(g => new VehicleRevenueReportItem
            {
                VehicleNumber = g.Key,
                CompanyQuotationCount = g.Where(x => x.LineTotal > 0).Select(x => x.QuotationId).Distinct().Count(),
                ExternalQuotationCount = g.Where(x => x.LineTotal < 0).Select(x => x.QuotationId).Distinct().Count(),
                CompanyVehicleRevenue = g.Where(x => x.LineTotal > 0).Sum(x => x.LineTotal),
                ExternalVehicleRevenue = g.Where(x => x.LineTotal < 0).Sum(x => x.LineTotal),
            })
            .OrderByDescending(x => x.CompanyVehicleRevenue)
            .ThenBy(x => x.VehicleNumber)
            .ToList();

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
