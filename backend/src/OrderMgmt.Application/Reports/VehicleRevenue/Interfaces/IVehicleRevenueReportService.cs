using OrderMgmt.Application.Reports.VehicleRevenue.Models;

namespace OrderMgmt.Application.Reports.VehicleRevenue.Interfaces;

public interface IVehicleRevenueReportService
{
    Task<VehicleRevenueReportDto> GetAsync(VehicleRevenueReportRequest request, CancellationToken ct = default);
}
