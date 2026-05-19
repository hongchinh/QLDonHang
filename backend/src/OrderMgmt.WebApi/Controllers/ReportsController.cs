using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Models;
using OrderMgmt.Application.Reports.VehicleRevenue.Interfaces;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class ReportsController : ApiControllerBase
{
    private readonly ISalesRevenueReportService _salesRevenue;
    private readonly IVehicleRevenueReportService _vehicleRevenue;
    private readonly IValidator<SalesRevenueReportRequest> _salesRevenueValidator;
    private readonly IValidator<VehicleRevenueReportRequest> _vehicleRevenueValidator;

    public ReportsController(
        ISalesRevenueReportService salesRevenue,
        IVehicleRevenueReportService vehicleRevenue,
        IValidator<SalesRevenueReportRequest> salesRevenueValidator,
        IValidator<VehicleRevenueReportRequest> vehicleRevenueValidator)
    {
        _salesRevenue = salesRevenue;
        _vehicleRevenue = vehicleRevenue;
        _salesRevenueValidator = salesRevenueValidator;
        _vehicleRevenueValidator = vehicleRevenueValidator;
    }

    [HttpGet("sales-revenue")]
    [HasPermission(Permissions.Reports.Revenue)]
    public async Task<ActionResult<ApiResponse<SalesRevenueReportDto>>> SalesRevenue(
        [FromQuery] SalesRevenueReportRequest request, CancellationToken ct)
    {
        await _salesRevenueValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _salesRevenue.GetAsync(request, ct));
    }

    [HttpGet("vehicle-revenue")]
    [HasPermission(Permissions.Reports.Revenue)]
    public async Task<ActionResult<ApiResponse<VehicleRevenueReportDto>>> VehicleRevenue(
        [FromQuery] VehicleRevenueReportRequest request, CancellationToken ct)
    {
        await _vehicleRevenueValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _vehicleRevenue.GetAsync(request, ct));
    }
}
