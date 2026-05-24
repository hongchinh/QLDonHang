# Phase 01 — Backend API

**Status:** [ ] pending
**Complexity:** M

## Objective

Thêm endpoint `GET /reports/sales-revenue/{saleUserId}/lines` trả về danh sách `SalesRevenueLineItemDto` — một dòng mỗi `QuotationLine` — cho một sale cụ thể trong khoảng ngày lọc. Trường cost/profit bị null khi caller thiếu `quotations.view_cost`.

## Files

- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Services/SalesRevenueReportService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Reports/SalesRevenueLineItemsTests.cs` (mới)

## Tasks

### Task 1 — Viết integration test thất bại đầu tiên

1. **Tạo file mới** `backend/tests/OrderMgmt.IntegrationTests/Reports/SalesRevenueLineItemsTests.cs`.

2. **Nội dung file:**

```csharp
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
public class SalesRevenueLineItemsTests : QuotationTestBase
{
    public SalesRevenueLineItemsTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task LineItems_ReturnsLinesWithCorrectFields()
    {
        // Arrange: one confirmed quotation (default BuildRequest has 1 line)
        var q = await CreateAndConfirmAsync();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == q.Id)).OwnerUserId;

        // Act
        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options);
        var items = body!.Data!;

        // Assert
        items.Should().HaveCount(1);
        items[0].IsFirstLineOfQuotation.Should().BeTrue();
        items[0].QuotationCode.Should().Be(q.Code);
        items[0].ProductName.Should().NotBeNullOrEmpty();
        items[0].LineTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LineItems_MultipleLines_OnlyFirstMarked()
    {
        // Arrange: quotation with 2 lines
        var req = BuildRequest(
            new UpsertQuotationLineRequest
            {
                SortOrder = 0,
                ProductId = _productId,
                ProductName = "Line A",
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 2,
                UnitPrice = 10_000,
            },
            new UpsertQuotationLineRequest
            {
                SortOrder = 1,
                ProductId = _productId,
                ProductName = "Line B",
                UnitName = "Tấm",
                PricingMode = PricingMode.PerUnit,
                Quantity = 3,
                UnitPrice = 20_000,
            });
        var q = await CreateAndConfirmAsync(req);

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == q.Id)).OwnerUserId;

        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var items = (await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        items.Should().HaveCount(2);
        items[0].IsFirstLineOfQuotation.Should().BeTrue();
        items[1].IsFirstLineOfQuotation.Should().BeFalse();
        items[0].QuotationCode.Should().Be(items[1].QuotationCode);
        items[0].Freight.Should().Be(items[1].Freight);
    }

    [Fact]
    public async Task LineItems_ExcludesCancelledQuotations()
    {
        var confirmed  = await CreateAndConfirmAsync();
        var toCancel   = await CreateAndConfirmAsync();

        var cancelResp = await _client.PostAsJsonAsync(
            $"/api/quotations/{toCancel.Id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Cancel });
        cancelResp.EnsureSuccessStatusCode();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saleUserId = (await db.Quotations.FirstAsync(x => x.Id == confirmed.Id)).OwnerUserId;

        var resp = await _client.GetAsync(
            $"/api/reports/sales-revenue/{saleUserId}/lines?from={from}&to={to}");
        resp.EnsureSuccessStatusCode();
        var items = (await resp.Content.ReadFromJsonAsync<ApiResponse<List<SalesRevenueLineItemDto>>>(TestJson.Options))!.Data!;

        items.Should().HaveCount(1);
        items.Should().AllSatisfy(i => i.QuotationId.Should().Be(confirmed.Id));
    }

    // --- helpers ---

    private async Task<QuotationDto> CreateAndConfirmAsync(UpsertQuotationRequest? req = null)
    {
        var create = await _client.PostAsJsonAsync("/api/quotations", req ?? BuildRequest());
        create.EnsureSuccessStatusCode();
        var id = (await create.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!.Id;
        await _client.PostAsJsonAsync($"/api/quotations/{id}/transition", new TransitionQuotationRequest { Action = QuotationAction.Send });
        var confirmResp = await _client.PostAsJsonAsync($"/api/quotations/{id}/transition", new TransitionQuotationRequest { Action = QuotationAction.Confirm });
        confirmResp.EnsureSuccessStatusCode();
        return (await confirmResp.Content.ReadFromJsonAsync<ApiResponse<QuotationDto>>(TestJson.Options))!.Data!;
    }
}
```

3. **Chạy test để xác nhận FAIL** (compilation error vì `SalesRevenueLineItemDto` chưa tồn tại):

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "SalesRevenueLineItemsTests" --logger "console;verbosity=normal"
```

Expected: Build error — `The type or namespace name 'SalesRevenueLineItemDto' could not be found`

### Task 2 — Thêm DTOs vào Models

Mở `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs` và **thêm** hai class sau vào cuối file:

```csharp
public class SalesRevenueLineItemsRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class SalesRevenueLineItemDto
{
    public Guid QuotationId { get; set; }
    public string QuotationCode { get; set; } = default!;
    public DateOnly QuotationDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerAddress { get; set; }
    public string? ContactPhone { get; set; }
    public decimal Freight { get; set; }
    public bool IsFirstLineOfQuotation { get; set; }

    public string ProductName { get; set; } = default!;
    public string? Specification { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Null when caller lacks quotations.view_cost
    public decimal? UnitCost { get; set; }
    public decimal? LineCost { get; set; }
    public decimal? LineProfit { get; set; }
}
```

### Task 3 — Cập nhật interface

Mở `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs` và **thêm** method:

```csharp
Task<List<SalesRevenueLineItemDto>> GetLineItemsAsync(
    Guid saleUserId,
    SalesRevenueLineItemsRequest request,
    CancellationToken ct = default);
```

File sau khi sửa:

```csharp
using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

public interface ISalesRevenueReportService
{
    Task<SalesRevenueReportDto> GetAsync(SalesRevenueReportRequest request, CancellationToken ct = default);

    Task<List<SalesRevenueLineItemDto>> GetLineItemsAsync(
        Guid saleUserId,
        SalesRevenueLineItemsRequest request,
        CancellationToken ct = default);
}
```

### Task 4 — Implement GetLineItemsAsync trong Service

Mở `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Services/SalesRevenueReportService.cs`.

**a.** Thêm `using`:
```csharp
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Domain.Constants;
```

**b.** Thêm field và cập nhật constructor để inject `ICurrentUser`:
```csharp
private readonly ICurrentUser _currentUser;

public SalesRevenueReportService(IAppDbContext db, ICurrentUser currentUser)
{
    _db = db;
    _currentUser = currentUser;
}
```

**c.** Thêm method `GetLineItemsAsync` sau method `GetAsync` hiện có:

```csharp
public async Task<List<SalesRevenueLineItemDto>> GetLineItemsAsync(
    Guid saleUserId,
    SalesRevenueLineItemsRequest request,
    CancellationToken ct = default)
{
    var fromUtc = DateTime.SpecifyKind(request.From.Date, DateTimeKind.Utc);
    var toExclusiveUtc = DateTime.SpecifyKind(request.To.Date.AddDays(1), DateTimeKind.Utc);
    var canViewCost = _currentUser.HasPermission(Permissions.Quotations.ViewCost);

    var quotations = await _db.Quotations
        .AsNoTracking()
        .Where(q => q.Status == QuotationStatus.Confirmed
            && q.CancelledAt == null
            && q.ConfirmedAt != null
            && q.ConfirmedAt >= fromUtc
            && q.ConfirmedAt < toExclusiveUtc
            && q.OwnerUserId == saleUserId)
        .Include(q => q.Lines)
        .OrderByDescending(q => q.ConfirmedAt)
        .ToListAsync(ct);

    var result = new List<SalesRevenueLineItemDto>();
    foreach (var q in quotations)
    {
        var lines = q.Lines.OrderBy(l => l.SortOrder).ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            result.Add(new SalesRevenueLineItemDto
            {
                QuotationId              = q.Id,
                QuotationCode            = q.Code,
                QuotationDate            = q.QuotationDate,
                ConfirmedAt              = q.ConfirmedAt,
                CustomerName             = q.CustomerName,
                CustomerAddress          = q.CustomerAddress,
                ContactPhone             = q.ContactPhone,
                Freight                  = q.Freight,
                IsFirstLineOfQuotation   = i == 0,
                ProductName              = line.ProductName,
                Specification            = line.Specification,
                Quantity                 = line.Quantity,
                UnitPrice                = line.UnitPrice,
                LineTotal                = line.LineTotal,
                UnitCost                 = canViewCost ? line.UnitCost   : null,
                LineCost                 = canViewCost ? line.LineCost   : null,
                LineProfit               = canViewCost ? line.LineProfit : null,
            });
        }
    }
    return result;
}
```

> **Note:** `ICurrentUser` đã được đăng ký trong DI bởi `WebApi` (pattern giống `QuotationService`). Không cần thay đổi `DependencyInjection.cs`.

### Task 5 — Thêm endpoint vào Controller

Mở `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`.

**Thêm** action sau action `SalesRevenue` hiện có:

```csharp
[HttpGet("sales-revenue/{saleUserId:guid}/lines")]
[HasPermission(Permissions.Reports.Revenue)]
public async Task<ActionResult<ApiResponse<List<SalesRevenueLineItemDto>>>> SalesRevenueLines(
    Guid saleUserId,
    [FromQuery] SalesRevenueLineItemsRequest request,
    CancellationToken ct)
{
    // Reuse the same date-range validation rules as the summary report
    var validator = new FluentValidation.InlineValidator<SalesRevenueLineItemsRequest>();
    validator.RuleFor(x => x.From).LessThanOrEqualTo(x => x.To)
        .WithMessage("From phải <= To.");
    validator.RuleFor(x => x).Must(x => (x.To - x.From).TotalDays <= 366)
        .WithMessage("Khoảng thời gian tối đa 366 ngày.");
    await validator.ValidateAndThrowAsync(request, ct);

    return Success(await _salesRevenue.GetLineItemsAsync(saleUserId, request, ct));
}
```

> **Alternative:** Nếu không muốn dùng `InlineValidator`, tạo file riêng `SalesRevenueLineItemsRequestValidator.cs` trong `Validators/` folder với cùng logic như `SalesRevenueReportRequestValidator`. Chọn cách nào cũng được — dùng file riêng sạch hơn và nhất quán với pattern hiện tại. Ưu tiên **tạo file riêng** để giữ nhất quán.

**Tạo validator riêng** `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Validators/SalesRevenueLineItemsRequestValidator.cs`:

```csharp
using FluentValidation;
using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Validators;

public class SalesRevenueLineItemsRequestValidator : AbstractValidator<SalesRevenueLineItemsRequest>
{
    public SalesRevenueLineItemsRequestValidator()
    {
        RuleFor(x => x.From).LessThanOrEqualTo(x => x.To)
            .WithMessage("From phải <= To.");
        RuleFor(x => x).Must(x => (x.To - x.From).TotalDays <= 366)
            .WithMessage("Khoảng thời gian tối đa 366 ngày.");
    }
}
```

Cập nhật action trong controller để dùng injected validator:

**Cập nhật constructor của `ReportsController`** thêm:
```csharp
private readonly IValidator<SalesRevenueLineItemsRequest> _salesRevenueLineItemsValidator;
```

Thêm param vào constructor:
```csharp
IValidator<SalesRevenueLineItemsRequest> salesRevenueLineItemsValidator
```

Gán trong body:
```csharp
_salesRevenueLineItemsValidator = salesRevenueLineItemsValidator;
```

Cập nhật action:
```csharp
[HttpGet("sales-revenue/{saleUserId:guid}/lines")]
[HasPermission(Permissions.Reports.Revenue)]
public async Task<ActionResult<ApiResponse<List<SalesRevenueLineItemDto>>>> SalesRevenueLines(
    Guid saleUserId,
    [FromQuery] SalesRevenueLineItemsRequest request,
    CancellationToken ct)
{
    await _salesRevenueLineItemsValidator.ValidateAndThrowAsync(request, ct);
    return Success(await _salesRevenue.GetLineItemsAsync(saleUserId, request, ct));
}
```

FluentValidation tự scan và đăng ký validators bằng `AddValidatorsFromAssemblyContaining` trong `DependencyInjection` — không cần thêm thủ công.

### Task 6 — Chạy tests để xác nhận PASS

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "SalesRevenueLineItemsTests" --logger "console;verbosity=normal"
```

Expected: 3 tests PASS.

### Task 7 — Commit

```bash
git add backend/src/OrderMgmt.Application/Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs
git add backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs
git add backend/src/OrderMgmt.Application/Reports/SalesRevenue/Services/SalesRevenueReportService.cs
git add backend/src/OrderMgmt.Application/Reports/SalesRevenue/Validators/SalesRevenueLineItemsRequestValidator.cs
git add backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs
git add backend/tests/OrderMgmt.IntegrationTests/Reports/SalesRevenueLineItemsTests.cs
git commit -m "feat: add sales-revenue line-items drill-down endpoint"
```

## Verification

```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "SalesRevenueLineItemsTests" --logger "console;verbosity=normal"
```

All 3 tests green.

## Exit Criteria

- [ ] `SalesRevenueLineItemDto` và `SalesRevenueLineItemsRequest` tồn tại trong Models
- [ ] `ISalesRevenueReportService.GetLineItemsAsync` được implement trong service
- [ ] `GET /api/reports/sales-revenue/{saleUserId}/lines?from=&to=` trả về HTTP 200 với danh sách line items
- [ ] `isFirstLineOfQuotation` là `true` chỉ trên dòng đầu mỗi báo giá
- [ ] Báo giá đã hủy không xuất hiện trong kết quả
- [ ] 3 integration tests PASS
