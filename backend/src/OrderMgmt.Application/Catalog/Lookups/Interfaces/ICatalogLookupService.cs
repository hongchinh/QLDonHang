using OrderMgmt.Application.Catalog.Lookups.Models;

namespace OrderMgmt.Application.Catalog.Lookups.Interfaces;

public interface ICatalogLookupService
{
    Task<IReadOnlyList<LookupItemDto>> ListProductGroupsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LookupItemDto>> ListUnitsAsync(CancellationToken ct = default);
}
