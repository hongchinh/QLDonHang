namespace OrderMgmt.Application.Reports.SalesRevenue.Models;

public class SalesRevenueReportRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? SaleUserId { get; set; }
}

public class SalesRevenueReportItem
{
    public Guid SaleUserId { get; set; }
    public string SaleName { get; set; } = default!;
    public bool IsSaleDeleted { get; set; }
    public int QuotationCount { get; set; }
    public decimal TotalRevenueGross { get; set; }
    public decimal TotalRevenueNet { get; set; }
}

public class SalesRevenueReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<SalesRevenueReportItem> Items { get; set; } = new();
    public int TotalQuotationCount { get; set; }
    public decimal GrandTotalGross { get; set; }
    public decimal GrandTotalNet { get; set; }
}
