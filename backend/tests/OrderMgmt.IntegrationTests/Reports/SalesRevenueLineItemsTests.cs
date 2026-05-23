using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.SalesRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Reports;

[Collection(nameof(PostgresCollection))]
public class SalesRevenueLineItemsTests : QuotationTestBase
{
    public SalesRevenueLineItemsTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task LineItems_ReturnsLinesWithCorrectFields()
    {
        var q = await CreateAndConfirmAsync();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == q.Id)).OwnerUserId;

        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options);
        var items = body!.Data!;

        items.Should().HaveCount(1);
        items[0].IsFirstLineOfQuotation.Should().BeTrue();
        items[0].QuotationCode.Should().Be(q.Code);
        items[0].ProductName.Should().NotBeNullOrEmpty();
        items[0].LineTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LineItems_MultipleLines_OnlyFirstMarked()
    {
        var req = BuildRequest(
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "Line A",
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 2,
                UnitPrice = 10_000,
            },
            new UpsertQuotationLineRequest
            {
                SortOrder = 1,
                ProductId = _productId,
                ProductName = "Line B",
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 3,
                UnitPrice = 20_000,
            });
        var q = await CreateAndConfirmAsync(req);

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == q.Id)).OwnerUserId;

        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var items = (await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        items.Should().HaveCount(2);
        items[0].IsFirstLineOfQuotation.Should().BeTrue();
        items[1].IsFirstLineOfQuotation.Should().BeFalse();
        items[0].QuotationCode.Should().Be(items[1].QuotationCode);
        items[0].Freight.Should().Be(items[1].Freight);
    }

    [Fact]
    public async Task LineItems_ExcludesCancelledQuotations()
    {
        var confirmed  = await CreateAndConfirmAsync();
        var toCancel   = await CreateAndConfirmAsync();

        var cancelResp = await _client.PostAsJsonAsync(
            $"/api/quotations/{toCancel.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Cancel });
        cancelResp.EnsureSuccessStatusCode();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == confirmed.Id)).OwnerUserId;

        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var items = (await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        items.Should().HaveCount(1);
        items.Should().AllSatisfy(i => i.QuotationId.Should().Be(confirmed.Id));
    }

    [Fact]
    public async Task LineItems_CostFields_NullWhenCallerLacksViewCostPermission()
    {
        // Arrange: create a confirmed quotation with UnitCost set on the line (admin owns it)
        var reqWithCost = BuildRequest(new UpsertQuotationLineRequest
        {
            SortOrder = 0,
            ProductId = _productId,
            ProductName = "Test EPS 1000x2000",
            UnitName = "Tấm",
            PricingMode = PricingMode.PerUnit,
            Quantity = 5,
            UnitPrice = 12_000,
            UnitCost = 7_000,   // explicitly set cost so cost fields are non-null
        });
        var q = await CreateAndConfirmAsync(reqWithCost);

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == q.Id)).OwnerUserId;

        // --- Act 1: Admin has quotations.view_cost → cost fields should be non-null ---
        await AuthenticateAsync("admin", "Admin@123");
        var adminResp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        adminResp.EnsureSuccessStatusCode();
        var adminItems = (await adminResp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        adminItems.Should().Contain(i => i.QuotationId == q.Id);
        var adminLine = adminItems.First(i => i.QuotationId == q.Id);
        adminLine.UnitCost.Should().NotBeNull("admin has quotations.view_cost");
        adminLine.LineCost.Should().NotBeNull("admin has quotations.view_cost");
        adminLine.LineProfit.Should().NotBeNull("admin has quotations.view_cost");

        // --- Act 2: Accountant has reports.revenue but NOT quotations.view_cost ---
        // The Accountant role is seeded with: customers.view, products.view,
        // quotations.view, quotations.view_all, quotations.accounting_confirm,
        // reports.revenue, reports.debt — but NOT quotations.view_cost.
        await CreateTestUserAsync("accountant-no-cost", "Acct@123", RoleCodes.Accountant);
        await AuthenticateAsync("accountant-no-cost", "Acct@123");

        var acctResp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        acctResp.EnsureSuccessStatusCode();
        var acctItems = (await acctResp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        acctItems.Should().Contain(i => i.QuotationId == q.Id);
        var acctLine = acctItems.First(i => i.QuotationId == q.Id);
        acctLine.UnitCost.Should().BeNull("accountant lacks quotations.view_cost");
        acctLine.LineCost.Should().BeNull("accountant lacks quotations.view_cost");
        acctLine.LineProfit.Should().BeNull("accountant lacks quotations.view_cost");

        // Restore admin auth for any subsequent operations
        await AuthenticateAsync("admin", "Admin@123");
    }

    private async Task<QuotationDto> CreateAndConfirmAsync(UpsertQuotationRequest? req = null)
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", req ?? BuildRequest());
        create.EnsureSuccessStatusCode();
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!.Id;
        await _client.PostAsJsonAsync($"/api/quotations/{id}/transition", new TransitionQuotationRequest { Action = QuotationAction.Send });
        var confirmResp = await _client.PostAsJsonAsync($"/api/quotations/{id}/transition", new TransitionQuotationRequest { Action = QuotationAction.Confirm });
        confirmResp.EnsureSuccessStatusCode();
        return (await confirmResp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!;
    }
}
