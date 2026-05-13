using OrderMgmt.Domain.Common;

namespace OrderMgmt.Domain.Entities.Catalog;

public class Unit : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
