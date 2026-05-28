# Phase 02 — Backend: Integration Tests → Full Implementation

**Status:** [ ] pending
**Complexity:** M

## Objective

Write integration tests first (they will fail with `NotImplementedException`), then implement
the service and controller fully until all tests pass.

## Files

- `backend/tests/OrderMgmt.IntegrationTests/ProductGroupCrudTests.cs` *(new)*
- `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Services/ProductGroupService.cs` *(replace stub)*
- `backend/src/OrderMgmt.WebApi/Controllers/ProductGroupsController.cs` *(replace stub)*

## Tasks

### Task 1 — Write integration tests (expect fail)

Create `backend/tests/OrderMgmt.IntegrationTests/ProductGroupCrudTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Infrastructure.Persistence;
using OrderMgmt.IntegrationTests.Fixtures;
using Xunit;

namespace OrderMgmt.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public class ProductGroupCrudTests : IAsyncLifetime
{
    private readonly PostgresFixture _pg;
    private WebAppFactory _factory = default!;
    private HttpClient _client = default!;
    private Guid _existingGroupId;

    public ProductGroupCrudTests(PostgresFixture pg) => _pg = pg;

    public async Task InitializeAsync()
    {
        _factory = new WebAppFactory(_pg.ConnectionString);
        await ((IAsyncLifetime)_factory).InitializeAsync();
        _client = _factory.CreateClient();
        await AuthenticateAsync();
        await ResolveExistingGroupIdAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await ((IAsyncLifetime)_factory).DisposeAsync();
    }

    private async Task AuthenticateAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Username = "admin", Password = "Admin@123" });
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(TestJson.Options);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body!.Data!.AccessToken);
    }

    private async Task ResolveExistingGroupIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Use any seeded group — EPS is seeded by the existing seed data.
        _existingGroupId = (await db.ProductGroups.FirstAsync(g => g.Code == "EPS")).Id;
    }

    // --- Happy path CRUD ---

    [Fact]
    public async Task Create_returns_dto_with_generated_code()
    {
        var response = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = "Nhóm Test " + Guid.NewGuid().ToString("N")[..6],
            SortOrder = 10,
            IsActive = true,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductGroupDto>>(TestJson.Options);
        body!.Data!.Code.Should().StartWith("NG-");
        body.Data.IsActive.Should().BeTrue();
        body.Data.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task Create_with_explicit_code_stores_that_code()
    {
        var uniqueCode = "TG-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        var response = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Code = uniqueCode,
            Name = "Nhóm mã tường minh",
            IsActive = true,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductGroupDto>>(TestJson.Options);
        body!.Data!.Code.Should().Be(uniqueCode);
    }

    [Fact]
    public async Task Create_duplicate_code_returns_409()
    {
        var uniqueCode = "DUP-" + Guid.NewGuid().ToString("N")[..4].ToUpper();

        await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Code = uniqueCode, Name = "First", IsActive = true,
        });

        var second = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Code = uniqueCode, Name = "Second", IsActive = true,
        });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Get_returns_correct_group()
    {
        var get = await _client.GetFromJsonAsync<ApiResponse<ProductGroupDto>>(
            $"/api/product-groups/{_existingGroupId}", TestJson.Options);
        get!.Data!.Id.Should().Be(_existingGroupId);
        get.Data.Code.Should().Be("EPS");
    }

    [Fact]
    public async Task List_returns_paginated_results()
    {
        var list = await _client.GetFromJsonAsync<ApiResponse<PagedResult<ProductGroupListItemDto>>>(
            "/api/product-groups?page=1&pageSize=20", TestJson.Options);
        list!.Data!.Items.Should().NotBeEmpty();
        list.Data.Page.Should().Be(1);
    }

    [Fact]
    public async Task List_search_filters_by_name_or_code()
    {
        // Create a group with a known unique name fragment.
        var uniqueName = "SearchTarget-" + Guid.NewGuid().ToString("N")[..6];
        await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = uniqueName, IsActive = true,
        });

        var list = await _client.GetFromJsonAsync<ApiResponse<PagedResult<ProductGroupListItemDto>>>(
            $"/api/product-groups?page=1&pageSize=20&search={Uri.EscapeDataString(uniqueName[..10])}",
            TestJson.Options);
        list!.Data!.Items.Should().Contain(i => i.Name == uniqueName);
    }

    [Fact]
    public async Task List_filter_by_isActive_false_excludes_active_groups()
    {
        // Create one inactive group.
        var uniqueName = "Inactive-" + Guid.NewGuid().ToString("N")[..6];
        await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = uniqueName, IsActive = false,
        });

        var list = await _client.GetFromJsonAsync<ApiResponse<PagedResult<ProductGroupListItemDto>>>(
            "/api/product-groups?page=1&pageSize=200&isActive=false", TestJson.Options);
        list!.Data!.Items.Should().Contain(i => i.Name == uniqueName);
        list.Data.Items.Should().OnlyContain(i => !i.IsActive);
    }

    [Fact]
    public async Task Update_changes_name_and_sortOrder()
    {
        var create = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = "Before Update", IsActive = true, SortOrder = 0,
        });
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<ProductGroupDto>>(TestJson.Options);

        var update = await _client.PutAsJsonAsync($"/api/product-groups/{created!.Data!.Id}",
            new UpdateProductGroupRequest
            {
                Name = "After Update",
                SortOrder = 5,
                IsActive = true,
            });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var get = await _client.GetFromJsonAsync<ApiResponse<ProductGroupDto>>(
            $"/api/product-groups/{created.Data.Id}", TestJson.Options);
        get!.Data!.Name.Should().Be("After Update");
        get.Data.SortOrder.Should().Be(5);
        // Code should be unchanged.
        get.Data.Code.Should().Be(created.Data.Code);
    }

    [Fact]
    public async Task Delete_soft_deletes_and_excludes_from_list()
    {
        var create = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = "Will Be Deleted", IsActive = true,
        });
        var created = await create.Content.ReadFromJsonAsync<ApiResponse<ProductGroupDto>>(TestJson.Options);

        var delete = await _client.DeleteAsync($"/api/product-groups/{created!.Data!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await _client.GetFromJsonAsync<ApiResponse<PagedResult<ProductGroupListItemDto>>>(
            "/api/product-groups?page=1&pageSize=200", TestJson.Options);
        list!.Data!.Items.Should().NotContain(i => i.Id == created.Data.Id);
    }

    [Fact]
    public async Task Delete_group_with_products_nullifies_product_group_reference()
    {
        // Create a new group.
        var grpResp = await _client.PostAsJsonAsync("/api/product-groups", new CreateProductGroupRequest
        {
            Name = "Group With Products", IsActive = true,
        });
        var grp = await grpResp.Content.ReadFromJsonAsync<ApiResponse<ProductGroupDto>>(TestJson.Options);

        // Create a product linked to this group.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var anyUnitId = (await db.Units.FirstAsync()).Id;

        await _client.PostAsJsonAsync("/api/products", new
        {
            name = "Test Product for Group Delete",
            productGroupId = grp!.Data!.Id,
            unitId = anyUnitId,
        });

        // Delete the group.
        var delete = await _client.DeleteAsync($"/api/product-groups/{grp.Data.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        // Product's ProductGroupId should now be null in DB.
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.ProductGroupId == grp.Data.Id);
        product.Should().BeNull("ProductGroupId should be nullified by cascade");
    }

    // --- Authorization ---

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/product-groups");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Task 2 — Run tests to confirm they fail

```bash
cd backend
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~ProductGroupCrud" \
  --logger "console;verbosity=normal"
```

Expected: tests fail with `NotImplementedException` (not a compilation error).

### Task 3 — Implement ProductGroupService

Replace the stub in `backend/src/OrderMgmt.Application/Catalog/ProductGroups/Services/ProductGroupService.cs`
with the full implementation:

```csharp
using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Catalog.ProductGroups.Interfaces;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Catalog;

namespace OrderMgmt.Application.Catalog.ProductGroups.Services;

public class ProductGroupService : IProductGroupService
{
    private const string CodePrefix = "NG";
    private const int MaxCreateAttempts = 5;

    private readonly IAppDbContext _db;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;

    public ProductGroupService(IAppDbContext db, IDateTime clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ProductGroupListItemDto>> ListAsync(
        ProductGroupListRequest request, CancellationToken ct = default)
    {
        var query = _db.ProductGroups.AsNoTracking().Where(g => !g.IsDeleted);

        if (request.IsActive.HasValue)
            query = query.Where(g => g.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = $"%{EscapeLike(request.Search.Trim())}%";
            query = query.Where(g =>
                EF.Functions.ILike(g.Name, pattern) ||
                EF.Functions.ILike(g.Code, pattern));
        }

        query = (request.SortBy?.ToLowerInvariant(), request.SortDirection?.ToLowerInvariant()) switch
        {
            ("name", "desc") => query.OrderByDescending(g => g.Name),
            ("name", _)      => query.OrderBy(g => g.Name),
            ("code", "desc") => query.OrderByDescending(g => g.Code),
            ("code", _)      => query.OrderBy(g => g.Code),
            _                => query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name),
        };

        var totalItems = await query.CountAsync(ct);

        // Two-query approach: load groups first, then compute ProductCount with a single
        // GROUP BY query. Avoids fragile correlated-subquery translation inside Select.
        var groups = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.Id).ToList();
        var countsByGroup = await _db.Products
            .Where(p => p.ProductGroupId.HasValue && groupIds.Contains(p.ProductGroupId.Value))
            .GroupBy(p => p.ProductGroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, ct);

        var items = groups.Select(g => new ProductGroupListItemDto
        {
            Id           = g.Id,
            Code         = g.Code,
            Name         = g.Name,
            Description  = g.Description,
            SortOrder    = g.SortOrder,
            IsActive     = g.IsActive,
            ProductCount = countsByGroup.TryGetValue(g.Id, out var c) ? c : 0,
        }).ToList();

        return new PagedResult<ProductGroupListItemDto>
        {
            Items      = items,
            Page       = request.Page,
            PageSize   = request.PageSize,
            TotalItems = totalItems,
        };
    }

    public async Task<ProductGroupDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var g = await _db.ProductGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        return await MapToDtoAsync(g, ct);
    }

    public async Task<ProductGroupDto> CreateAsync(
        CreateProductGroupRequest request, CancellationToken ct = default)
    {
        var explicitCode = !string.IsNullOrWhiteSpace(request.Code);

        for (var attempt = 1; attempt <= MaxCreateAttempts; attempt++)
        {
            var code = explicitCode ? request.Code!.Trim() : await GenerateCodeAsync(ct);

            if (await _db.ProductGroups.AnyAsync(g => g.Code == code, ct))
            {
                if (explicitCode)
                    throw new ConflictException($"Mã nhóm '{code}' đã tồn tại.");
                continue;
            }

            var group = new ProductGroup
            {
                Code        = code,
                Name        = request.Name.Trim(),
                Description = request.Description?.Trim(),
                SortOrder   = request.SortOrder,
                IsActive    = request.IsActive,
            };

            _db.ProductGroups.Add(group);

            try
            {
                await _db.SaveChangesAsync(ct);
                return await GetAsync(group.Id, ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex) && !explicitCode && attempt < MaxCreateAttempts)
            {
                _db.Entry(group).State = EntityState.Detached;
            }
        }

        throw new ConflictException("Không thể tạo mã nhóm tự động sau nhiều lần thử. Vui lòng thử lại.");
    }

    public async Task<ProductGroupDto> UpdateAsync(
        Guid id, UpdateProductGroupRequest request, CancellationToken ct = default)
    {
        var group = await _db.ProductGroups
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        group.Name        = request.Name.Trim();
        group.Description = request.Description?.Trim();
        group.SortOrder   = request.SortOrder;
        group.IsActive    = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(group.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var group = await _db.ProductGroups
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(ProductGroup), id);

        group.IsDeleted = true;
        group.DeletedAt = _clock.UtcNow;
        group.DeletedBy = _currentUser.UserId;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<ProductGroupDto> MapToDtoAsync(ProductGroup g, CancellationToken ct)
    {
        var productCount = await _db.Products
            .CountAsync(p => p.ProductGroupId == g.Id && !p.IsDeleted, ct);

        return new ProductGroupDto
        {
            Id           = g.Id,
            Code         = g.Code,
            Name         = g.Name,
            Description  = g.Description,
            SortOrder    = g.SortOrder,
            IsActive     = g.IsActive,
            ProductCount = productCount,
            CreatedAt    = g.CreatedAt,
        };
    }

    private async Task<string> GenerateCodeAsync(CancellationToken ct)
    {
        var date = _clock.Now.ToString("yyMMdd");
        var todayCount = await _db.ProductGroups
            .CountAsync(g => g.Code.StartsWith($"{CodePrefix}-{date}"), ct);
        return $"{CodePrefix}-{date}-{todayCount + 1:D4}";
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        var sqlState = inner?.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
        return sqlState == "23505";
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
```

**Note:** `BaseEntity` must expose `DeletedAt` and `DeletedBy`. Check that `BaseEntity` has
these properties — if it only has `IsDeleted`, adapt the delete logic accordingly (use only
`IsDeleted = true`). Refer to the existing `ProductService.DeleteAsync` as the authoritative
pattern.

### Task 4 — Implement ProductGroupsController

Replace the stub in `backend/src/OrderMgmt.WebApi/Controllers/ProductGroupsController.cs`:

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
```

### Task 5 — Run tests to confirm they pass

```bash
cd backend
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~ProductGroupCrud" \
  --logger "console;verbosity=normal"
```

Expected: all 10 tests pass.

### Task 6 — Commit

```bash
git add backend/src/OrderMgmt.Application/Catalog/ProductGroups/ \
        backend/src/OrderMgmt.WebApi/Controllers/ProductGroupsController.cs \
        backend/src/OrderMgmt.Application/DependencyInjection.cs \
        backend/tests/OrderMgmt.IntegrationTests/ProductGroupCrudTests.cs
git commit -m "feat: add ProductGroup CRUD backend (service, controller, integration tests)"
```

## Verification

```bash
cd backend
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~ProductGroupCrud" \
  --logger "console;verbosity=normal"
```

Expected: 10 tests pass, 0 fail.

## Exit Criteria

- All 10 integration tests in `ProductGroupCrudTests` pass.
- No regression in existing integration tests (`dotnet test tests/OrderMgmt.IntegrationTests`
  exits with 0 failures).
