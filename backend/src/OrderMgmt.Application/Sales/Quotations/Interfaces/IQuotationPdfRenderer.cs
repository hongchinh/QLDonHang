using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationPdfRenderer
{
    byte[] Render(QuotationDto quotation);
}
