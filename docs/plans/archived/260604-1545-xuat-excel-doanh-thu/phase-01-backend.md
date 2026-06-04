# Phase 01 — Backend: Renderer + Endpoint

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo `IRevenueReportExcelRenderer` interface, `RevenueReportExcelRenderer` implementation dùng ClosedXML, đăng ký DI, thêm endpoint `GET /api/reports/revenue-lines/excel`, và integration test.

## Files

- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/IRevenueReportExcelRenderer.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/Excel/RevenueReportExcelRenderer.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` (edit)
- `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs` (edit)
- `backend/tests/OrderMgmt.IntegrationTests/Reports/RevenueLineItemsExportTests.cs` (new)

## Tasks

### Task 1 — Write failing integration test

1. Tạo file `backend/tests/OrderMgmt.IntegrationTests/Reports/RevenueLineItemsExportTests.cs`:

```csharp
using System.Net;
using ClosedXML.Excel;
using FluentAssertions;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;
using OrderMgmt.IntegrationTests.Fixtures;
using OrderMgmt.IntegrationTests.Quotations;
using Xunit;

namespace OrderMgmt.IntegrationTests.Reports;

[Collection(nameof(PostgresCollection))]
public class RevenueLineItemsExportTests : QuotationTestBase
{
    public RevenueLineItemsExportTests(PostgresFixture pg) : base(pg) { }

    [Fact]
    public async Task Excel_Returns200WithXlsxContentType()
    {
        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync(
            $"/api/reports/revenue-lines/excel?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Excel_HeaderRow_HasExpectedColumns()
    {
        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync(
            $"/api/reports/revenue-lines/excel?from={from}&to={to}");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);

        ws.Cell(1, 1).GetString().Should().Be("Ngày");
        ws.Cell(1, 2).GetString().Should().Be("Mã BG");
        ws.Cell(1, 3).GetString().Should().Be("Địa chỉ giao hàng");
        ws.Cell(1, 4).GetString().Should().Be("Hàng hóa");
        ws.Cell(1, 5).GetString().Should().Be("Kích thước");
        ws.Cell(1, 6).GetString().Should().Be("Tỷ trọng");
        ws.Cell(1, 7).GetString().Should().Be("SL m²");
        ws.Cell(1, 8).GetString().Should().Be("SL tấm");
        ws.Cell(1, 9).GetString().Should().Be("Đơn giá");
        ws.Cell(1, 10).GetString().Should().Be("Thành tiền");
        ws.Cell(1, 11).GetString().Should().Be("Cước vận chuyển");
        ws.Cell(1, 12).GetString().Should().Be("VAT");
        ws.Cell(1, 13).GetString().Should().Be("Tổng cộng");
        ws.Cell(1, 14).GetString().Should().Be("Giá nhập");
        ws.Cell(1, 15).GetString().Should().Be("Thành tiền nhập");
        ws.Cell(1, 16).GetString().Should().Be("Chênh lệch");
        ws.Cell(1, 17).GetString().Should().Be("Chênh + cước");
        ws.Cell(1, 18).GetString().Should().Be("Liên hệ");
    }

    [Fact]
    public async Task Excel_DataRows_ReflectConfirmedQuotation()
    {
        // Arrange: tạo và confirm một quotation
        var req = BuildRequest(new UpsertQuotationLineRequest
        {
            SortOrder = 0,
            ProductId = _productId,
            ProductName = "Test EPS Export",
            UnitName = "Tấm",
            PricingMode = PricingMode.PerUnit,
            Quantity = 3,
            UnitPrice = 10_000,
        });
        var create = await _client.PostAsJsonAsync("/api/quotations", req);
        create.EnsureSuccessStatusCode();
        var body = await create.Content.ReadFromJsonAsync<OrderMgmt.Application.Common.Models.ApiResponse<OrderMgmt.Application.Sales.Quotations.Models.QuotationDto>>(
            OrderMgmt.IntegrationTests.Fixtures.TestJson.Options);
        var id = body!.Data!.Id;
        var code = body!.Data!.Code;

        await _client.PostAsJsonAsync($"/api/quotations/{id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Send });
        var confirm = await _client.PostAsJsonAsync($"/api/quotations/{id}/transition",
            new TransitionQuotationRequest { Action = QuotationAction.Confirm });
        confirm.EnsureSuccessStatusCode();

        var from = DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd");
        var to   = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync(
            $"/api/reports/revenue-lines/excel?from={from}&to={to}");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        // Assert: data row exists with matching quotation code and product name
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);

        var dataRows = ws.RowsUsed().Skip(1).ToList(); // skip header
        dataRows.Should().NotBeEmpty();

        var matchingRow = dataRows.FirstOrDefault(r => r.Cell(2).GetString() == code);
        matchingRow.Should().NotBeNull("should have a row for the confirmed quotation");
        matchingRow!.Cell(4).GetString().Should().Be("Test EPS Export");
        matchingRow.Cell(7).GetDouble().Should().Be(3.0); // quantity
        matchingRow.Cell(9).GetDouble().Should().Be(10_000.0); // unit price
        matchingRow.Cell(10).GetDouble().Should().Be(30_000.0); // line total = 3 * 10000

        // Footer row
        var footerRow = ws.RowsUsed().Last();
        footerRow.Cell(1).GetString().Should().Be("Tổng cộng");
        footerRow.Cell(7).GetDouble().Should().Be(3.0);   // total quantity
        footerRow.Cell(10).GetDouble().Should().Be(30_000.0); // total line total
    }

    [Fact]
    public async Task Excel_EmptyRange_ReturnsFileWithOnlyHeaderRow()
    {
        // Khoảng ngày xa trong tương lai → không có dữ liệu
        var response = await _client.GetAsync(
            "/api/reports/revenue-lines/excel?from=2099-01-01&to=2099-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheet(1);

        var usedRows = ws.RowsUsed().Count();
        usedRows.Should().Be(1, "only header row when no data");
    }
}
```

2. Chạy test để xác nhận FAIL:
```
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~RevenueLineItemsExportTests" \
  -- TestRunParameters.Parameter(name="TEST_DB_CONNECTION", value="<test-conn-str>")
```
Expected: FAIL — `Excel_Returns200WithXlsxContentType` fails with 404 Not Found (endpoint chưa tồn tại).

---

### Task 2 — Create Application interface

Tạo `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/IRevenueReportExcelRenderer.cs`:

```csharp
using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Interfaces;

public interface IRevenueReportExcelRenderer
{
    (byte[] Bytes, string FileName) Render(
        List<SalesRevenueLineItemDto> items,
        DateTime from,
        DateTime to);
}
```

---

### Task 3 — Implement RevenueReportExcelRenderer

Tạo `backend/src/OrderMgmt.Infrastructure/Excel/RevenueReportExcelRenderer.cs`:

```csharp
using ClosedXML.Excel;
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Infrastructure.Excel;

public class RevenueReportExcelRenderer : IRevenueReportExcelRenderer
{
    private static readonly string[] Headers =
    [
        "Ngày", "Mã BG", "Địa chỉ giao hàng", "Hàng hóa", "Kích thước",
        "Tỷ trọng", "SL m²", "SL tấm", "Đơn giá", "Thành tiền",
        "Cước vận chuyển", "VAT", "Tổng cộng",
        "Giá nhập", "Thành tiền nhập", "Chênh lệch", "Chênh + cước",
        "Liên hệ",
    ];

    public (byte[] Bytes, string FileName) Render(
        List<SalesRevenueLineItemDto> items,
        DateTime from,
        DateTime to)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Chi tiết doanh thu");

        WriteHeaderRow(ws);

        int row = 2;
        foreach (var item in items)
            WriteDataRow(ws, row++, item);

        if (items.Count > 0)
            WriteFooterRow(ws, row, items);

        ws.Columns().AdjustToContents(1, row);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        var fromStr = from.ToString("yyyyMMdd");
        var toStr   = to.ToString("yyyyMMdd");
        return (ms.ToArray(), $"BaoCaoDoanhThu_{fromStr}_{toStr}.xlsx");
    }

    private static void WriteHeaderRow(IXLWorksheet ws)
    {
        for (int i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.SetValue(Headers[i]);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void WriteDataRow(IXLWorksheet ws, int row, SalesRevenueLineItemDto item)
    {
        bool isFirst = item.IsFirstLineOfQuotation;

        // Col 1: Ngày (only on first line of quotation)
        if (isFirst)
        {
            var d = item.RevenueDate ?? (item.ConfirmedAt.HasValue ? (DateTime?)item.ConfirmedAt.Value : null);
            if (d.HasValue)
                ws.Cell(row, 1).SetValue(d.Value.ToString("dd/MM"));
        }

        // Col 2: Mã BG (only on first line)
        if (isFirst)
            ws.Cell(row, 2).SetValue(item.QuotationCode);

        // Col 3: Địa chỉ giao hàng (only on first line)
        if (isFirst)
            ws.Cell(row, 3).SetValue(item.DeliveryAddress ?? item.CustomerAddress ?? string.Empty);

        // Col 4: Hàng hóa
        ws.Cell(row, 4).SetValue(item.ProductName);

        // Col 5: Kích thước
        ws.Cell(row, 5).SetValue(FormatSize(item));

        // Col 6: Tỷ trọng
        if (item.Density.HasValue)
            SetDecimalCell(ws.Cell(row, 6), (double)item.Density.Value);

        // Col 7: SL m²
        SetDecimalCell(ws.Cell(row, 7), (double)item.Quantity);

        // Col 8: SL tấm
        if (item.SheetCount.HasValue)
            SetDecimalCell(ws.Cell(row, 8), (double)item.SheetCount.Value);

        // Col 9: Đơn giá
        SetMoneyCell(ws.Cell(row, 9), (double)item.UnitPrice);

        // Col 10: Thành tiền
        SetMoneyCell(ws.Cell(row, 10), (double)item.LineTotal);

        if (isFirst)
        {
            // Col 11: Cước vận chuyển
            SetMoneyCell(ws.Cell(row, 11), (double)item.Freight);

            // Col 12: VAT
            SetMoneyCell(ws.Cell(row, 12), (double)item.TaxAmount);

            // Col 13: Tổng cộng
            SetMoneyCell(ws.Cell(row, 13), (double)item.Total);
        }

        // Col 14: Giá nhập
        if (item.UnitCost.HasValue)
            SetMoneyCell(ws.Cell(row, 14), (double)item.UnitCost.Value);

        // Col 15: Thành tiền nhập
        if (item.LineCost.HasValue)
            SetMoneyCell(ws.Cell(row, 15), (double)item.LineCost.Value);

        // Col 16: Chênh lệch
        if (item.LineProfit.HasValue)
            SetMoneyCell(ws.Cell(row, 16), (double)item.LineProfit.Value);

        // Col 17: Chênh + cước (lineProfit + freight, only meaningful on first line where freight is non-zero)
        if (item.LineProfit.HasValue)
        {
            var freight = isFirst ? (double)item.Freight : 0.0;
            SetMoneyCell(ws.Cell(row, 17), (double)item.LineProfit.Value + freight);
        }

        // Col 18: Liên hệ (only on first line)
        if (isFirst)
            ws.Cell(row, 18).SetValue(item.DeliveryPhone ?? item.ContactPhone ?? string.Empty);
    }

    private static void WriteFooterRow(IXLWorksheet ws, int row, List<SalesRevenueLineItemDto> items)
    {
        ws.Cell(row, 1).SetValue("Tổng cộng");
        ws.Cell(row, 1).Style.Font.Bold = true;

        ws.Cell(row, 7).SetValue((double)items.Sum(i => i.Quantity));
        ws.Cell(row, 7).Style.Font.Bold = true;
        ApplyDecimalFormat(ws.Cell(row, 7));

        var sheetTotal = items.Sum(i => i.SheetCount ?? 0);
        if (sheetTotal != 0)
        {
            ws.Cell(row, 8).SetValue((double)sheetTotal);
            ws.Cell(row, 8).Style.Font.Bold = true;
            ApplyDecimalFormat(ws.Cell(row, 8));
        }

        SetBoldMoneyCell(ws.Cell(row, 10), (double)items.Sum(i => i.LineTotal));

        var firstLines = items.Where(i => i.IsFirstLineOfQuotation).ToList();
        SetBoldMoneyCell(ws.Cell(row, 11), (double)firstLines.Sum(i => i.Freight));
        SetBoldMoneyCell(ws.Cell(row, 12), (double)firstLines.Sum(i => i.TaxAmount));
        SetBoldMoneyCell(ws.Cell(row, 13), (double)firstLines.Sum(i => i.Total));

        if (items.Any(i => i.LineCost.HasValue))
            SetBoldMoneyCell(ws.Cell(row, 15), (double)items.Sum(i => i.LineCost ?? 0));

        if (items.Any(i => i.LineProfit.HasValue))
        {
            SetBoldMoneyCell(ws.Cell(row, 16), (double)items.Sum(i => i.LineProfit ?? 0));

            // Chênh + cước: sum of (lineProfit + freight) per first-line item
            var profitPlusFreight = items
                .Where(i => i.LineProfit.HasValue)
                .Sum(i => i.LineProfit!.Value + (i.IsFirstLineOfQuotation ? i.Freight : 0));
            SetBoldMoneyCell(ws.Cell(row, 17), (double)profitPlusFreight);
        }
    }

    private static string FormatSize(SalesRevenueLineItemDto item)
    {
        var dims = new[] { item.Length, item.Width, item.Thickness }
            .Where(v => v.HasValue)
            .Select(v => v!.Value.ToString("0.##"))
            .ToList();
        return dims.Count > 0 ? string.Join(" x ", dims) : (item.Specification ?? string.Empty);
    }

    private static void SetMoneyCell(IXLCell cell, double value)
    {
        cell.SetValue(value);
        ApplyMoneyFormat(cell);
    }

    private static void SetBoldMoneyCell(IXLCell cell, double value)
    {
        cell.SetValue(value);
        ApplyMoneyFormat(cell);
        cell.Style.Font.Bold = true;
    }

    private static void ApplyMoneyFormat(IXLCell cell)
    {
        cell.Style.NumberFormat.Format = "#,##0";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void SetDecimalCell(IXLCell cell, double value)
    {
        cell.SetValue(value);
        ApplyDecimalFormat(cell);
    }

    private static void ApplyDecimalFormat(IXLCell cell)
    {
        cell.Style.NumberFormat.Format = "#,##0.##";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
}
```

---

### Task 4 — Register renderer in DI

Sửa `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` — thêm sau dòng `AddScoped<IHandoverExcelRenderer, HandoverExcelRenderer>()`:

```csharp
// Thêm dòng này:
services.AddScoped<IRevenueReportExcelRenderer, RevenueReportExcelRenderer>();
```

Cũng thêm using:
```csharp
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
```

---

### Task 5 — Add controller endpoint

Sửa `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`:

**Constructor**: thêm `IRevenueReportExcelRenderer revenueExcelRenderer` vào constructor, lưu vào field `_revenueExcelRenderer`.

**Endpoint mới** — thêm sau action `RevenueLines`:

```csharp
[HttpGet("revenue-lines/excel")]
[HasPermission(Permissions.Reports.Revenue)]
public async Task<IActionResult> RevenueLineItemsExcel(
    [FromQuery] SalesRevenueLineItemsRequest request,
    CancellationToken ct)
{
    await _salesRevenueLineItemsValidator.ValidateAndThrowAsync(request, ct);
    var items = await _salesRevenue.GetLineItemsAsync(request, ct);
    var (bytes, fileName) = _revenueExcelRenderer.Render(items, request.From, request.To);
    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}
```

Thêm field trong class:
```csharp
private readonly IRevenueReportExcelRenderer _revenueExcelRenderer;
```

Thêm using:
```csharp
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
```

---

### Task 6 — Run tests to verify PASS

```
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~RevenueLineItemsExportTests" \
  -- TestRunParameters.Parameter(name="TEST_DB_CONNECTION", value="<test-conn-str>")
```
Expected: PASS — tất cả 4 test cases pass.

---

### Task 7 — Commit

```
git add backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/IRevenueReportExcelRenderer.cs \
        backend/src/OrderMgmt.Infrastructure/Excel/RevenueReportExcelRenderer.cs \
        backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs \
        backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs \
        backend/tests/OrderMgmt.IntegrationTests/Reports/RevenueLineItemsExportTests.cs
git commit -m "feat: add revenue line items Excel export endpoint"
```

## Verification

```
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~RevenueLineItemsExportTests" \
  -- TestRunParameters.Parameter(name="TEST_DB_CONNECTION", value="<test-conn-str>")
```

## Exit Criteria

- [ ] 4 integration tests pass
- [ ] `GET /api/reports/revenue-lines/excel?from=2026-01-01&to=2026-12-31` trả 200 với `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- [ ] File download được mở bằng Excel với 18 cột header đúng
- [ ] Khi không có dữ liệu: file có header row, không lỗi 500
