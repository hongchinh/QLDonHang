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
        ws.Cell("C9").SetValue($"Hà nội, ngày {q.QuotationDate.Day:D2} tháng {q.QuotationDate.Month:D2} năm {q.QuotationDate.Year}");
        ws.Cell("B10").SetValue($"Đơn vị mua hàng: {q.CustomerName}");
        ws.Cell("B11").SetValue($"Địa chỉ giao hàng: {q.DeliveryAddress ?? string.Empty}");
        ws.Cell("B12").SetValue(FormatDeliveryContact(q));
        ws.Cell("B13").SetValue(FormatProductNames(q));
        ws.Cell("B13").Style.Alignment.WrapText = true;
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
            FillItemRow(ws, FirstSampleRow + i, i + 1, lines[i]);

        if (n > 0)
        {
            ws.Cell(summaryRow, 4).FormulaA1 = $"SUM(D{FirstSampleRow}:D{FirstSampleRow + n - 1})";
            ws.Cell(summaryRow, 7).FormulaA1 = $"SUM(G{FirstSampleRow}:G{FirstSampleRow + n - 1})";
        }
        else
        {
            ws.Cell(summaryRow, 4).SetValue(0);
            ws.Cell(summaryRow, 7).SetValue(0);
        }
    }

    private static void FillItemRow(IXLWorksheet ws, int row, int index, QuotationLineDto line)
    {
        ws.Cell(row, 1).SetValue(index);
        ws.Cell(row, 2).SetValue(FormatItemDescription(line));

        if (line.Density.HasValue)
            ws.Cell(row, 3).SetValue((double)line.Density.Value);
        else
            ws.Cell(row, 3).Clear(XLClearOptions.Contents);

        ws.Cell(row, 4).SetValue((double)line.Quantity);

        if (line.SheetCount.HasValue)
            ws.Cell(row, 5).SetValue((double)line.SheetCount.Value);
        else
            ws.Cell(row, 5).Clear(XLClearOptions.Contents);

        ws.Cell(row, 6).SetValue((double)line.UnitPrice);
        ws.Cell(row, 7).SetValue((double)line.LineTotal);
    }

    private static void CopyRowStyle(IXLWorksheet ws, int sourceRow, int targetRow)
    {
        for (int col = 1; col <= 7; col++)
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
            .Where(l => !IsShippingLine(l))
            .Select(l => l.ProductName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return $"Hàng hóa cung cấp: {string.Join(", ", names)}";
    }

    private static bool IsShippingLine(QuotationLineDto line)
    {
        var name = line.ProductName ?? string.Empty;
        return ContainsIgnoreAccent(name, "vận chuyển") || ContainsIgnoreAccent(name, "van chuyen");
    }

    private static bool ContainsIgnoreAccent(string source, string value) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            source, value,
            CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0;

    private static string FormatItemDescription(QuotationLineDto line)
    {
        if (line.Length.HasValue && line.Width.HasValue && line.Thickness.HasValue)
            return $"KT: {line.Length}*{line.Width}*{line.Thickness}mm";
        return string.IsNullOrWhiteSpace(line.Specification) ? line.ProductName : line.Specification!;
    }
}
