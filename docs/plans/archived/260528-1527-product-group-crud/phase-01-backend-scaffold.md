# Phase 01 — Backend: Models, Service Stub & DI

**Status:** [ ] pending
**Complexity:** S

## Objective

Create all Application-layer types (DTOs, request models, interface, validators) and a
stubbed service + controller that compile cleanly. DI is wired so Phase 02 can immediately
write runnable integration tests.

## Files

- `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Models/ProductGroupDto.cs` *(new)*
- `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Interfaces/IProductGroupService.cs` *(new)*
- `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Validators/ProductGroupValidators.cs` *(new)*
- `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Services/ProductGroupService.cs` *(new — stub)*
- `backend/src/OrderMgmt.WebApi/Controllers/ProductGroupsController.cs` *(new — stub)*
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` *(edit — add DI registration)*

## Tasks

### Task 1 — Create DTOs and request models

Create `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Models/ProductGroupDto.cs`:

```csharp
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.ProductGroups.Models;

public class ProductGroupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ProductGroupListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
}

public class CreateProductGroupRequest
{
    public string? Code { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProductGroupRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ProductGroupListRequest : PageRequest
{
    public bool? IsActive { get; set; }
}
```

### Task 2 — Create service interface

Create `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Interfaces/IProductGroupService.cs`:

```csharp
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.ProductGroups.Interfaces;

public interface IProductGroupService
{
    Task<PagedResult<ProductGroupListItemDto>> ListAsync(ProductGroupListRequest request, CancellationToken ct = default);
    Task<ProductGroupDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProductGroupDto> CreateAsync(CreateProductGroupRequest request, CancellationToken ct = default);
    Task<ProductGroupDto> UpdateAsync(Guid id, UpdateProductGroupRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

### Task 3 — Create validators

Create `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Validators/ProductGroupValidators.cs`:

```csharp
using FluentValidation;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Validators;

namespace OrderMgmt.Application.Catalog.ProductGroups.Validators;

public class CreateProductGroupRequestValidator : AbstractValidator<CreateProductGroupRequest>
{
    public CreateProductGroupRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductGroupRequestValidator : AbstractValidator<UpdateProductGroupRequest>
{
    public UpdateProductGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class ProductGroupListRequestValidator : PageRequestValidator<ProductGroupListRequest>
{
}
```

### Task 4 — Create service stub

Create `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Services/ProductGroupService.cs`
with stub implementations that throw `NotImplementedException` — enough to compile:

```csharp
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Catalog.ProductGroups.Services;

public class ProductGroupService : IProductGroupService
{
    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;

    public ProductGroupService(IAppDbContext db, IDateTime clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public Task<PagedResult<ProductGroupListItemDto>> ListAsync(ProductGroupListRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ProductGroupDto> GetAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ProductGroupDto> CreateAsync(CreateProductGroupRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<ProductGroupDto> UpdateAsync(Guid id, UpdateProductGroupRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
}
```

### Task 5 — Create controller stub

Create `backend/src/OrderMgmt.WebApi/Controllers/ProductGroupsController.cs`
with stub actions that compile:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Constants;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

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
    public Task<ActionResult<ApiResponse<PagedResult<ProductGroupListItemDto>>>> List(
        [FromQuery] ProductGroupListRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Products.View)]
    public Task<ActionResult<ApiResponse<ProductGroupDto>>> Get(Guid id, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPost]
    [HasPermission(Permissions.Products.Create)]
    public Task<ActionResult<ApiResponse<ProductGroupDto>>> Create(
        [FromBody] CreateProductGroupRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Products.Update)]
    public Task<ActionResult<ApiResponse<ProductGroupDto>>> Update(
        Guid id, [FromBody] UpdateProductGroupRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Products.Delete)]
    public Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken ct)
        => throw new NotImplementedException();
}
```

### Task 6 — Register service in DI

Edit `backend/src/OrderMgmt.Application/DependencyInjection.cs`.

Add the following using at the top:
```csharp
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Services;
```

Add the following line inside `AddApplication()`, after the `IProductService` registration:
```csharp
services.AddScoped<IProductGroupService, ProductGroupService>();
```

### Task 7 — Verify build

```bash
cd backend
dotnet build src/OrderMgmt.Application src/OrderMgmt.WebApi
```

Expected: zero errors. Warnings about `NotImplementedException` are acceptable.

## Verification

```bash
cd backend && dotnet build src/OrderMgmt.Application src/OrderMgmt.WebApi
```

Expected output: `Build succeeded. 0 Error(s)`

## Exit Criteria

- All new files exist at the specified paths.
- `dotnet build` on the two projects exits with 0 errors.
- No changes to any migration or domain entity.
