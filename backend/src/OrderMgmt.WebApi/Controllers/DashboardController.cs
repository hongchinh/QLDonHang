using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IQuotationDashboardService _legacy;
    private readonly IDashboardService _dashboard;

    public DashboardController(IQuotationDashboardService legacy, IDashboardService dashboard)
    {
        _legacy = legacy;
        _dashboard = dashboard;
    }

    [HttpGet("quotation-stats")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<QuotationStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QuotationStatsDto>>> Stats(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Success(await _legacy.GetStatsAsync(from, to, ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> Summary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? saleUserId,
        CancellationToken ct)
        => Success(await _dashboard.GetSummaryAsync(from, to, saleUserId, ct));

    [HttpGet("revenue-series")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<RevenueSeriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevenueSeriesDto>>> RevenueSeries(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] string granularity = "day",
        [FromQuery] Guid? saleUserId = null,
        CancellationToken ct = default)
        => Success(await _dashboard.GetRevenueSeriesAsync(from, to, granularity, saleUserId, ct));

    [HttpGet("top-customers")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopCustomerDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopCustomerDto>>>> TopCustomers(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int limit = 5,
        [FromQuery] Guid? saleUserId = null,
        CancellationToken ct = default)
        => Success(await _dashboard.GetTopCustomersAsync(from, to, limit, saleUserId, ct));

    [HttpGet("top-products")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>>> TopProducts(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int limit = 5,
        [FromQuery] Guid? saleUserId = null,
        CancellationToken ct = default)
        => Success(await _dashboard.GetTopProductsAsync(from, to, limit, saleUserId, ct));

    [HttpGet("recent-activity")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActivityItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityItemDto>>>> RecentActivity(
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        => Success(await _dashboard.GetRecentActivityAsync(limit, ct));

    [HttpGet("sales-leaderboard")]
    [HasPermission(Permissions.Quotations.ViewAll)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesLeaderboardItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesLeaderboardItemDto>>>> Leaderboard(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        => Success(await _dashboard.GetSalesLeaderboardAsync(from, to, limit, ct));
}
