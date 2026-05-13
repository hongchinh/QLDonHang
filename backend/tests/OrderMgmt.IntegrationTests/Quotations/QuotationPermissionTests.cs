using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class QuotationPermissionTests : QuotationTestBase
{
    public QuotationPermissionTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Sales_can_create_and_update_but_not_delete()
    {
        await CreateTestUserAsync("sales1", "Sales@123", RoleCodes.Sales);
        await AuthenticateAsync("sales1", "Sales@123");

        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;

        var update = await _client.PutAsJsonAsync($"/api/quotations/{id}", BuildRequest());
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var delete = await _client.DeleteAsync($"/api/quotations/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accountant_can_view_but_cannot_create()
    {
        // Create with admin first
        var seeded = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        seeded.EnsureSuccessStatusCode();
        var body = await seeded.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;

        await CreateTestUserAsync("accountant1", "Acc@123", RoleCodes.Accountant);
        await AuthenticateAsync("accountant1", "Acc@123");

        var get = await _client.GetAsync($"/api/quotations/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Warehouse_cannot_view_quotations()
    {
        await CreateTestUserAsync("warehouse1", "Wh@123", RoleCodes.Warehouse);
        await AuthenticateAsync("warehouse1", "Wh@123");

        var list = await _client.GetAsync("/api/quotations?page=1&pageSize=20");
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_can_perform_full_lifecycle()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;

        var update = await _client.PutAsJsonAsync($"/api/quotations/{id}", BuildRequest());
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var delete = await _client.DeleteAsync($"/api/quotations/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
