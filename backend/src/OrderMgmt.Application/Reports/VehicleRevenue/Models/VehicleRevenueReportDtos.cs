namespace OrderMgmt.Application.Reports.VehicleRevenue.Models;

public class VehicleRevenueReportRequest
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Months { get; set; } = 6;
    public int TopVehicles { get; set; } = 5;
}

public class VehicleRevenueReportItem
{
    public string VehicleNumber { get; set; } = default!;
    public int QuotationCount { get; set; }
    public decimal TotalRevenueGross { get; set; }
    public decimal TotalRevenueNet { get; set; }
}

public class VehicleRevenueMonthlyValue
{
    public string VehicleNumber { get; set; } = default!;
    public decimal TotalRevenueGross { get; set; }
}

public class VehicleRevenueMonthlyPoint
{
    public string Month { get; set; } = default!;
    public List<VehicleRevenueMonthlyValue> Values { get; set; } = new();
}

public class VehicleRevenueReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int Months { get; set; }
    public int TopVehicles { get; set; }
    public List<VehicleRevenueReportItem> Items { get; set; } = new();
    public List<string> ChartVehicles { get; set; } = new();
    public List<VehicleRevenueMonthlyPoint> MonthlySeries { get; set; } = new();
    public int TotalQuotationCount { get; set; }
    public decimal GrandTotalGross { get; set; }
    public decimal GrandTotalNet { get; set; }
}
