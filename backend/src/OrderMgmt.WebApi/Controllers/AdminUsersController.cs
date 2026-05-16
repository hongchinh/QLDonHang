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
    [HasPermission(Permissions.Users.View)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserListItemDto>>>> List(
        [FromQuery] AdminUserListQuery query,
        CancellationToken ct)
        => Success(await _service.ListAsync(query, ct));

    [HttpGet("/api/admin/users/{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Get(Guid id, CancellationToken ct)
        => Success(await _service.GetAsync(id, ct));

    [HttpPost("/api/admin/users")]
    [HasPermission(Permissions.Users.Create)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Create(
        [FromBody] CreateUserRequest req,
        CancellationToken ct)
        => Success(await _service.CreateAsync(req, ct));

    [HttpPut("/api/admin/users/{id:guid}")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest req,
        CancellationToken ct)
        => Success(await _service.UpdateAsync(id, req, ct));

    [HttpPost("/api/admin/users/{id:guid}/reset-password")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest req,
        CancellationToken ct)
    {
        await _service.ResetPasswordAsync(id, req, ct);
        return Success();
    }

    [HttpPost("/api/admin/users/{id:guid}/status")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> SetStatus(
        Guid id,
        [FromBody] SetUserStatusRequest req,
        CancellationToken ct)
    {
        await _service.SetStatusAsync(id, req, ct);
        return Success();
    }

    [HttpDelete("/api/admin/users/{id:guid}")]
    [HasPermission(Permissions.Users.Delete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _service.SoftDeleteAsync(id, ct);
        return Success();
    }
}
