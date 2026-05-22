using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

[Route("api/settings/quotation")]
public class QuotationSettingsController : ApiControllerBase
{
    private readonly IQuotationSystemSettingsService _settings;

    public QuotationSettingsController(IQuotationSystemSettingsService settings)
        => _settings = settings;

    [HttpGet]
    [HasPermission(Permissions.System.ManageSettings)]
    public async Task<ActionResult<ApiResponse<QuotationSystemSettingsDto>>> Get(CancellationToken ct)
        => Success(await _settings.GetAsync(ct));

    [HttpPut]
    [HasPermission(Permissions.System.ManageSettings)]
    public async Task<ActionResult<ApiResponse<QuotationSystemSettingsDto>>> Update(
        [FromBody] UpdateQuotationSystemSettingsRequest request, CancellationToken ct)
        => Success(await _settings.UpdateAsync(request, ct));
}
