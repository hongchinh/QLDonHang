namespace OrderMgmt.Domain.Entities.Sales;

public class QuotationSystemSettings
{
    public int Id { get; set; }
    // "QuotationDate" | "ConfirmedAt" | "AccountingConfirmedAt"
    public string RevenueReportingDateField { get; set; } = "QuotationDate";
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
