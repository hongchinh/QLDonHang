using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Reports.SalesRevenue.Interfaces;
using OrderMgmt.Application.Reports.SalesRevenue.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class ReportsController : ApiControllerBase
{
    private readonly ISalesRevenueReportService _salesRevenue;
    private readonly IValidator<SalesRevenueReportRequest> _validator;

    public ReportsController(
        ISalesRevenueReportService salesRevenue,
        IValidator<SalesRevenueReportRequest> validator)
    {
        _salesRevenue = salesRevenue;
        _validator = validator;
    }

    [HttpGet("sales-revenue")]
    [HasPermission(Permissions.Reports.Revenue)]
    public async Task<ActionResult<ApiResponse<SalesRevenueReportDto>>> SalesRevenue(
        [FromQuery] SalesRevenueReportRequest request, CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        return Success(await _salesRevenue.GetAsync(request, ct));
    }
}
