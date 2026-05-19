using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Catalog;
using OrderMgmt.Domain.Entities.Identity;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Domain.Entities.Sales;

public class Quotation : BaseEntity
{
    public string Code { get; set; } = default!;
    public DateOnly QuotationDate { get; set; }

    public Guid OwnerUserId { get; set; }
    public User? Owner { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string CustomerName { get; set; } = default!;
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }

    public string? DeliveryAddress { get; set; }
    public string? DeliveryRecipient { get; set; }
    public string? DeliveryPhone { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public string? DeliveryNote { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Freight { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? InternalNote { get; set; }

    public ICollection<QuotationLine> Lines { get; set; } = new List<QuotationLine>();
    public ICollection<QuotationActivity> Activities { get; set; } = new List<QuotationActivity>();
}
