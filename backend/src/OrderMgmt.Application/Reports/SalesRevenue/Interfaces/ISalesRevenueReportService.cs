using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

public interface ISalesRevenueReportService
{
    Task<SalesRevenueReportDto> GetAsync(SalesRevenueReportRequest request, CancellationToken ct = default);
}
