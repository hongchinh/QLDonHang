using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.UserSettings.Interfaces;
using OrderMgmt.Application.Identity.UserSettings.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class AdminUserSettingsController : ApiControllerBase
{
    private readonly IUserQuotationSettingsService _service;
    private readonly IValidator<UpdateLockAtRequest> _validator;

    public AdminUserSettingsController(
        IUserQuotationSettingsService service,
        IValidator<UpdateLockAtRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpGet("/api/admin/user-settings/{userId:guid}")]
    [HasPermission(Permissions.UserSettings.Manage)]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> Get(Guid userId, CancellationToken ct)
        => Success(await _service.GetForUserAsync(userId, ct));

    [HttpPut("/api/admin/user-settings/{userId:guid}/lock-at")]
    [HasPermission(Permissions.UserSettings.Manage)]
    [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> SetLockAt(
        Guid userId,
        [FromBody] UpdateLockAtRequest request,
        CancellationToken ct)
    {
        await _validator.ValidateAndThrowAsync(request, ct);
        return Success(await _service.SetLockAtAsync(userId, request, ct));
    }

    [HttpPost("/api/admin/users/{userId:guid}/transfer-quotations")]
    [HasPermission(Permissions.Quotations.TransferAny)]
    [ProducesResponseType(typeof(ApiResponse<BulkTransferResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BulkTransferResult>>> BulkTransfer(
        Guid userId,
        [FromBody] BulkTransferRequest request,
        [FromServices] IQuotationBulkTransferService bulkService,
        [FromServices] IValidator<BulkTransferRequest> bulkValidator,
        CancellationToken ct)
    {
        await bulkValidator.ValidateAndThrowAsync(request, ct);
        return Success(await bulkService.TransferAllAsync(userId, request, ct));
    }
}
