using OrderMgmt.Application.Catalog.Customers.Models;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.Customers.Interfaces;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> ListAsync(CustomerListRequest request, CancellationToken ct = default);
    Task<List<CustomerSearchItemDto>> SearchAsync(CustomerSearchRequest request, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
