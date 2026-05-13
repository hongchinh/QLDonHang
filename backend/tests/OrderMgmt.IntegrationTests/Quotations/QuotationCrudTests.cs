using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationCrudTests : QuotationTestBase
{
    public QuotationCrudTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Create_then_get_returns_quotation_with_snapshotted_customer_and_lines()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        created!.Data!.Code.Should().StartWith("BG-");
        created.Data.CustomerName.Should().Be("Test Customer");
        created.Data.Status.Should().Be(QuotationStatus.Draft);

        var get = await _client.GetFromJsonAsync<ApiResponse<QuotationDto>>(
            $"/api/quotations/{created.Data.Id}", TestJson.Options);
        get!.Data!.Lines.Should().HaveCount(1);
        get.Data.Lines[0].ProductName.Should().Be("Test EPS 1000x2000");
        get.Data.Lines[0].LineTotal.Should().Be(60_000m);
    }

    [Fact]
    public async Task Update_changes_line_price_and_recomputes_total()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var update = BuildRequest();
        update.Lines = new[]
        {
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "Test EPS 1000x2000",
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 10,
                UnitPrice = 20_000,
            },
        };

        var put = await _client.PutAsJsonAsync($"/api/quotations/{id}", update);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetFromJsonAsync<ApiResponse<QuotationDto>>(
            $"/api/quotations/{id}", TestJson.Options);
        get!.Data!.Lines[0].LineTotal.Should().Be(200_000m);
        get.Data.Subtotal.Should().Be(200_000m);
    }

    [Fact]
    public async Task Delete_then_get_returns_404()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var delete = await _client.DeleteAsync($"/api/quotations/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetAsync($"/api/quotations/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Code_increments_per_day()
    {
        var c1 = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var b1 = await c1.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var c2 = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var b2 = await c2.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);

        b1!.Data!.Code.Should().NotBe(b2!.Data!.Code);
        b1.Data.Code.Should().StartWith("BG-");
        b2.Data.Code.Should().StartWith("BG-");
    }

    [Fact]
    public async Task Create_WithCustomerNameOverride_PersistsOverride()
    {
        var request = BuildRequest();
        request.CustomerName = "ABC Display";

        var create = await _client.PostAsJsonAsync("/api/quotations", request);
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        created!.Data!.CustomerName.Should().Be("ABC Display");

        var get = await _client.GetFromJsonAsync<ApiResponse<QuotationDto>>(
            $"/api/quotations/{created.Data.Id}", TestJson.Options);
        get!.Data!.CustomerName.Should().Be("ABC Display");
    }

    [Fact]
    public async Task Create_WithoutCustomerName_FallsBackToMaster()
    {
        var request = BuildRequest();
        request.CustomerName = null;

        var create = await _client.PostAsJsonAsync("/api/quotations", request);
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        created!.Data!.CustomerName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task Update_ChangesCustomerNameOverride()
    {
        var initial = BuildRequest();
        initial.CustomerName = "Initial Override";
        var create = await _client.PostAsJsonAsync("/api/quotations", initial);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var update = BuildRequest();
        update.CustomerName = "Updated Override";
        var put = await _client.PutAsJsonAsync($"/api/quotations/{id}", update);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetFromJsonAsync<ApiResponse<QuotationDto>>(
            $"/api/quotations/{id}", TestJson.Options);
        get!.Data!.CustomerName.Should().Be("Updated Override");
    }

    [Fact]
    public async Task Update_WithEmptyCustomerName_FallsBackToMaster()
    {
        var initial = BuildRequest();
        initial.CustomerName = "Initial Override";
        var create = await _client.PostAsJsonAsync("/api/quotations", initial);
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var update = BuildRequest();
        update.CustomerName = "   ";
        var put = await _client.PutAsJsonAsync($"/api/quotations/{id}", update);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetFromJsonAsync<ApiResponse<QuotationDto>>(
            $"/api/quotations/{id}", TestJson.Options);
        get!.Data!.CustomerName.Should().Be("Test Customer");
    }
}
