namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationSpreadsheetPdfConverter
{
    Task<byte[]> ConvertAsync(byte[] xlsxBytes, CancellationToken ct = default);
}
