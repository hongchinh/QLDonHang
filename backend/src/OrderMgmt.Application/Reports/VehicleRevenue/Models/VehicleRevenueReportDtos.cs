namespace OrderMgmt.Application.Reports.VehicleRevenue.Models;

public class VehicleRevenueReportRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Months { get; set; } = 6;
    // TopVehicles đã bỏ — không còn giới hạn N xe trên chart
}

public class VehicleRevenueReportItem
{
    public string VehicleNumber { get; set; } = default!;
    public int CompanyQuotationCount { get; set; }
    public int ExternalQuotationCount { get; set; }
    public decimal CompanyVehicleRevenue { get; set; }   // tổng LineTotal > 0 của dòng "cuoc"
    public decimal ExternalVehicleRevenue { get; set; }  // tổng LineTotal < 0 của dòng "cuoc" (âm)
}

public class VehicleRevenueMonthlyPoint
{
    public string Month { get; set; } = default!;        // "yyyy-MM"
    public decimal CompanyTotal { get; set; }
    public decimal ExternalTotal { get; set; }           // âm
}

public class VehicleRevenueReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int Months { get; set; }
    public List<VehicleRevenueReportItem> Items { get; set; } = new();
    public List<VehicleRevenueMonthlyPoint> MonthlySeries { get; set; } = new();
    public decimal GrandTotalCompany { get; set; }
    public decimal GrandTotalExternal { get; set; }      // âm
}
