using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Search.Interfaces;
using OrderMgmt.Application.Search.Models;

namespace OrderMgmt.WebApi.Controllers;

[Authorize]
[Route("api/search")]
public class SearchController : ApiControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search)
    {
        _search = search;
    }

    [HttpGet("global")]
    public async Task<ActionResult<ApiResponse<GlobalSearchResultDto>>> Global(
        [FromQuery] string q,
        CancellationToken ct)
        => Success(await _search.GlobalAsync(q ?? string.Empty, ct));
}
