# Phase 01 — Product PricingMode

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Add `PricingMode` to the `Product` aggregate so each catalog item declares how its lines are priced (per unit, per m², per linear meter, per m³). Surface the field in the Products list filter, the Product form, the typeahead suggestion, and the existing Product DTOs. Default existing rows to `PerUnit`. This is a prerequisite for line-item computation in Quotations.

## Files
- `backend/src/OrderMgmt.Domain/Enums/Enums.cs` (add `PricingMode` enum)
- `backend/src/OrderMgmt.Domain/Entities/Catalog/Product.cs` (add `PricingMode` field, default `PerUnit`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/CatalogConfiguration.cs` (`ProductConfiguration`: add `.Property(x => x.PricingMode)` with default to keep migration simple)
- `backend/src/OrderMgmt.Infrastructure/Migrations/<timestamp>_AddPricingModeToProduct.cs` (generated)
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (regenerated)
- `backend/src/OrderMgmt.Application/Catalog/Products/Models/ProductDto.cs` (`ProductDto`, `ProductListItemDto`, `CreateProductRequest`, `UpdateProductRequest` — add `PricingMode`)
- `backend/src/OrderMgmt.Application/Catalog/Products/Services/ProductService.cs` (carry the field through Create/Update and `MapToDto`)
- `backend/src/OrderMgmt.Application/Catalog/Products/Validators/ProductValidators.cs` (`PricingMode` required, `IsInEnum()`)
- `frontend/src/features/products/types.ts` (add `PricingMode` union, add field to `Product`, `ProductListItem`, `CreateProductRequest`, `UpdateProductRequest`)
- `frontend/src/features/products/schema.ts` (add `pricingMode` enum field, default `PerUnit` on create)
- `frontend/src/pages/products/product-form-page.tsx` (Pricing mode `Select` in "Thông tin chung" card; include in `toFormDefaults` / `toCreatePayload` / `toUpdatePayload`)
- `frontend/src/pages/products/product-list-page.tsx` (column `Loại giá` + optional filter chip — column only is required)

## Tasks
1. **Domain enum**: in `Enums.cs` add
   ```csharp
   public enum PricingMode
   {
       PerUnit = 1,
       PerSquareMeter = 2,
       PerLinearMeter = 3,
       PerCubicMeter = 4,
   }
   ```
2. **Entity field**: in `Product.cs` add `public PricingMode PricingMode { get; set; } = PricingMode.PerUnit;`
3. **EF configuration**: in `ProductConfiguration.Configure` add `b.Property(x => x.PricingMode).HasConversion<int>().HasDefaultValue(PricingMode.PerUnit);` — the default ensures the migration backfills existing rows safely.
4. **Generate migration**:
   ```
   dotnet ef migrations add AddPricingModeToProduct \
     --project backend/src/OrderMgmt.Infrastructure \
     --startup-project backend/src/OrderMgmt.WebApi \
     --output-dir Migrations
   ```
   Review the generated `Up` for `AddColumn<int>("pricing_mode", ... defaultValue: 1)` and `Down` for `DropColumn`.
5. **Application DTOs**: extend `ProductDto`, `ProductListItemDto`, `CreateProductRequest`, `UpdateProductRequest` with `PricingMode PricingMode { get; set; }`.
6. **Service**: in `ProductService.CreateAsync` and `UpdateAsync` copy `request.PricingMode` onto the entity; in `MapToDto` map back. In `ListAsync` `.Select(...)` include `PricingMode = p.PricingMode`.
7. **Validator**: in both `CreateProductRequestValidator` and `UpdateProductRequestValidator` add `RuleFor(x => x.PricingMode).IsInEnum();`.
8. **Frontend types**: add `export type PricingMode = 'PerUnit' | 'PerSquareMeter' | 'PerLinearMeter' | 'PerCubicMeter';` and include `pricingMode: PricingMode` in each Product type.
9. **Frontend schema**: add `pricingMode: z.enum(['PerUnit','PerSquareMeter','PerLinearMeter','PerCubicMeter'])` to `productSchema`.
10. **Form**: in `product-form-page.tsx`, add a `Select` for pricing mode under the "Thông tin chung" card with the four options (label them "Theo đơn vị / Theo m² / Theo m dài / Theo m³"); wire `toFormDefaults` to fall back to `'PerUnit'`; include `pricingMode` in both payload builders.
11. **List**: in `product-list-page.tsx`, add column `{ header: 'Loại giá', accessorKey: 'pricingMode', cell: ({ row }) => labelForMode(row.original.pricingMode) }` using a small lookup map.

## Verification
```
# Backend
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal

# Migration sanity check
git diff --stat backend/src/OrderMgmt.Infrastructure/Migrations/

# Frontend
cd frontend && npm run build && npm test -- --run
```

## Exit Criteria
- `Application` and `Infrastructure` projects build with zero warnings/errors.
- Migration file present, `Up` contains `AddColumn` with default `1`, `Down` contains `DropColumn`.
- `npm run build` succeeds; `npm test -- --run` passes (28 existing tests still green).
- Manually viewing the Products list/form (after WebApi restart) shows the new field — defer this to user smoke testing; not part of automated gates.
