# Phase 03 — Quotation domain + EF + migration

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Introduce the `Quotation` aggregate root with owned `QuotationLine` entities. Snapshot fields make each quotation invariant to later catalog edits. Register both `DbSet`s on `IAppDbContext` + `AppDbContext`, configure EF mappings (snake_case via existing convention), and produce the `AddQuotations` migration.

## Files
- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` (new)
- `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationLine.cs` (new)
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` (add two DbSets)
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` (add DbSet properties + ensure configuration is auto-applied via `ApplyConfigurationsFromAssembly`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` (new — `QuotationConfiguration`, `QuotationLineConfiguration`)
- `backend/src/OrderMgmt.Infrastructure/Migrations/<timestamp>_AddQuotations.cs` (generated)
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (regenerated)

## Tasks

1. **Quotation entity** (`Quotation.cs`):
   ```csharp
   using OrderMgmt.Domain.Common;
   using OrderMgmt.Domain.Entities.Catalog;
   using OrderMgmt.Domain.Enums;

   namespace OrderMgmt.Domain.Entities.Sales;

   public class Quotation : BaseEntity
   {
       public string Code { get; set; } = default!;
       public DateOnly QuotationDate { get; set; }

       public Guid CustomerId { get; set; }
       public Customer? Customer { get; set; }

       // Customer snapshot
       public string CustomerName { get; set; } = default!;
       public string? CustomerTaxCode { get; set; }
       public string? CustomerAddress { get; set; }
       public string? ContactPerson { get; set; }
       public string? ContactPhone { get; set; }

       // Delivery snapshot
       public string? DeliveryAddress { get; set; }
       public string? DeliveryRecipient { get; set; }
       public string? DeliveryPhone { get; set; }
       public DateOnly? DeliveryDate { get; set; }
       public string? DeliveryNote { get; set; }

       // Totals (server recomputes)
       public decimal Subtotal { get; set; }
       public decimal Discount { get; set; }
       public decimal Freight { get; set; }
       public decimal TaxRate { get; set; }
       public decimal TaxAmount { get; set; }
       public decimal Total { get; set; }
       public decimal TotalCost { get; set; }
       public decimal GrossProfit { get; set; }

       public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
       public string? InternalNote { get; set; }

       public ICollection<QuotationLine> Lines { get; set; } = new List<QuotationLine>();
   }
   ```

2. **QuotationLine entity** (`QuotationLine.cs`):
   ```csharp
   public class QuotationLine : BaseEntity
   {
       public Guid QuotationId { get; set; }
       public Quotation? Quotation { get; set; }

       public int SortOrder { get; set; }

       public Guid? ProductId { get; set; }
       public Product? Product { get; set; }

       // Product snapshot
       public string? ProductCode { get; set; }
       public string ProductName { get; set; } = default!;
       public string? Specification { get; set; }
       public string UnitName { get; set; } = default!;
       public PricingMode PricingMode { get; set; } = PricingMode.PerUnit;

       // Dimensions (optional, used when pricing-mode-aware)
       public decimal? Length { get; set; }
       public decimal? Width { get; set; }
       public decimal? Thickness { get; set; }
       public decimal? Density { get; set; }
       public decimal? SheetCount { get; set; }

       // Pricing
       public decimal Quantity { get; set; }
       public decimal UnitPrice { get; set; }
       public decimal LineTotal { get; set; }
       public decimal? UnitCost { get; set; }
       public decimal? LineCost { get; set; }
       public decimal? LineProfit { get; set; }

       public string? Note { get; set; }
   }
   ```

3. **DbSet** on `IAppDbContext` (Application):
   ```csharp
   DbSet<Quotation> Quotations { get; }
   DbSet<QuotationLine> QuotationLines { get; }
   ```
   and the matching `public DbSet<Quotation> Quotations => Set<Quotation>();` / `QuotationLines` in `AppDbContext.cs`. Verify the existing `modelBuilder.ApplyConfigurationsFromAssembly` line already picks up the new `SalesConfiguration` (it does — both configurations live in the same assembly).

4. **EF configuration** (`SalesConfiguration.cs`):
   - `QuotationConfiguration`:
     - `ToTable("quotations")`, `HasKey(x => x.Id)`.
     - `Code`: `IsRequired().HasMaxLength(50)`, unique filtered index `HasIndex(x => x.Code).IsUnique().HasFilter("is_deleted = false")`.
     - All snapshot strings sized (Name 255, TaxCode 20, Address 1000, ContactPerson 255, ContactPhone 30, DeliveryAddress 1000, DeliveryRecipient 255, DeliveryPhone 30, DeliveryNote 1000, InternalNote 2000).
     - Money columns `Subtotal`, `Discount`, `Freight`, `TaxAmount`, `Total`, `TotalCost`, `GrossProfit`: `HasColumnType("numeric(18,2)")`.
     - `TaxRate`: `numeric(5,2)`.
     - `HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict)`.
     - `HasMany(x => x.Lines).WithOne(x => x.Quotation!).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Restrict)` — cascade soft-delete is handled by `AppDbContext.SaveChangesAsync`; physical cascade would conflict.
     - `HasQueryFilter(x => !x.IsDeleted)`.
     - `HasIndex(x => x.CustomerId)`, `HasIndex(x => x.QuotationDate)`, `HasIndex(x => x.Status)`.
   - `QuotationLineConfiguration`:
     - `ToTable("quotation_lines")`, `HasKey(x => x.Id)`.
     - Strings sized (ProductCode 50, ProductName 255, Specification 500, UnitName 100, Note 1000).
     - Money columns `Quantity` `numeric(18,4)`, `UnitPrice/LineTotal/UnitCost/LineCost/LineProfit` `numeric(18,2)`, dimensions `numeric(18,4)`.
     - `HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull)`.
     - `HasQueryFilter(x => !x.IsDeleted && !x.Quotation!.IsDeleted)` — the parent filter avoids orphan lines being returned when the parent is soft-deleted but the child query is evaluated independently. Consistent with `UserRole`/`RolePermission`.

5. **Generate migration**:
   ```
   dotnet ef migrations add AddQuotations \
     --project backend/src/OrderMgmt.Infrastructure \
     --startup-project backend/src/OrderMgmt.WebApi \
     --output-dir Migrations
   ```
   Inspect the generated migration:
   - `quotations` table includes all snake_case columns.
   - `quotation_lines` table with FK to `quotations.id` (`ON DELETE RESTRICT`) and FK to `products.id` (`ON DELETE SET NULL`).
   - Unique index on `quotations.code` filtered by `is_deleted = false`.
   - Non-unique indexes for `customer_id`, `quotation_date`, `status`.
   - Audit columns (`created_at`, `updated_at`, etc.) inherited from `BaseEntity` are present.

## Verification
```
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal

# Inspect migration content
git diff backend/src/OrderMgmt.Infrastructure/Migrations/ | head -200
```

## Exit Criteria
- Three projects build clean.
- Migration file exists and contains both tables, FKs, indexes as specified.
- `AppDbContextModelSnapshot.cs` updated.
- No physical cascade-delete from `quotations` to `quotation_lines` in the migration (FK `ON DELETE RESTRICT` only).
