using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Admin.Interfaces;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class AdminRolesController : ApiControllerBase
{
    private readonly IAdminRoleService _service;

    public AdminRolesController(IAdminRoleService service)
    {
        _service = service;
    }

    [HttpGet("/api/admin/permissions")]
    [HasPermission(Permissions.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> ListPermissions(
        CancellationToken ct)
        => Success(await _service.ListPermissionsAsync(ct));

    [HttpGet("/api/admin/roles")]
    [HasPermission(Permissions.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleListItemDto>>>> List(
        CancellationToken ct)
        => Success(await _service.ListAsync(ct));

    [HttpGet("/api/admin/roles/{id:guid}")]
    [HasPermission(Permissions.Roles.View)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Get(Guid id, CancellationToken ct)
        => Success(await _service.GetAsync(id, ct));

    [HttpPost("/api/admin/roles")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Create(
        [FromBody] CreateRoleRequest req,
        CancellationToken ct)
        => Success(await _service.CreateAsync(req, ct));

    [HttpPut("/api/admin/roles/{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest req,
        CancellationToken ct)
        => Success(await _service.UpdateAsync(id, req, ct));

    [HttpPut("/api/admin/roles/{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse<RoleDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest req,
        CancellationToken ct)
        => Success(await _service.UpdatePermissionsAsync(id, req, ct));

    [HttpDelete("/api/admin/roles/{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return Success();
    }
}
