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
