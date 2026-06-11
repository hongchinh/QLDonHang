# Phase 01 — Backend DTO + Validator + Integration Tests rewrite

**Status:** [ ] pending
**Complexity:** M

## Objective

Cập nhật DTOs, bỏ rule validator `TopVehicles`, và rewrite integration tests để phản ánh contract mới. Sau phase này:
- Code compile được
- Integration tests compile và chạy được (sẽ FAIL vì service chưa thay đổi)
- Rõ ràng expectation cho Phase 02

## Files

- `backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Models/VehicleRevenueReportDtos.cs`
- `backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Validators/VehicleRevenueReportRequestValidator.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Reports/VehicleRevenueReportTests.cs`

## Tasks

### Task 1 — Rewrite `VehicleRevenueReportDtos.cs`

Thay toàn bộ nội dung file thành:

```csharp
namespace OrderMgmt.Application.Reports.VehicleRevenue.Models;

public class VehicleRevenueReportRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Months { get; set; } = 6;
    // TopVehicles đã bỏ — không còn giới hạn N xe trên chart
}

public class VehicleRevenueReportItem
{
    public string VehicleNumber { get; set; } = default!;
    public decimal CompanyVehicleRevenue { get; set; }   // tổng LineTotal > 0 của dòng "cuoc"
    public decimal ExternalVehicleRevenue { get; set; }  // tổng LineTotal < 0 của dòng "cuoc" (âm)
}

public class VehicleRevenueMonthlyPoint
{
    public string Month { get; set; } = default!;        // "yyyy-MM"
    public decimal CompanyTotal { get; set; }
    public decimal ExternalTotal { get; set; }           // âm
}

public class VehicleRevenueReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int Months { get; set; }
    public List<VehicleRevenueReportItem> Items { get; set; } = new();
    public List<VehicleRevenueMonthlyPoint> MonthlySeries { get; set; } = new();
    public decimal GrandTotalCompany { get; set; }
    public decimal GrandTotalExternal { get; set; }      // âm
}
```

**Lưu ý thay đổi:**
- `VehicleRevenueReportRequest`: xóa `TopVehicles`
- `VehicleRevenueReportItem`: xóa `QuotationCount`, `TotalRevenueGross`, `TotalRevenueNet`; thêm `CompanyVehicleRevenue`, `ExternalVehicleRevenue`
- `VehicleRevenueMonthlyPoint`: xóa `Values` (list per-vehicle); thêm `CompanyTotal`, `ExternalTotal`
- `VehicleRevenueReportDto`: xóa `TopVehicles`, `ChartVehicles`, `TotalQuotationCount`, `GrandTotalGross`, `GrandTotalNet`; thêm `GrandTotalCompany`, `GrandTotalExternal`
- Xóa hoàn toàn class `VehicleRevenueMonthlyValue` (không còn dùng)

### Task 2 — Cập nhật Validator

Trong `VehicleRevenueReportRequestValidator.cs`, xóa **chỉ dòng này**:
```csharp
RuleFor(x => x.TopVehicles).InclusiveBetween(1, 10);
```
Giữ nguyên tất cả các rule khác.

### Task 3 — Stub service để Application layer compile

`VehicleRevenueReportService.cs` vẫn còn reference 6 property đã xóa (`TopVehicles`, `QuotationCount`, `TotalRevenueGross`, `TotalRevenueNet`, `ChartVehicles`, `VehicleRevenueMonthlyValue`) — build **chắc chắn FAIL** nếu không stub.

**Bắt buộc**: comment out toàn bộ body của `GetAsync` trong `VehicleRevenueReportService.cs` và thay bằng:
```csharp
public async Task<VehicleRevenueReportDto> GetAsync(VehicleRevenueReportRequest request, CancellationToken ct = default)
{
    // TODO Phase 02: rewrite
    await Task.CompletedTask;
    return new VehicleRevenueReportDto();
}
```

Sau đó build:
```bash
dotnet build backend/src/OrderMgmt.Application
```
Expected: BUILD SUCCEEDED.

### Task 4 — Rewrite Integration Tests

Thay toàn bộ nội dung `VehicleRevenueReportTests.cs` thành:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Reports;

[Collection(nameof(PostgresCollection))]
public class VehicleRevenueReportTests : QuotationTestBase
{
    public VehicleRevenueReportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Report_SumsPositiveCuocLines_AsCompanyRevenue()
    {
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 10_000);
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 5_000);

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].VehicleNumber.Should().Be("51C-123");
        report.Items[0].CompanyVehicleRevenue.Should().Be(15_000);
        report.Items[0].ExternalVehicleRevenue.Should().Be(0);
        report.GrandTotalCompany.Should().Be(15_000);
        report.GrandTotalExternal.Should().Be(0);
    }

    [Fact]
    public async Task Report_SumsNegativeCuocLines_AsExternalRevenue()
    {
        await CreateConfirmedWithCuocAsync("29H-456", unitPrice: -8_000);

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].VehicleNumber.Should().Be("29H-456");
        report.Items[0].CompanyVehicleRevenue.Should().Be(0);
        report.Items[0].ExternalVehicleRevenue.Should().Be(-8_000);
        report.GrandTotalExternal.Should().Be(-8_000);
    }

    [Fact]
    public async Task Report_VehicleWithBothTypes_HasBothValues()
    {
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 10_000);
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: -3_000);

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].CompanyVehicleRevenue.Should().Be(10_000);
        report.Items[0].ExternalVehicleRevenue.Should().Be(-3_000);
    }

    [Fact]
    public async Task Report_ExcludesCancelledQuotations()
    {
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 10_000);
        var cancelled = await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 5_000);
        await TransitionAsync(cancelled.Id, QuotationAction.Cancel);

        var report = await GetReportAsync();

        report.Items[0].CompanyVehicleRevenue.Should().Be(10_000);
    }

    [Fact]
    public async Task Report_IgnoresLinesWithoutCuocCode()
    {
        // Quotation với lines mặc định (không có ProductCode = "cuoc")
        await CreateConfirmedAsync("51C-123");

        var report = await GetReportAsync();

        report.Items.Should().BeEmpty();
        report.GrandTotalCompany.Should().Be(0);
    }

    [Fact]
    public async Task Report_GroupsBlankVehicleAsXeKhac()
    {
        await CreateConfirmedWithCuocAsync("", unitPrice: 7_000);

        var report = await GetReportAsync();

        report.Items.Should().ContainSingle();
        report.Items[0].VehicleNumber.Should().Be("Xe khác");
    }

    [Fact]
    public async Task Report_DefaultsToSixMonthlyPoints_AndAllowsCustomMonths()
    {
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 10_000);

        var defaultReport = await GetReportAsync();
        defaultReport.Months.Should().Be(6);
        defaultReport.MonthlySeries.Should().HaveCount(6);

        var customReport = await GetReportAsync(months: 3);
        customReport.Months.Should().Be(3);
        customReport.MonthlySeries.Should().HaveCount(3);
    }

    [Fact]
    public async Task Report_MonthlySeries_HasCompanyAndExternalTotals()
    {
        await CreateConfirmedWithCuocAsync("51C-123", unitPrice: 10_000);
        await CreateConfirmedWithCuocAsync("29H-456", unitPrice: -4_000);

        var report = await GetReportAsync();

        var thisMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var point = report.MonthlySeries.First(p => p.Month == thisMonth);
        point.CompanyTotal.Should().Be(10_000);
        point.ExternalTotal.Should().Be(-4_000);
    }

    [Fact]
    public async Task Report_RequiresDates()
    {
        var resp = await _client.GetAsync("/api/reports/vehicle-revenue");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<VehicleRevenueReportDto> GetReportAsync(int? months = null)
    {
        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
        var url = $"/api/reports/vehicle-revenue?from={from}&to={to}";
        if (months.HasValue) url += $"&months={months.Value}";

        var resp = await _client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<VehicleRevenueReportDto>>(TestJson.Options);
        return body!.Data!;
    }

    private async Task<QuotationDto> CreateConfirmedWithCuocAsync(string? vehicleNumber, decimal unitPrice)
    {
        var req = BuildRequest(new UpsertQuotationLineRequest
        {
            SortOrder = 0,
            ProductCode = "cuoc",
            ProductName = "Cước vận chuyển",
            UnitName = "Chuyến",
            PricingMode = PricingMode.PerUnit,
            Quantity = 1,
            UnitPrice = unitPrice,
        });
        req.TransportVehicleNumber = vehicleNumber;

        var create = await _client.PostAsJsonAsync("/api/quotations", req);
        create.EnsureSuccessStatusCode();
        var body = await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options);
        var id = body!.Data!.Id;
        await TransitionAsync(id, QuotationAction.Send);
        return await TransitionAsync(id, QuotationAction.Confirm);
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
```

### Task 5 — Verify tests compile (not pass)

```bash
dotnet build backend/tests/OrderMgmt.IntegrationTests
```
Expected: BUILD SUCCEEDED. Tests sẽ FAIL khi chạy vì service chưa được cập nhật — đó là trạng thái mong muốn của phase này.

### Task 6 — Commit

```bash
git add backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Models/VehicleRevenueReportDtos.cs
git add backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Validators/VehicleRevenueReportRequestValidator.cs
git add backend/tests/OrderMgmt.IntegrationTests/Reports/VehicleRevenueReportTests.cs
git commit -m "refactor(vehicle-revenue): update DTOs and tests for cuoc-based split"
```

## Verification

```bash
dotnet build backend/src/OrderMgmt.Application
dotnet build backend/tests/OrderMgmt.IntegrationTests
```

## Exit Criteria

- `VehicleRevenueReportDtos.cs` không còn `TopVehicles`, `ChartVehicles`, `QuotationCount`, `TotalRevenueGross`, `TotalRevenueNet`, `VehicleRevenueMonthlyValue`
- Validator không còn rule `TopVehicles`
- Test file compile sạch với DTO mới
- Service có thể vẫn fail tạm thời — chấp nhận được đến Phase 02
