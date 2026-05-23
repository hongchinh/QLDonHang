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

public class SalesRevenueLineItemsRequest
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class SalesRevenueLineItemDto
{
    public Guid QuotationId { get; set; }
    public string QuotationCode { get; set; } = default!;
    public DateOnly QuotationDate { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string CustomerName { get; set; } = default!;
    public string? CustomerAddress { get; set; }
    public string? ContactPhone { get; set; }
    public decimal Freight { get; set; }
    public bool IsFirstLineOfQuotation { get; set; }

    public string ProductName { get; set; } = default!;
    public string? Specification { get; set; }
    public string UnitName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // Null when caller lacks quotations.view_cost
    public decimal? UnitCost { get; set; }
    public decimal? LineCost { get; set; }
    public decimal? LineProfit { get; set; }
}
