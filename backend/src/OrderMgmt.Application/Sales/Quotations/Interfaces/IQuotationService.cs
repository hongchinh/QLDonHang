using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationService
{
    Task<PagedResult<QuotationListItemDto>> ListAsync(QuotationListRequest request, CancellationToken ct = default);
    Task<QuotationDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> CreateAsync(UpsertQuotationRequest request, CancellationToken ct = default);
    Task<QuotationDto> UpdateAsync(Guid id, UpsertQuotationRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action, CancellationToken ct = default);
    Task<(byte[] Excel, string FileName)> RenderExcelAsync(Guid id, CancellationToken ct = default);
    Task<(byte[] Pdf, string FileName)> RenderPdfAsync(Guid id, CancellationToken ct = default);
}
