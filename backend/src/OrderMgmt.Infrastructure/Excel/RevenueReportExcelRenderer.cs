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
