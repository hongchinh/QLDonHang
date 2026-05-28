using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

[Route("api/product-groups")]
public class ProductGroupsController : ApiControllerBase
{
    private readonly IProductGroupService _service;
    private readonly IValidator<CreateProductGroupRequest> _createValidator;
    private readonly IValidator<UpdateProductGroupRequest> _updateValidator;
    private readonly IValidator<ProductGroupListRequest> _listValidator;

    public ProductGroupsController(
        IProductGroupService service,
        IValidator<CreateProductGroupRequest> createValidator,
        IValidator<UpdateProductGroupRequest> updateValidator,
        IValidator<ProductGroupListRequest> listValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _listValidator = listValidator;
    }

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductGroupListItemDto>>>> List(
        [FromQuery] ProductGroupListRequest request, CancellationToken ct)
    {
        await _listValidator.ValidateAndThrowAsync(request, ct);
        var result = await _service.ListAsync(request, ct);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _service.GetAsync(id, ct);
        return Success(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> Create(
        [FromBody] CreateProductGroupRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _service.CreateAsync(request, ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Products.Update)]
    public async Task<ActionResult<ApiResponse<ProductGroupDto>>> Update(
        Guid id, [FromBody] UpdateProductGroupRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var result = await _service.UpdateAsync(id, request, ct);
        return Success(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Products.Delete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return Success();
    }
}
