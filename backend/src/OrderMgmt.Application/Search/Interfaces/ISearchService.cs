using OrderMgmt.Application.Search.Models;

namespace OrderMgmt.Application.Search.Interfaces;

public interface ISearchService
{
    Task<GlobalSearchResultDto> GlobalAsync(string keyword, CancellationToken ct = default);
}
