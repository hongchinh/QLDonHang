using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.ProductGroups.Interfaces;

public interface IProductGroupService
{
    Task<PagedResult<ProductGroupListItemDto>> ListAsync(ProductGroupListRequest request, CancellationToken ct = default);
    Task<ProductGroupDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProductGroupDto> CreateAsync(CreateProductGroupRequest request, CancellationToken ct = default);
    Task<ProductGroupDto> UpdateAsync(Guid id, UpdateProductGroupRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
