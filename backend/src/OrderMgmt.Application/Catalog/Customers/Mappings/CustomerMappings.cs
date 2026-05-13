using Mapster;
using OrderMgmt.Application.Catalog.Customers.Models;
using OrderMgmt.Domain.Entities.Catalog;

namespace OrderMgmt.Application.Catalog.Customers.Mappings;

public class CustomerMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerDto>();
        config.NewConfig<Customer, CustomerListItemDto>();
    }
}
