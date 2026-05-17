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
public class QuotationOwnersEndpointTests : QuotationTestBase
{
    public QuotationOwnersEndpointTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Owners_returns_only_users_with_quotations_active_only()
    {
        // admin: 2 quotations.
        await CreateSimpleQuotationAsync(1, 100);
        await CreateSimpleQuotationAsync(1, 100);

        // sale_owners_a: 1 quotation; sale_owners_b: 0 quotations.
        await CreateTestUserAsync("sale_owners_a", "Sale@123", "SALES");
        await CreateTestUserAsync("sale_owners_b", "Sale@123", "SALES");
        await AuthenticateAsync("sale_owners_a", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);

        await AuthenticateAsync("admin", "Admin@123");

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=false", TestJson.Options);

        res!.Data.Should().HaveCount(2);
        res.Data.Should().Contain(o => o.FullName == "Test SALES");
        res.Data.Should().NotContain(o => o.IsDeleted);
    }

    [Fact]
    public async Task Owners_with_includeDeleted_returns_orphan_users_at_end()
    {
        await CreateTestUserAsync("sale_to_delete", "Sale@123", "SALES");
        await AuthenticateAsync("sale_to_delete", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);

        await AuthenticateAsync("admin", "Admin@123");
        await CreateSimpleQuotationAsync(1, 100);

        // Soft-delete the sale user.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FirstAsync(x => x.Username == "sale_to_delete");
            u.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=true", TestJson.Options);

        res!.Data.Should().HaveCount(2);
        res.Data!.Last().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Owners_with_includeDeleted_false_excludes_orphan()
    {
        await CreateTestUserAsync("sale_excluded", "Sale@123", "SALES");
        await AuthenticateAsync("sale_excluded", "Sale@123");
        await CreateSimpleQuotationAsync(1, 100);

        await AuthenticateAsync("admin", "Admin@123");
        await CreateSimpleQuotationAsync(1, 100);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FirstAsync(x => x.Username == "sale_excluded");
            u.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=false", TestJson.Options);

        res!.Data.Should().HaveCount(1);
        res.Data.Should().NotContain(o => o.IsDeleted);
    }

    [Fact]
    public async Task Owners_without_view_all_returns_403()
    {
        await CreateTestUserAsync("sale_no_perm", "Sale@123", "SALES");
        await AuthenticateAsync("sale_no_perm", "Sale@123");

        var res = await _client.GetAsync("/api/quotations/owners?includeDeleted=true");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owners_accountant_can_call_after_seed()
    {
        await CreateTestUserAsync("acc_owner", "Acc@123", "ACCOUNTANT");
        await CreateSimpleQuotationAsync(1, 100);

        await AuthenticateAsync("acc_owner", "Acc@123");

        var res = await _client.GetFromJsonAsync<ApiResponse<List<QuotationOwnerOptionDto>>>(
            "/api/quotations/owners?includeDeleted=true", TestJson.Options);

        res!.Data.Should().NotBeEmpty();
    }

    private async Task<Guid> CreateSimpleQuotationAsync(decimal quantity, decimal unitPrice)
    {
        var req = BuildRequest(new UpsertQuotationLineRequest
        {
            SortOrder = 0,
            ProductId = _productId,
            ProductName = "Test EPS 1000x2000",
            UnitName = "Tấm",
            PricingMode = PricingMode.PerUnit,
            Quantity = quantity,
            UnitPrice = unitPrice,
        });
        req.TaxRate = 0; req.Discount = 0; req.Freight = 0;

        var res = await _client.PostAsJsonAsync("/api/quotations", req);
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return created!.Data!.Id;
    }
}
