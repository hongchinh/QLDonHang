using OrderMgmt.Domain.Common;

namespace OrderMgmt.Domain.Entities.Catalog;

public class CustomerAddress : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public string? Label { get; set; }
    public string Address { get; set; } = default!;
    public string? DefaultRecipient { get; set; }
    public string? RecipientPhone { get; set; }
    public string? Note { get; set; }
    public bool IsDefault { get; set; }
}
