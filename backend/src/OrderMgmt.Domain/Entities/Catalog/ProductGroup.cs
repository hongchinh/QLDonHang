using OrderMgmt.Domain.Common;

namespace OrderMgmt.Domain.Entities.Catalog;

public class ProductGroup : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
