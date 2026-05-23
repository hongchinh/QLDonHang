using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.SalesRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
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
