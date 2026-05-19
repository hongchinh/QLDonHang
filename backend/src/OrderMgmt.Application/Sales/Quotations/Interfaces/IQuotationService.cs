using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationService
{
    Task<QuotationListResult> ListAsync(QuotationListRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<QuotationOwnerOptionDto>> ListOwnersAsync(bool includeDeleted, CancellationToken ct = default);
    Task<IReadOnlyList<QuotationActivityDto>> ListActivitiesAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> CreateAsync(UpsertQuotationRequest request, CancellationToken ct = default);
    Task<QuotationDto> UpdateAsync(Guid id, UpsertQuotationRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action, CancellationToken ct = default);
    Task<QuotationDto> TransferOwnerAsync(Guid id, TransferOwnerRequest request, CancellationToken ct = default);
    Task<QuotationDto> CloneAsync(Guid id, CancellationToken ct = default);
    Task<(byte[] Excel, string FileName)> RenderExcelAsync(Guid id, CancellationToken ct = default);
    Task<(byte[] Pdf, string FileName)> RenderPdfAsync(Guid id, CancellationToken ct = default);
}
