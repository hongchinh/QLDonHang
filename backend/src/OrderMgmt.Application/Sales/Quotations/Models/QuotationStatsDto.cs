namespace OrderMgmt.Application.Sales.Quotations.Models;

public class QuotationStatsDto
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int SentCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int AccountingConfirmedCount { get; set; }
    public decimal AccountingConfirmedRevenue { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}
