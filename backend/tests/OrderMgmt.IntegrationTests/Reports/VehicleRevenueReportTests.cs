using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Reports;

[Collection(nameof(PostgresCollection))]
public class VehicleRevenueReportTests : QuotationTestBase
{
    public VehicleRevenueReportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Report_AggregatesByVehicle_AndExcludesCancelled()
    {
        var q1 = await CreateConfirmedAsync("51C-12345");
        var q2 = await CreateConfirmedAsync("51C-12345");
        var q3 = await CreateConfirmedAsync("29H-67890");
        await TransitionAsync(q3.Id, QuotationAction.Cancel);

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].VehicleNumber.Should().Be("51C-12345");
        report.Items[0].QuotationCount.Should().Be(2);
        report.Items[0].TotalRevenueGross.Should().Be(q1.Total + q2.Total);
        report.TotalQuotationCount.Should().Be(2);
        report.GrandTotalGross.Should().Be(q1.Total + q2.Total);
    }

    [Fact]
    public async Task Report_GroupsBlankVehicleAsXeKhac()
    {
        await CreateConfirmedAsync("");

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].VehicleNumber.Should().Be("Xe khác");
        report.ChartVehicles.Should().Contain("Xe khác");
    }

    [Fact]
    public async Task Report_UsesConfirmedAt_NotQuotationDate()
    {
        var q = await CreateConfirmedAsync("51C-12345");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var entity = await db.Quotations.FirstAsync(x => x.Id == q.Id);
            entity.QuotationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
            await db.SaveChangesAsync();
        }

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].QuotationCount.Should().Be(1);
    }

    [Fact]
    public async Task Report_DefaultsToSixMonthlyPoints_AndAllowsCustomMonths()
    {
        await CreateConfirmedAsync("51C-12345");

        var defaultReport = await GetReportAsync();
        defaultReport.Months.Should().Be(6);
        defaultReport.MonthlySeries.Should().HaveCount(6);

        var customReport = await GetReportAsync(months: 3);
        customReport.Months.Should().Be(3);
        customReport.MonthlySeries.Should().HaveCount(3);
    }

    [Fact]
    public async Task Report_LimitsTopVehicles_ButIncludesXeKhac()
    {
        await CreateConfirmedAsync("A");
        await CreateConfirmedAsync("B");
        await CreateConfirmedAsync("");

        var report = await GetReportAsync(topVehicles: 1);

        report.ChartVehicles.Should().Contain("Xe khác");
        report.ChartVehicles.Should().HaveCountLessThanOrEqualTo(2);
        report.Items.Select(x => x.VehicleNumber).Should().Contain(new[] { "A", "B", "Xe khác" });
    }

    [Fact]
    public async Task Report_RequiresDates()
    {
        var resp = await _client.GetAsync("/api/reports/vehicle-revenue");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<VehicleRevenueReportDto> GetReportAsync(int? months = null, int? topVehicles = null)
    {
        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
        var url = $"/api/reports/vehicle-revenue?from={from}&to={to}";
        if (months.HasValue) url += $"&months={months.Value}";
        if (topVehicles.HasValue) url += $"&topVehicles={topVehicles.Value}";

        var resp = await _client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<VehicleRevenueReportDto>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<QuotationDto> CreateConfirmedAsync(string? vehicleNumber)
    {
        var req = BuildRequest();
        req.TransportVehicleNumber = vehicleNumber;

        var create = await _client.PostAsJsonAsync("/api/quotations", req);
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
