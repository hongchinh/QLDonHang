using OrderMgmt.Application.Catalog.Products.Models;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.Products.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> ListAsync(ProductListRequest request, CancellationToken ct = default);
    Task<ProductDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSuggestionDto>> SearchAsync(string? query, int take, CancellationToken ct = default);
}
