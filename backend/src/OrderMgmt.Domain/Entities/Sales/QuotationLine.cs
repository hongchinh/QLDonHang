using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Catalog;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Domain.Entities.Sales;

public class QuotationLine : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public int SortOrder { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = default!;
    public string? Specification { get; set; }
    public string UnitName { get; set; } = default!;
    public PricingMode PricingMode { get; set; } = PricingMode.PerUnit;

    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? Thickness { get; set; }
    public decimal? Density { get; set; }
    public decimal? SheetCount { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? LineCost { get; set; }
    public decimal? LineProfit { get; set; }

    public string? Note { get; set; }
}
