using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

public interface ISalesRevenueReportService
{
    Task<SalesRevenueReportDto> GetAsync(SalesRevenueReportRequest request, CancellationToken ct = default);

    Task<List<SalesRevenueLineItemDto>> GetLineItemsAsync(
        Guid saleUserId,
        SalesRevenueLineItemsRequest request,
        CancellationToken ct = default);
}
