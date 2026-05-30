using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Infrastructure.Excel;

public class QuotationExcelRenderer : IQuotationExcelRenderer
{
    // Template has 2 sample item rows: 15 and 16.
    private const int FirstSampleRow = 15;
    private const int SampleRowCount = 2;

    // Offsets from the subtotal row in template_baogia.xlsx.
    internal const int TaxRowOffset = 1;
    internal const int TotalRowOffset = 2;
    internal const int AdvancePaymentRowOffset = 3;
    internal const int RemainingBalanceRowOffset = 4;

    private readonly IOptions<QuotationExportOptions> _options;

    public QuotationExcelRenderer(IOptions<QuotationExportOptions> options)
        => _options = options;

    public Task<byte[]> RenderAsync(QuotationDto quotation, CancellationToken ct = default)
        => RenderAsync(quotation, ResolveDefaultTemplatePath(), ct);

    public Task<byte[]> RenderAsync(QuotationDto quotation, string templatePath, CancellationToken ct = default)
    {
        var resolved = ResolveAbsolutePath(templatePath);
        using var workbook = new XLWorkbook(resolved);
        var ws = workbook.Worksheet(1);

        FillHeader(ws, quotation);
        FillItemRows(ws, quotation);
        FillSummaryTotals(ws, FirstSampleRow + quotation.Lines.Count, quotation);

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

    private string ResolveDefaultTemplatePath() => ResolveAbsolutePath(_options.Value.TemplatePath);

    private static string ResolveAbsolutePath(string path)
    {
        var p = path;
        if (!Path.IsPathRooted(p))
            p = Path.Combine(AppContext.BaseDirectory, p);
        if (!File.Exists(p))
            throw new InvalidOperationException($"Quotation Excel template not found: {p}");
        return p;
    }

    private static void FillHeader(IXLWorksheet ws, QuotationDto q)
    {
        ws.Cell("A8").SetValue($"Số: {q.Code}");
        ws.Cell("A9").SetValue($"Hà nội, ngày {q.QuotationDate.Day:D2} tháng {q.QuotationDate.Month:D2} năm {q.QuotationDate.Year}");
        ws.Cell("B10").SetValue($"Đơn vị mua hàng: {q.CustomerName}");
        ws.Cell("B11").SetValue($"Địa chỉ giao hàng: {q.DeliveryAddress ?? string.Empty}");
        ws.Cell("B12").SetValue(FormatDeliveryContact(q));
        ws.Cell("B13").SetValue(FormatProductNames(q));
        ws.Cell("B13").Style.Alignment.WrapText = true;
        ws.Row(13).AdjustToContents();
        ws.Row(13).Height = ws.Row(13).Height * 1.2; // Add some extra vertical padding for readability
    }

    private static void FillItemRows(IXLWorksheet ws, QuotationDto q)
    {
        var lines = q.Lines.OrderBy(l => l.SortOrder).ToList();
        int n = lines.Count;
        int lastSampleRow = FirstSampleRow + SampleRowCount - 1; // 16

        if (n > SampleRowCount)
        {
            int extra = n - SampleRowCount;
            // Insert extra rows just above the summary row (currently at lastSampleRow + 1 = 17).
            // Inserted rows inherit formatting from the row above (sample row 16).
            ws.Row(lastSampleRow + 1).InsertRowsAbove(extra);
            // Explicitly copy style from first sample row to each new row.
            for (int i = 0; i < extra; i++)
                CopyRowStyle(ws, FirstSampleRow, lastSampleRow + 1 + i);
        }
        else if (n < SampleRowCount)
        {
            // Delete excess sample rows bottom-up so row numbers stay stable.
            for (int r = lastSampleRow; r >= FirstSampleRow + n; r--)
                ws.Row(r).Delete();
        }

        // Summary row is now directly after the last item row.
        int summaryRow = FirstSampleRow + n;

        for (int i = 0; i < n; i++)
        {
            FillItemRow(ws, FirstSampleRow + i, i + 1, lines[i]);
            ws.Row(FirstSampleRow + i).AdjustToContents();
            ws.Row(FirstSampleRow + i).Height=ws.Row(FirstSampleRow + i).Height * 1.2; // Add some extra vertical padding for readability
        }

        if (n > 0)
        {
            ws.Cell(summaryRow, 4).FormulaA1 = $"SUM(D{FirstSampleRow}:D{FirstSampleRow + n - 1})";
            ws.Cell(summaryRow, 6).FormulaA1 = $"SUM(F{FirstSampleRow}:F{FirstSampleRow + n - 1})";
        }
        else
        {
            ws.Cell(summaryRow, 4).SetValue(0);
            ws.Cell(summaryRow, 6).SetValue(0);
        }
    }

    private static void FillSummaryTotals(IXLWorksheet ws, int summaryRow, QuotationDto q)
    {
        ws.Cell(summaryRow + TaxRowOffset, 5).SetValue((double)(q.TaxRate / 100m));
        ws.Cell(summaryRow + TaxRowOffset, 6).SetValue((double)q.TaxAmount);
        ws.Cell(summaryRow + TotalRowOffset, 6).SetValue((double)(q.Subtotal + q.TaxAmount));
        ws.Cell(summaryRow + AdvancePaymentRowOffset, 6).SetValue((double)q.AdvancePayment);
        ws.Cell(summaryRow + RemainingBalanceRowOffset, 6).SetValue((double)(q.Subtotal + q.TaxAmount - q.AdvancePayment));
    }

    private static void FillItemRow(IXLWorksheet ws, int row, int index, QuotationLineDto line)
    {
        ws.Cell(row, 1).SetValue(index);
        ws.Cell(row, 2).SetValue(line.ProductName);
        ws.Cell(row, 3).SetValue(line.UnitName);
        ws.Cell(row, 4).SetValue((double)line.Quantity);
        ws.Cell(row, 5).SetValue((double)line.UnitPrice);
        ws.Cell(row, 6).SetValue((double)line.LineTotal);
    }

    private static void CopyRowStyle(IXLWorksheet ws, int sourceRow, int targetRow)
    {
        for (int col = 1; col <= 6; col++)
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
        return $"Điện thoại người nhận: {string.Join(" - ", parts)}";
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

}
