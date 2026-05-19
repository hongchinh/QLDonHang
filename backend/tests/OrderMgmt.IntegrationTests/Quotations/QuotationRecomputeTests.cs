using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationRecomputeTests : QuotationTestBase
{
    public QuotationRecomputeTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task PerUnit_line_total_is_quantity_times_unit_price()
    {
        var dto = await CreateAsync(PricingMode.PerUnit, quantity: 5, unitPrice: 12_000);
        dto.Lines[0].LineTotal.Should().Be(60_000m);
    }

    [Fact]
    public async Task PerSquareMeter_line_total_uses_quantity_dimensions_and_unit_price()
    {
        var dto = await CreateAsync(PricingMode.PerSquareMeter, quantity: 2, unitPrice: 50_000, length: 2000, width: 1000);
        dto.Lines[0].LineTotal.Should().Be(200_000m);
    }

    [Fact]
    public async Task PerLinearMeter_line_total_uses_quantity_length_and_unit_price()
    {
        var dto = await CreateAsync(PricingMode.PerLinearMeter, quantity: 4, unitPrice: 25_000, length: 2500);
        dto.Lines[0].LineTotal.Should().Be(250_000m);
    }

    [Fact]
    public async Task PerCubicMeter_line_total_uses_quantity_dimensions_and_unit_price()
    {
        var dto = await CreateAsync(PricingMode.PerCubicMeter, quantity: 1, unitPrice: 1_000_000, length: 1000, width: 1000, thickness: 500);
        dto.Lines[0].LineTotal.Should().Be(500_000m);
    }

    [Fact]
    public async Task Totals_apply_subtotal_minus_discount_plus_freight_plus_tax()
    {
        var req = BuildRequest();
        req.TaxRate = 8;
        req.Discount = 10_000;
        req.Freight = 5_000;
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 1, UnitPrice = 100_000,
            },
        };

        var dto = await PostAsync(req);
        dto.Subtotal.Should().Be(100_000m);
        dto.TaxAmount.Should().Be(8_000m);
        dto.Total.Should().Be(103_000m);
    }

    [Fact]
    public async Task Line_cost_and_profit_are_computed_when_unit_cost_supplied()
    {
        var req = BuildRequest();
        req.TaxRate = 0;
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 5, UnitPrice = 12_000, UnitCost = 8_000,
            },
        };
        var dto = await PostAsync(req);
        dto.Lines[0].LineCost.Should().Be(40_000m);
        dto.Lines[0].LineProfit.Should().Be(20_000m);
    }

    [Fact]
    public async Task Tax_rounds_to_integer_vnd()
    {
        var req = BuildRequest();
        req.TaxRate = 8;
        req.Discount = 0;
        req.Freight = 0;
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 1, UnitPrice = 123,
            },
        };
        var dto = await PostAsync(req);
        dto.Subtotal.Should().Be(123m);
        dto.TaxAmount.Should().Be(10m);
    }

    [Fact]
    public async Task Gross_profit_excludes_freight_includes_discount()
    {
        var req = BuildRequest();
        req.TaxRate = 0;
        req.Discount = 10_000;
        req.Freight = 50_000;
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 5, UnitPrice = 12_000, UnitCost = 8_000,
            },
        };
        var dto = await PostAsync(req);
        dto.Subtotal.Should().Be(60_000m);
        dto.TotalCost.Should().Be(40_000m);
        dto.GrossProfit.Should().Be(10_000m); // 60k - 40k - 10k discount
    }

    private async Task<QuotationDto> CreateAsync(
        PricingMode mode,
        decimal quantity,
        decimal unitPrice,
        decimal? length = null,
        decimal? width = null,
        decimal? thickness = null)
    {
        var req = BuildRequest();
        req.TaxRate = 0;
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = mode,
                Length = length,
                Width = width,
                Thickness = thickness,
                Quantity = quantity, UnitPrice = unitPrice,
            },
        };
        return await PostAsync(req);
    }

    private async Task<QuotationDto> PostAsync(UpsertQuotationRequest req)
    {
        var resp = await _client.PostAsJsonAsync("/api/quotations", req);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return body!.Data!;
    }
}
