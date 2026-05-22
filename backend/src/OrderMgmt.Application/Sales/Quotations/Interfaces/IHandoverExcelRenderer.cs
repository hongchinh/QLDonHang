using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IHandoverExcelRenderer
{
    Task<byte[]> RenderAsync(
        QuotationDto quotation,
        string templatePath,
        bool withPrice,
        CancellationToken ct = default);
}
