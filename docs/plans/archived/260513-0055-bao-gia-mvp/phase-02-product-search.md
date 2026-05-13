# Phase 02 — Product search endpoint

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** S

## Objective
Add a typeahead endpoint `GET /api/products/search?q=&take=20` that returns a lightweight product list optimised for the Quotation line-item product picker. Reused by the suggestion table the sales user sees while typing into the `Mã hàng` cell.

## Files
- `backend/src/OrderMgmt.Application/Catalog/Products/Interfaces/IProductService.cs` (add `SearchAsync`)
- `backend/src/OrderMgmt.Application/Catalog/Products/Models/ProductDto.cs` (add `ProductSuggestionDto`)
- `backend/src/OrderMgmt.Application/Catalog/Products/Services/ProductService.cs` (implement `SearchAsync`)
- `backend/src/OrderMgmt.WebApi/Controllers/ProductsController.cs` (add `Search` action)

## Tasks
1. **DTO** — add to `ProductDto.cs`:
   ```csharp
   public class ProductSuggestionDto
   {
       public Guid Id { get; set; }
       public string Code { get; set; } = default!;
       public string Name { get; set; } = default!;
       public string? Specification { get; set; }
       public string? UnitName { get; set; }
       public PricingMode PricingMode { get; set; }
       public decimal? DefaultPrice { get; set; }
       public decimal? CostPrice { get; set; }
   }
   ```
2. **Interface** — add `Task<IReadOnlyList<ProductSuggestionDto>> SearchAsync(string? query, int take, CancellationToken ct = default);` to `IProductService`.
3. **Implementation** — in `ProductService.SearchAsync`:
   - Clamp `take` to `[1, 50]`; default 20.
   - Filter `Status == Active && !IsDeleted`.
   - If `query` non-empty: `EF.Functions.ILike(p.Code, pattern) || ILike(p.Name, pattern)` with the existing `EscapeLike` helper.
   - Include `Unit` for `UnitName`.
   - Order by `Code` asc.
   - Project to `ProductSuggestionDto` and `.Take(take).ToListAsync(ct)`.
4. **Controller** — in `ProductsController.cs` add:
   ```csharp
   [HttpGet("search")]
   [HasPermission(Permissions.Products.View)]
   public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductSuggestionDto>>>> Search(
       [FromQuery] string? q, [FromQuery] int take = 20, CancellationToken ct = default)
   {
       var result = await _products.SearchAsync(q, take, ct);
       return Success(result);
   }
   ```

## Verification
```
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
```

After user restarts WebApi: `curl -H "Authorization: Bearer <token>" "http://localhost:5050/api/products/search?q=EPS&take=5"` returns up to 5 suggestions.

## Exit Criteria
- Application project builds clean.
- `SearchAsync` returns an empty list when `q` is empty or whitespace (do not return all products — that defeats typeahead pagination expectations).
- `take` is clamped to `[1, 50]` to avoid abuse.
