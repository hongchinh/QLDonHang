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
public class SalesRevenueReportTests : QuotationTestBase
{
    public SalesRevenueReportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Report_AggregatesByOwner_AndExcludesCancelled()
    {
        var q1 = await CreateConfirmedAsync();
        var q2 = await CreateConfirmedAsync();
        var q3 = await CreateConfirmedAsync();
        await TransitionAsync(q3.Id, QuotationAction.Cancel);

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        var resp = await _client.GetAsync($"/api/reports/sales-revenue?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<SalesRevenueReportDto>>(TestJson.Options);
        var report = body!.Data!;

        report.Items.Should().HaveCount(1);
        report.Items[0].QuotationCount.Should().Be(2);
        report.Items[0].TotalRevenueGross.Should().Be(q1.Total + q2.Total);
        report.TotalQuotationCount.Should().Be(2);
        report.GrandTotalGross.Should().Be(q1.Total + q2.Total);
    }

    [Fact]
    public async Task Report_FiltersByConfirmedAt_NotQuotationDate()
    {
        var q = await CreateConfirmedAsync();

        // Backdate quotation_date to a year ago in DB; confirmed_at remains "now".
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Quotations.FirstAsync(x => x.Id == q.Id);
            entity.QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
            await db.SaveChangesAsync();
        }

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        var resp = await _client.GetAsync($"/api/reports/sales-revenue?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<SalesRevenueReportDto>>(TestJson.Options);
        var report = body!.Data!;

        report.Items.Should().HaveCount(1);
        report.Items[0].QuotationCount.Should().Be(1);
    }

    [Fact]
    public async Task Report_FiltersBySaleUserId()
    {
        await CreateConfirmedAsync(); // owned by admin (initial auth)

        await CreateTestUserAsync("sales-rep", "Sales@123", Domain.Constants.RoleCodes.Sales);
        await AuthenticateAsync("sales-rep", "Sales@123");
        var salesQ = await CreateConfirmedAsync();

        // Re-authenticate as admin so the report endpoint allows access (reports.revenue).
        await AuthenticateAsync("admin", "Admin@123");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var salesUserId = await db.Users.Where(u => u.Username == "sales-rep").Select(u => u.Id).FirstAsync();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        var resp = await _client.GetAsync($"/api/reports/sales-revenue?from={from}&to={to}&saleUserId={salesUserId}");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<SalesRevenueReportDto>>(TestJson.Options);
        var report = body!.Data!;

        report.Items.Should().HaveCount(1);
        report.Items[0].SaleUserId.Should().Be(salesUserId);
        report.Items[0].QuotationCount.Should().Be(1);
        report.Items[0].TotalRevenueGross.Should().Be(salesQ.Total);
    }

    private async Task<QuotationDto> CreateConfirmedAsync()
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", BuildRequest());
        create.EnsureSuccessStatusCode();
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;
        await TransitionAsync(id, QuotationAction.Send);
        return await TransitionAsync(id, QuotationAction.Confirm);
    }

    private async Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action)
    {
        var resp = await _client.PostAsJsonAsync(
            $"/api/quotations/{id}/transition",
            new TransitionQuotationRequest { Action = action });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        return body!.Data!;
    }
}
