using System.Globalization;
using ClosedXML.Excel;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Infrastructure.Excel;

/// <summary>
/// Renders biên bản bàn giao kiêm phiếu xuất kho from a QuotationDto into an Excel byte array.
///
/// Two template variants are supported:
///   withPrice=true  — templete_bbbg.xlsx      (6 columns A-F; includes unit price + total)
///   withPrice=false — templete_bbbg_sl.xlsx   (5 columns A-E; quantity only, no price columns)
///
/// Template layout (both variants share the same header rows):
///   B6  : Date line  "Hà nội, ngày DD tháng MM năm YYYY"
///   B9  : Customer name
///   B10 : Delivery address
///   B11 : Phone / contact
///   B12 : Product names summary
///   Row 13 : Table header (read-only, not touched)
///   Row 14 : First sample item row
///   Row 15 : Second sample item row
///
/// With-price footer (rows shift after row adjustments):
///   summaryRow + 0 : Subtotal          → col F
///   summaryRow + 1 : Tax               → col F
///   summaryRow + 2 : Total             → col F
///   summaryRow + 3 : Advance payment   → col F
///   summaryRow + 4 : Remaining balance → col F
///
/// No-price footer:
///   summaryRow : Tổng cộng → col D (quantity total only)
/// </summary>
public sealed class HandoverExcelRenderer : IHandoverExcelRenderer
{
    // Both templates have the same header and item-row layout.
    private const int FirstSampleRow = 14;
    private const int SampleRowCount = 2;

    // With-price footer row offsets relative to summaryRow (the row immediately after the last item).
    // In the template the order is: Cộng tiền hàng, Thuế GTGT, Tổng cộng, Đã tạm ứng, Còn lại.
    private const int SubtotalRowOffset      = 0; // "Cộng tiền hàng"
    private const int TaxRowOffset           = 1; // "Thuế GTGT"
    private const int TotalRowOffset         = 2; // "Tổng cộng"
    private const int AdvancePaymentRowOffset = 3; // "Đã tạm ứng"
    private const int RemainingRowOffset     = 4; // "Còn lại thanh toán"

    // Column indices (1-based)
    private const int ColStt       = 1; // A
    private const int ColName      = 2; // B
    private const int ColUnit      = 3; // C
    private const int ColQty       = 4; // D
    private const int ColUnitPrice = 5; // E  (with-price only)
    private const int ColTotal     = 6; // F  (with-price only)

    private const int MaxColWithPrice   = 6;
    private const int MaxColNoPrice     = 5;

    public Task<byte[]> RenderAsync(
        QuotationDto quotation,
        string templatePath,
        bool withPrice,
        CancellationToken ct = default)
    {
        var resolved = ResolveAbsolutePath(templatePath);
        using var workbook = new XLWorkbook(resolved);
        var ws = workbook.Worksheet(1);

        FillHeader(ws, quotation);
        FillItemRows(ws, quotation, withPrice);

        ws.PageSetup.Margins.Left   = 0.4;
        ws.PageSetup.Margins.Right  = 0.4;
        ws.PageSetup.Margins.Top    = 0.5;
        ws.PageSetup.Margins.Bottom = 0.5;
        ws.PageSetup.PagesWide = 1;
        ws.PageSetup.PagesTall = 0;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    private static string ResolveAbsolutePath(string path)
    {
        var p = path;
        if (!Path.IsPathRooted(p))
            p = Path.Combine(AppContext.BaseDirectory, p);
        if (!File.Exists(p))
            throw new InvalidOperationException($"Handover Excel template not found: {p}");
        return p;
    }

    private static void FillHeader(IXLWorksheet ws, QuotationDto q)
    {
        var date = q.DeliveryDate ?? q.QuotationDate;
        ws.Cell("B6").SetValue(
            $"Hà nội, ngày {date.Day:D2} tháng {date.Month:D2} năm {date.Year}");

        ws.Cell("B9").SetValue($"Đơn vị mua hàng: {q.CustomerName}");
        ws.Cell("B10").SetValue($"Địa chỉ giao hàng: {q.DeliveryAddress ?? string.Empty}");
        ws.Cell("B11").SetValue(FormatDeliveryContact(q));
        ws.Cell("B12").SetValue(FormatProductNames(q));
        ws.Cell("B12").Style.Alignment.WrapText = true;
        ws.Row(12).AdjustToContents();
        ws.Row(12).Height=ws.Row(12).Height * 1.2; // add extra spacing for wrapped text
    }

    private static void FillItemRows(IXLWorksheet ws, QuotationDto q, bool withPrice)
    {
        var lines = q.Lines.OrderBy(l => l.SortOrder).ToList();
        int n = lines.Count;
        int lastSampleRow = FirstSampleRow + SampleRowCount - 1; // row 15

        if (n > SampleRowCount)
        {
            int extra = n - SampleRowCount;
            // Insert rows just above the summary row (currently at lastSampleRow + 1 = 16).
            ws.Row(lastSampleRow + 1).InsertRowsAbove(extra);
            // Copy style from first sample row to each inserted row.
            int maxCol = withPrice ? MaxColWithPrice : MaxColNoPrice;
            for (int i = 0; i < extra; i++)
                CopyRowStyle(ws, FirstSampleRow, lastSampleRow + 1 + i, maxCol);
        }
        else if (n < SampleRowCount)
        {
            // Delete excess sample rows bottom-up so row numbers stay stable.
            for (int r = lastSampleRow; r >= FirstSampleRow + n; r--)
                ws.Row(r).Delete();
        }

        // Summary row is directly after the last item row.
        int summaryRow = FirstSampleRow + n;

        for (int i = 0; i < n; i++)
        {
            FillItemRow(ws, FirstSampleRow + i, i + 1, lines[i], withPrice);
            ws.Row(FirstSampleRow + i).AdjustToContents();
            ws.Row(FirstSampleRow + i).Height= ws.Row(FirstSampleRow + i).Height * 1.2; // add extra spacing for wrapped text
        }

        if (withPrice)
            FillWithPriceFooter(ws, summaryRow, q, n);
        else
            FillNoPriceFooter(ws, summaryRow, q, n);
    }

    private static void FillItemRow(IXLWorksheet ws, int row, int index, QuotationLineDto line, bool withPrice)
    {
        ws.Cell(row, ColStt).SetValue(index);
        ws.Cell(row, ColName).SetValue(FormatItemDescription(line));
        ws.Cell(row, ColUnit).SetValue(line.UnitName);
        ws.Cell(row, ColQty).SetValue((double)line.Quantity);

        if (withPrice)
        {
            ws.Cell(row, ColUnitPrice).SetValue((double)line.UnitPrice);
            ws.Cell(row, ColTotal).SetValue((double)line.LineTotal);
        }
        // When withPrice=false, leave columns E and beyond as-is (they don't exist in the no-price template).
    }

    private static void FillWithPriceFooter(IXLWorksheet ws, int summaryRow, QuotationDto q, int n)
    {
        // Subtotal = sum of line totals
        if (n > 0)
        {
            ws.Cell(summaryRow + SubtotalRowOffset, ColTotal).FormulaA1 =
                $"SUM(F{FirstSampleRow}:F{FirstSampleRow + n - 1})";
        }
        else
        {
            ws.Cell(summaryRow + SubtotalRowOffset, ColTotal).SetValue(0);
        }

        ws.Cell(summaryRow + TaxRowOffset, ColUnitPrice).SetValue((double)(q.TaxRate / 100m));
        ws.Cell(summaryRow + TaxRowOffset, ColTotal).SetValue((double)q.TaxAmount);
        ws.Cell(summaryRow + TotalRowOffset, ColTotal).SetValue((double)(q.Subtotal + q.TaxAmount));
        ws.Cell(summaryRow + AdvancePaymentRowOffset, ColTotal).SetValue((double)q.AdvancePayment);
        ws.Cell(summaryRow + RemainingRowOffset, ColTotal).SetValue((double)(q.Subtotal + q.TaxAmount - q.AdvancePayment));
    }

    private static void FillNoPriceFooter(IXLWorksheet ws, int summaryRow, QuotationDto q, int n)
    {
        // No-price template: only show total quantity in column D.
        if (n > 0)
        {
            ws.Cell(summaryRow, ColQty).FormulaA1 =
                $"SUM(D{FirstSampleRow}:D{FirstSampleRow + n - 1})";
        }
        else
        {
            ws.Cell(summaryRow, ColQty).SetValue(0);
        }
    }

    private static void CopyRowStyle(IXLWorksheet ws, int sourceRow, int targetRow, int maxCol)
    {
        for (int col = 1; col <= maxCol; col++)
        {
            var src = ws.Cell(sourceRow, col);
            var dst = ws.Cell(targetRow, col);
            dst.Style.Font.FontName = src.Style.Font.FontName;
            dst.Style.Font.FontSize = src.Style.Font.FontSize;
            dst.Style.Font.Bold = src.Style.Font.Bold;
            dst.Style.Fill.BackgroundColor = src.Style.Fill.BackgroundColor;
            dst.Style.Border.TopBorder = src.Style.Border.TopBorder;
            dst.Style.Border.BottomBorder = src.Style.Border.BottomBorder;
            dst.Style.Border.LeftBorder = src.Style.Border.LeftBorder;
            dst.Style.Border.RightBorder = src.Style.Border.RightBorder;
            dst.Style.Border.TopBorderColor = src.Style.Border.TopBorderColor;
            dst.Style.Border.BottomBorderColor = src.Style.Border.BottomBorderColor;
            dst.Style.Border.LeftBorderColor = src.Style.Border.LeftBorderColor;
            dst.Style.Border.RightBorderColor = src.Style.Border.RightBorderColor;
            dst.Style.Alignment.Horizontal = src.Style.Alignment.Horizontal;
            dst.Style.Alignment.Vertical = src.Style.Alignment.Vertical;
            dst.Style.NumberFormat.Format = src.Style.NumberFormat.Format;
        }
        ws.Row(targetRow).Height = ws.Row(sourceRow).Height;
    }

    private static string FormatDeliveryContact(QuotationDto q)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.DeliveryPhone)) parts.Add(q.DeliveryPhone);
        if (!string.IsNullOrWhiteSpace(q.DeliveryRecipient)) parts.Add(q.DeliveryRecipient);
        return $"Điện thoại: {string.Join(" - ", parts)}";
    }

    private static string FormatProductNames(QuotationDto q)
    {
        var names = q.Lines
            .Where(l => !IsShippingGroup(l))
            .Where(l => !string.IsNullOrWhiteSpace(l.ProductGroupName))
            .GroupBy(l => l.ProductGroupName!.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Min(l => l.ProductGroupSortOrder ?? int.MaxValue))
            .ThenBy(g => g.Key, StringComparer.Create(new CultureInfo("vi-VN"), ignoreCase: true))
            .Select(g => g.Key)
            .ToList();
        return $"Hàng hóa cung cấp: {string.Join(", ", names)}";
    }

    private static bool IsShippingGroup(QuotationLineDto line)
    {
        if (string.Equals(line.ProductGroupCode, "VC", StringComparison.OrdinalIgnoreCase))
            return true;

        var name = line.ProductGroupName ?? string.Empty;
        return ContainsIgnoreAccent(name, "vận chuyển") || ContainsIgnoreAccent(name, "van chuyen");
    }

    private static bool ContainsIgnoreAccent(string source, string value) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            source, value,
            CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0;

    private static string FormatItemDescription(QuotationLineDto line) => line.ProductName;
}
