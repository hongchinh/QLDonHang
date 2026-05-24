# Phase 01 — Backend: AdvancePayment in list DTO & aggregates

**Status:** [ ] pending
**Complexity:** S

## Objective

Expose `AdvancePayment` in the quotation list API — both per-item and as an aggregate sum — so the frontend can display the column and footer total.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`

## Tasks

1. **`QuotationDto.cs` — `QuotationListItemDto`** (around line 117, after `Freight`/`Total`)
   Add:
   ```csharp
   public decimal AdvancePayment { get; set; }
   ```

2. **`QuotationDto.cs` — `QuotationListAggregates`** (around line 137, after `Total`)
   Add:
   ```csharp
   public decimal AdvancePayment { get; set; }
   ```

3. **`QuotationService.cs` — aggregate `Select` projection** (around line 196–204)
   Inside the `.Select(g => new QuotationListAggregates { ... })` block, add:
   ```csharp
   AdvancePayment = g.Sum(q => q.AdvancePayment),
   ```
   Place it after the `Total` line.

4. **`QuotationService.cs` — item `Select` projection** (around line 223–256)
   Inside the `.Select(q => new QuotationListItemDto { ... })` block, add:
   ```csharp
   AdvancePayment = q.AdvancePayment,
   ```
   Place it after the `Total` line.

## Verification

```bash
cd backend && dotnet build --no-restore -v q
```

No compiler errors, no warnings about missing properties.

## Exit Criteria

- `dotnet build` exits 0
- `QuotationListItemDto` has `AdvancePayment` property
- `QuotationListAggregates` has `AdvancePayment` property
- Both appear in the LINQ projections
