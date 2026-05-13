using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationSoftDeleteCascadeTests : QuotationTestBase
{
    public QuotationSoftDeleteCascadeTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Delete_cascades_soft_delete_to_lines()
    {
        var req = BuildRequest();
        req.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "X", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 1, UnitPrice = 100,
            },
            new UpsertQuotationLineRequest
            {
                SortOrder = 1,
                ProductId = _productId,
                ProductName = "Y", UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 2, UnitPrice = 200,
            },
        };
        var create = await _client.PostAsJsonAsync("/api/quotations", req);
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;

        var delete = await _client.DeleteAsync($"/api/quotations/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lines = await db.QuotationLines
            .IgnoreQueryFilters()
            .Where(l => l.QuotationId == id)
            .ToListAsync();

        lines.Should().HaveCount(2);
        lines.Should().OnlyContain(l => l.IsDeleted);
        lines.Should().OnlyContain(l => l.DeletedAt != null);
    }

    [Fact]
    public async Task Get_after_delete_returns_404()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;

        await _client.DeleteAsync($"/api/quotations/{id}");

        var get = await _client.GetAsync($"/api/quotations/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
