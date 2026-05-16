using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Dashboard;

[Collection(nameof(PostgresCollection))]
public class DashboardEndpointsTests : DashboardTestBase
{
    public DashboardEndpointsTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task RevenueSeries_Day_FillsZeroGaps()
    {
        await CreateTestUserAsync("dash_rs", "Sales@123", RoleCodes.Sales);
        var sale = await GetUserIdAsync("dash_rs");
        var d1 = new DateOnly(2026, 5, 1);
        var d5 = new DateOnly(2026, 5, 5);
        var d7 = new DateOnly(2026, 5, 7);

        await SeedQuotationAsync("BG-RS-1", sale, QuotationStatus.Confirmed, d1, 100m, confirmedAt: new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc));
        await SeedQuotationAsync("BG-RS-2", sale, QuotationStatus.Confirmed, d5, 200m, confirmedAt: new DateTime(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc));
        await SeedQuotationAsync("BG-RS-3", sale, QuotationStatus.Confirmed, d7, 300m, confirmedAt: new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc));

        var resp = await _client.GetAsync("/api/dashboard/revenue-series?from=2026-05-01&to=2026-05-07&granularity=day");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<RevenueSeriesDto>>(TestJson.Options);
        body!.Data!.Points.Should().HaveCount(7);
        body.Data.Points.Sum(p => p.Total).Should().Be(600m);
        body.Data.Points.First(p => p.Date == new DateOnly(2026, 5, 2)).Total.Should().Be(0m);
    }

    [Fact]
    public async Task TopCustomers_OrdersByRevenueDesc_LimitsCorrectly()
    {
        await CreateTestUserAsync("dash_tc", "Sales@123", RoleCodes.Sales);
        var sale = await GetUserIdAsync("dash_tc");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Multiple quotations per customer with varying revenue. Customer A wins.
        await SeedQuotationAsync("BG-TC-A1", sale, QuotationStatus.Confirmed, today, 1000m,
            confirmedAt: now, customerName: "Khách A");
        await SeedQuotationAsync("BG-TC-A2", sale, QuotationStatus.Confirmed, today, 1000m,
            confirmedAt: now, customerName: "Khách A");
        await SeedQuotationAsync("BG-TC-B", sale, QuotationStatus.Confirmed, today, 500m,
            confirmedAt: now, customerName: "Khách B");
        await SeedQuotationAsync("BG-TC-C", sale, QuotationStatus.Confirmed, today, 100m,
            confirmedAt: now, customerName: "Khách C");

        var resp = await _client.GetAsync($"/api/dashboard/top-customers?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}&limit=2");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<TopCustomerDto>>>(TestJson.Options);
        body!.Data!.Should().HaveCount(2);
        body.Data![0].Revenue.Should().BeGreaterThanOrEqualTo(body.Data![1].Revenue);
    }

    [Fact]
    public async Task RevenueRule_ExcludesCancelled()
    {
        await CreateTestUserAsync("dash_rr", "Sales@123", RoleCodes.Sales);
        var sale = await GetUserIdAsync("dash_rr");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        await SeedQuotationAsync("BG-RR-1", sale, QuotationStatus.Confirmed, today, 1000m, confirmedAt: now);
        // Cancelled-after-confirm row must NOT contribute to revenue.
        await SeedQuotationAsync("BG-RR-2", sale, QuotationStatus.Confirmed, today, 500m,
            confirmedAt: now, cancelledAt: now);

        var resp = await _client.GetAsync($"/api/dashboard/summary?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>(TestJson.Options);
        body!.Data!.RangeRevenue.Value.Should().Be(1000m);
    }

    [Fact]
    public async Task Funnel_OnlyCountsWithinRange()
    {
        await CreateTestUserAsync("dash_f", "Sales@123", RoleCodes.Sales);
        var sale = await GetUserIdAsync("dash_f");
        var inside = new DateOnly(2026, 5, 5);
        var outside = new DateOnly(2026, 4, 30);

        await SeedQuotationAsync("BG-F-IN", sale, QuotationStatus.Confirmed, inside, 100m,
            confirmedAt: new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc));
        await SeedQuotationAsync("BG-F-OUT", sale, QuotationStatus.Confirmed, outside, 100m,
            confirmedAt: new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc));

        var resp = await _client.GetAsync("/api/dashboard/summary?from=2026-05-01&to=2026-05-15");
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>(TestJson.Options);
        body!.Data!.Funnel.Confirmed.Should().Be(1);
    }

    [Fact]
    public async Task RecentActivity_EmptyDb_Returns200WithEmptyList()
    {
        var resp = await _client.GetAsync("/api/dashboard/recent-activity?limit=8");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ActivityItemDto>>>(TestJson.Options);
        body!.Data.Should().NotBeNull();
        body.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task RecentActivity_MergesAllSources_OrdersByTimeDesc_RespectsLimit()
    {
        await CreateTestUserAsync("dash_ra", "Sales@123", RoleCodes.Sales);
        var sale = await GetUserIdAsync("dash_ra");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Created (no terminal event) — only contributes a "created" entry.
        await SeedQuotationAsync("BG-RA-C", sale, QuotationStatus.Draft, today, 100m);
        // Confirmed — contributes both "created" and "confirmed" entries.
        await SeedQuotationAsync("BG-RA-F", sale, QuotationStatus.Confirmed, today, 200m,
            confirmedAt: DateTime.UtcNow.AddMinutes(-5));
        // Cancelled (newest event) — contributes "created" and "cancelled".
        await SeedQuotationAsync("BG-RA-X", sale, QuotationStatus.Cancelled, today, 300m,
            cancelledAt: DateTime.UtcNow);

        var resp = await _client.GetAsync("/api/dashboard/recent-activity?limit=8");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<ActivityItemDto>>>(TestJson.Options);
        var items = body!.Data!;

        items.Should().NotBeEmpty();
        items.Select(i => i.Type).Should().Contain(new[] { "created", "confirmed", "cancelled" });
        items.Should().BeInDescendingOrder(i => i.At);
        items.Count.Should().BeLessThanOrEqualTo(8);

        var cancelledRow = items.FirstOrDefault(i => i.Type == "cancelled");
        cancelledRow.Should().NotBeNull();
        cancelledRow!.Code.Should().Be("BG-RA-X");
        cancelledRow.Amount.Should().Be(300m);
    }
}
