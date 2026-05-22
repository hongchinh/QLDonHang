using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests.Quotations;

[Collection(nameof(PostgresCollection))]
public class HandoverExportTests : QuotationTestBase
{
    public HandoverExportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task HandoverWithPrice_Excel_returns_xlsx()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await _client.GetAsync($"/api/quotations/{id}/handover-with-price/excel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandoverNoPrice_Excel_returns_xlsx()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await _client.GetAsync($"/api/quotations/{id}/handover-no-price/excel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandoverWithPrice_Pdf_returns_pdf_via_fake_converter()
    {
        var factory = new WebAppFactoryWithFakeHandoverPdfConverter(_pg.ConnectionString);
        await ((IAsyncLifetime)factory).InitializeAsync();
        var client = factory.CreateClient();
        await AuthenticateClientAsync(client);

        var create = await client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await client.GetAsync($"/api/quotations/{id}/handover-with-price/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        client.Dispose();
        await ((IAsyncLifetime)factory).DisposeAsync();
    }

    [Fact]
    public async Task HandoverNoPrice_Pdf_returns_pdf_via_fake_converter()
    {
        var factory = new WebAppFactoryWithFakeHandoverPdfConverter(_pg.ConnectionString);
        await ((IAsyncLifetime)factory).InitializeAsync();
        var client = factory.CreateClient();
        await AuthenticateClientAsync(client);

        var create = await client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await client.GetAsync($"/api/quotations/{id}/handover-no-price/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        client.Dispose();
        await ((IAsyncLifetime)factory).DisposeAsync();
    }

    private static async Task AuthenticateClientAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "Admin@123" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
    }
}

file sealed class WebAppFactoryWithFakeHandoverPdfConverter : WebAppFactory
{
    public WebAppFactoryWithFakeHandoverPdfConverter(string connectionString) : base(connectionString) { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IQuotationSpreadsheetPdfConverter));
            if (descriptor is not null) services.Remove(descriptor);
            services.AddScoped<IQuotationSpreadsheetPdfConverter, FakeHandoverPdfConverter>();
        });
    }
}

file sealed class FakeHandoverPdfConverter : IQuotationSpreadsheetPdfConverter
{
    private static readonly byte[] FakePdf = "%PDF-1.0 fake\n"u8.ToArray();
    public Task<byte[]> ConvertAsync(byte[] xlsxBytes, CancellationToken ct = default)
        => Task.FromResult(FakePdf);
}
