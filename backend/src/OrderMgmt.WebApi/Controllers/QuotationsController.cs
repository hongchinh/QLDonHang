using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Sales.Quotations.Interfaces;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class QuotationsController : ApiControllerBase
{
    private readonly IQuotationService _quotations;
    private readonly IValidator<UpsertQuotationRequest> _upsertValidator;
    private readonly IValidator<QuotationListRequest> _listValidator;
    private readonly IValidator<TransitionQuotationRequest> _transitionValidator;

    public QuotationsController(
        IQuotationService quotations,
        IValidator<UpsertQuotationRequest> upsertValidator,
        IValidator<QuotationListRequest> listValidator,
        IValidator<TransitionQuotationRequest> transitionValidator)
    {
        _quotations = quotations;
        _upsertValidator = upsertValidator;
        _listValidator = listValidator;
        _transitionValidator = transitionValidator;
    }

    [HttpGet]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<QuotationListItemDto>>>> List(
        [FromQuery] QuotationListRequest request, CancellationToken ct)
    {
        await _listValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _quotations.ListAsync(request, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Get(Guid id, CancellationToken ct)
        => Success(await _quotations.GetAsync(id, ct));

    [HttpPost]
    [HasPermission(Permissions.Quotations.Create)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Create(
        [FromBody] UpsertQuotationRequest request, CancellationToken ct)
    {
        await _upsertValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _quotations.CreateAsync(request, ct));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Quotations.Update)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Update(
        Guid id, [FromBody] UpsertQuotationRequest request, CancellationToken ct)
    {
        await _upsertValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _quotations.UpdateAsync(id, request, ct));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Quotations.Delete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _quotations.DeleteAsync(id, ct);
        return Success();
    }

    [HttpPost("{id:guid}/transition")]
    [HasPermission(Permissions.Quotations.Update)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Transition(
        Guid id, [FromBody] TransitionQuotationRequest request, CancellationToken ct)
    {
        await _transitionValidator.ValidateAndThrowAsync(request, ct);
        return Success(await _quotations.TransitionAsync(id, request.Action, ct));
    }

    // Returns raw PDF bytes (not wrapped in ApiResponse) so browsers can save it directly.
    [HttpGet("{id:guid}/pdf")]
    [HasPermission(Permissions.Quotations.Print)]
    public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
    {
        var (bytes, fileName) = await _quotations.RenderPdfAsync(id, ct);
        return File(bytes, "application/pdf", fileName);
    }
}
