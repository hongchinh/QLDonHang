using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationSystemSettingsService
{
    Task<QuotationSystemSettingsDto> GetAsync(CancellationToken ct = default);
    Task<QuotationSystemSettingsDto> UpdateAsync(UpdateQuotationSystemSettingsRequest request, CancellationToken ct = default);
}
