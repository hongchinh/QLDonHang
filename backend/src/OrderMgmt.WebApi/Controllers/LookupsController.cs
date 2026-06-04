using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Catalog.Lookups.Interfaces;
using OrderMgmt.Application.Catalog.Lookups.Models;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/lookups")]
public class LookupsController : ApiControllerBase
{
    private readonly ICatalogLookupService _lookups;

    public LookupsController(ICatalogLookupService lookups)
    {
        _lookups = lookups;
    }

    [HttpGet("product-groups")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> ProductGroups(CancellationToken ct)
    {
        var result = await _lookups.ListProductGroupsAsync(ct);
        return Success(result);
    }

    [HttpGet("units")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> Units(CancellationToken ct)
    {
        var result = await _lookups.ListUnitsAsync(ct);
        return Success(result);
    }

    [HttpPost("units")]
    public async Task<ActionResult<ApiResponse<LookupItemDto>>> GetOrCreateUnit(
        [FromBody] GetOrCreateUnitRequest request,
        CancellationToken ct)
    {
        var result = await _lookups.GetOrCreateUnitAsync(request.Name, ct);
        return Success(result);
    }
}
