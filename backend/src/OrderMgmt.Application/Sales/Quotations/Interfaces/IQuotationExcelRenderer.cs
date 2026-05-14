using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationExcelRenderer
{
    Task<byte[]> RenderAsync(QuotationDto quotation, CancellationToken ct = default);
    Task<byte[]> RenderAsync(QuotationDto quotation, string templatePath, CancellationToken ct = default);
}
