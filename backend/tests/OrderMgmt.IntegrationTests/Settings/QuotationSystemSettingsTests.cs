using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Settings;

[Collection(nameof(PostgresCollection))]
public class QuotationSystemSettingsTests : QuotationTestBase
{
    public QuotationSystemSettingsTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Get_returns_default_QuotationDate()
    {
        var resp = await _client.GetAsync("/api/settings/quotation");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationSystemSettingsDto>>(TestJson.Options);
        dto!.Data!.RevenueReportingDateField.Should().Be("QuotationDate");
    }

    [Fact]
    public async Task Get_returns_403_for_sales_user()
    {
        await CreateTestUserAsync("settings-sales1", "Sales@123", RoleCodes.Sales);
        await AuthenticateAsync("settings-sales1", "Sales@123");
        try
        {
            var resp = await _client.GetAsync("/api/settings/quotation");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await AuthenticateAsync("admin", "Admin@123");
        }
    }

    [Fact]
    public async Task Put_valid_value_persists()
    {
        try
        {
            var putResp = await _client.PutAsJsonAsync(
                "/api/settings/quotation",
                new { revenueReportingDateField = "AccountingConfirmedAt" });
            putResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResp = await _client.GetAsync("/api/settings/quotation");
            var dto = await getResp.Content.ReadFromJsonAsync<ApiResponse<QuotationSystemSettingsDto>>(TestJson.Options);
            dto!.Data!.RevenueReportingDateField.Should().Be("AccountingConfirmedAt");
        }
        finally
        {
            await _client.PutAsJsonAsync(
                "/api/settings/quotation",
                new { revenueReportingDateField = "QuotationDate" });
        }
    }

    [Fact]
    public async Task Put_invalid_value_returns_error()
    {
        var resp = await _client.PutAsJsonAsync(
            "/api/settings/quotation",
            new { revenueReportingDateField = "InvalidField" });
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }
}
