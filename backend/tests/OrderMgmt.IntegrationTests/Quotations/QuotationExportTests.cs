using System.Net;
using System.Net.Http.Headers;
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
public class QuotationExportTests : QuotationTestBase
{
    public QuotationExportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Excel_returns_xlsx_with_correct_content_type()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await _client.GetAsync($"/api/quotations/{id}/excel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Pdf_returns_pdf_bytes_via_fake_converter()
    {
        // Use a factory override that replaces the LibreOffice converter with a fake,
        // so this test runs without LibreOffice installed.
        var factory = new WebAppFactoryWithFakePdfConverter(_pg.ConnectionString);
        await ((IAsyncLifetime)factory).InitializeAsync();
        var client = factory.CreateClient();
        await AuthenticateClientAsync(client);

        var create = await client.PostAsJsonAsync("/api/quotations", BuildRequest());
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = created!.Data!.Id;

        var response = await client.GetAsync($"/api/quotations/{id}/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();

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
            new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
    }
}

/// <summary>
/// WebAppFactory variant that replaces IQuotationSpreadsheetPdfConverter with a stub
/// returning a minimal PDF-like byte array, so PDF export tests run without LibreOffice.
/// </summary>
file sealed class WebAppFactoryWithFakePdfConverter : WebAppFactory
{
    public WebAppFactoryWithFakePdfConverter(string connectionString) : base(connectionString) { }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            // Replace real converter with a no-op stub.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IQuotationSpreadsheetPdfConverter));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddScoped<IQuotationSpreadsheetPdfConverter, FakePdfConverter>();
        });
    }
}

file sealed class FakePdfConverter : IQuotationSpreadsheetPdfConverter
{
    // Minimal valid PDF header so the response has non-empty bytes.
    private static readonly byte[] FakePdf = "%PDF-1.0 fake\n"u8.ToArray();

    public Task<byte[]> ConvertAsync(byte[] xlsxBytes, CancellationToken ct = default)
        => Task.FromResult(FakePdf);
}
