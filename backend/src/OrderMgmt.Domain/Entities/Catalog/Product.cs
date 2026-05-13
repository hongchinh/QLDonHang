using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Domain.Entities.Catalog;

public class Product : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public Guid? ProductGroupId { get; set; }
    public ProductGroup? ProductGroup { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Density { get; set; }
    public string? Specification { get; set; }

    public decimal? DefaultPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public decimal? DefaultTaxRate { get; set; }

    public string? Note { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public PricingMode PricingMode { get; set; } = PricingMode.PerUnit;
}
