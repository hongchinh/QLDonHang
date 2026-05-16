using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from, DateOnly? to, Guid? saleUserId, CancellationToken ct);
    Task<RevenueSeriesDto> GetRevenueSeriesAsync(DateOnly from, DateOnly to, string granularity, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DateOnly from, DateOnly to, int limit, Guid? saleUserId, CancellationToken ct);
    Task<IReadOnlyList<ActivityItemDto>> GetRecentActivityAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<SalesLeaderboardItemDto>> GetSalesLeaderboardAsync(DateOnly from, DateOnly to, int limit, CancellationToken ct);
}
