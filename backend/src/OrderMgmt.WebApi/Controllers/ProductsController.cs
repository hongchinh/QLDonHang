using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Catalog.Products.Interfaces;
using OrderMgmt.Application.Catalog.Products.Models;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class ProductsController : ApiControllerBase
{
    private readonly IProductService _products;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IValidator<ProductListRequest> _listValidator;

    public ProductsController(
        IProductService products,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        IValidator<ProductListRequest> listValidator)
    {
        _products = products;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _listValidator = listValidator;
    }

    [HttpGet]
    [HasPermission(Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductListItemDto>>>> List(
        [FromQuery] ProductListRequest request, CancellationToken ct)
    {
        await _listValidator.ValidateAndThrowAsync(request, ct);
        var result = await _products.ListAsync(request, ct);
        return Success(result);
    }

    [HttpGet("search")]
    [HasPermission(Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductSuggestionDto>>>> Search(
        [FromQuery] string? q, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        var result = await _products.SearchAsync(q, take, ct);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Products.View)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Get(Guid id, CancellationToken ct)
    {
        var result = await _products.GetAsync(id, ct);
        return Success(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
        [FromBody] CreateProductRequest request, CancellationToken ct)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);
        var result = await _products.CreateAsync(request, ct);
        return Success(result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Products.Update)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
        Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        await _updateValidator.ValidateAndThrowAsync(request, ct);
        var result = await _products.UpdateAsync(id, request, ct);
        return Success(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Products.Delete)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return Success();
    }
}
