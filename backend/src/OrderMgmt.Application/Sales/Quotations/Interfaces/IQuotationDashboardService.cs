using OrderMgmt.Application.Sales.Quotations.Models;

namespace OrderMgmt.Application.Sales.Quotations.Interfaces;

public interface IQuotationDashboardService
{
    Task<QuotationStatsDto> GetStatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
