using System.Globalization;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrderMgmt.Infrastructure.Pdf;

public class QuotationPdfRenderer : IQuotationPdfRenderer
{
    private static readonly CultureInfo Vn = new("vi-VN");
    private const string FontFamily = "Roboto";

    public byte[] Render(QuotationDto q)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(2, Unit.Centimetre);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("CÔNG TY").Bold().FontSize(12);
                            left.Item().Text("Địa chỉ công ty").FontSize(9);
                            left.Item().Text("MST: ...   ĐT: ...").FontSize(9);
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().AlignRight().Text($"Số: {q.Code}").Bold();
                            right.Item().AlignRight().Text(FormatDateMeta(q.QuotationDate));
                        });
                    });

                    col.Item().PaddingTop(10).AlignCenter().Text("BẢNG BÁO GIÁ HÀNG HÓA")
                        .Bold().FontSize(18);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Element(c => CustomerBlock(c, q));

                    if (HasDelivery(q))
                        col.Item().PaddingTop(8).Element(c => DeliveryBlock(c, q));

                    col.Item().PaddingTop(10).Element(c => LineItemsTable(c, q));

                    col.Item().PaddingTop(10).AlignRight().Element(c => TotalsBox(c, q));

                    col.Item().PaddingTop(20).Element(c => SignatureBlock(c, q));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return bytes;
    }

    private static void CustomerBlock(IContainer c, QuotationDto q)
    {
        c.Column(col =>
        {
            col.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold();
            col.Item().Text(t =>
            {
                t.Span("Đơn vị: ").SemiBold();
                t.Span(q.CustomerName);
            });
            if (!string.IsNullOrWhiteSpace(q.CustomerTaxCode))
                col.Item().Text(t => { t.Span("MST: ").SemiBold(); t.Span(q.CustomerTaxCode!); });
            if (!string.IsNullOrWhiteSpace(q.CustomerAddress))
                col.Item().Text(t => { t.Span("Địa chỉ: ").SemiBold(); t.Span(q.CustomerAddress!); });
            if (!string.IsNullOrWhiteSpace(q.ContactPerson))
                col.Item().Text(t => { t.Span("Người liên hệ: ").SemiBold(); t.Span(q.ContactPerson!); });
            if (!string.IsNullOrWhiteSpace(q.ContactPhone))
                col.Item().Text(t => { t.Span("Điện thoại: ").SemiBold(); t.Span(q.ContactPhone!); });
        });
    }

    private static bool HasDelivery(QuotationDto q) =>
        !string.IsNullOrWhiteSpace(q.DeliveryAddress)
        || !string.IsNullOrWhiteSpace(q.DeliveryRecipient)
        || !string.IsNullOrWhiteSpace(q.DeliveryPhone)
        || q.DeliveryDate.HasValue
        || !string.IsNullOrWhiteSpace(q.DeliveryNote);

    private static void DeliveryBlock(IContainer c, QuotationDto q)
    {
        c.Column(col =>
        {
            col.Item().Text("THÔNG TIN GIAO HÀNG").Bold();
            if (!string.IsNullOrWhiteSpace(q.DeliveryAddress))
                col.Item().Text(t => { t.Span("Địa chỉ giao: ").SemiBold(); t.Span(q.DeliveryAddress!); });
            if (!string.IsNullOrWhiteSpace(q.DeliveryRecipient))
                col.Item().Text(t => { t.Span("Người nhận: ").SemiBold(); t.Span(q.DeliveryRecipient!); });
            if (!string.IsNullOrWhiteSpace(q.DeliveryPhone))
                col.Item().Text(t => { t.Span("Điện thoại: ").SemiBold(); t.Span(q.DeliveryPhone!); });
            if (q.DeliveryDate.HasValue)
                col.Item().Text(t => { t.Span("Ngày giao: ").SemiBold(); t.Span(q.DeliveryDate.Value.ToString("dd/MM/yyyy")); });
            if (!string.IsNullOrWhiteSpace(q.DeliveryNote))
                col.Item().Text(t => { t.Span("Ghi chú: ").SemiBold(); t.Span(q.DeliveryNote!); });
        });
    }

    private static void LineItemsTable(IContainer c, QuotationDto q)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28);    // STT
                cols.ConstantColumn(70);    // Mã
                cols.RelativeColumn(3);     // Tên / quy cách
                cols.ConstantColumn(50);    // ĐVT
                cols.ConstantColumn(55);    // SL
                cols.ConstantColumn(75);    // Đơn giá
                cols.ConstantColumn(85);    // Thành tiền
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("STT");
                header.Cell().Element(HeaderCell).Text("Mã");
                header.Cell().Element(HeaderCell).Text("Tên hàng / quy cách");
                header.Cell().Element(HeaderCell).Text("ĐVT");
                header.Cell().Element(HeaderCell).AlignRight().Text("SL");
                header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
            });

            var idx = 1;
            foreach (var line in q.Lines.OrderBy(l => l.SortOrder))
            {
                table.Cell().Element(BodyCell).Text(idx.ToString(Vn));
                table.Cell().Element(BodyCell).Text(line.ProductCode ?? string.Empty);
                table.Cell().Element(BodyCell).Column(col =>
                {
                    col.Item().Text(line.ProductName).SemiBold();
                    if (!string.IsNullOrWhiteSpace(line.Specification))
                        col.Item().Text(line.Specification!).FontSize(9).Italic();
                });
                table.Cell().Element(BodyCell).Text(line.UnitName);
                table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(line.Quantity));
                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.UnitPrice));
                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.LineTotal));
                idx++;
            }
        });

        static IContainer HeaderCell(IContainer c) => c
            .DefaultTextStyle(t => t.SemiBold())
            .PaddingVertical(4).PaddingHorizontal(3)
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1).BorderColor(Colors.Grey.Medium);

        static IContainer BodyCell(IContainer c) => c
            .PaddingVertical(3).PaddingHorizontal(3)
            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    }

    private static void TotalsBox(IContainer c, QuotationDto q)
    {
        c.Width(260).Column(col =>
        {
            col.Item().Element(d => Row(d, "Cộng tiền hàng", FormatMoney(q.Subtotal)));
            if (q.Discount > 0)
                col.Item().Element(d => Row(d, "Chiết khấu", FormatMoney(q.Discount)));
            if (q.Freight > 0)
                col.Item().Element(d => Row(d, "Cước vận chuyển", FormatMoney(q.Freight)));
            col.Item().Element(d => Row(d, $"Thuế GTGT ({q.TaxRate:0.##}%)", FormatMoney(q.TaxAmount)));
            col.Item().PaddingTop(4).Element(d => Row(d, "Tổng cộng", FormatMoney(q.Total), bold: true, large: true));
        });

        static void Row(IContainer c, string label, string value, bool bold = false, bool large = false)
        {
            c.Row(r =>
            {
                r.RelativeItem().Text(t =>
                {
                    var span = t.Span(label);
                    if (bold) span.Bold();
                    if (large) span.FontSize(12);
                });
                r.RelativeItem().AlignRight().Text(t =>
                {
                    var span = t.Span(value);
                    if (bold) span.Bold();
                    if (large) span.FontSize(12);
                });
            });
        }
    }

    private static void SignatureBlock(IContainer c, QuotationDto q)
    {
        c.Row(row =>
        {
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().Text("Bên mua").Bold();
                col.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9);
                col.Item().Height(50);
            });
            row.RelativeItem().AlignCenter().Column(col =>
            {
                col.Item().Text("Bên bán").Bold();
                col.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9);
                col.Item().Height(50);
            });
        });
    }

    private static string FormatMoney(decimal value) => value.ToString("#,##0", Vn);
    private static string FormatQty(decimal value) => value == Math.Truncate(value)
        ? value.ToString("#,##0", Vn)
        : value.ToString("#,##0.####", Vn);

    private static string FormatDateMeta(DateOnly d) =>
        $"Hà Nội, ngày {d.Day:D2} tháng {d.Month:D2} năm {d.Year}";
}
