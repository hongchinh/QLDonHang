using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Admin.Interfaces;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class AdminUsersController : ApiControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service)
    {
        _service = service;
    }

    [HttpGet("/api/admin/users")]
    [HasPermission(Permissions.UserSettings.Manage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserListItemDto>>>> List(
        [FromQuery] AdminUserListQuery query,
        CancellationToken ct)
        => Success(await _service.ListAsync(query, ct));
}
