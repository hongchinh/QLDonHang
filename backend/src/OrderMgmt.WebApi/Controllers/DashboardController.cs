using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    private readonly IQuotationDashboardService _service;

    public DashboardController(IQuotationDashboardService service)
    {
        _service = service;
    }

    [HttpGet("quotation-stats")]
    [HasPermission(Permissions.Quotations.View)]
    [ProducesResponseType(typeof(ApiResponse<QuotationStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QuotationStatsDto>>> Stats(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => Success(await _service.GetStatsAsync(from, to, ct));
}
