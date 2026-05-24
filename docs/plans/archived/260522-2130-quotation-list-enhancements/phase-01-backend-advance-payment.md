# Phase 01 — Backend AdvancePayment

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm `AdvancePayment` vào DTO list item và aggregate, đồng thời select và sum field này trong `ListAsync`.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`

## Tasks

1. **`QuotationDto.cs` — `QuotationListItemDto`**: thêm property sau `Total`:
   ```csharp
   public decimal AdvancePayment { get; set; }
   ```

2. **`QuotationDto.cs` — `QuotationListAggregates`**: thêm property sau `Total`:
   ```csharp
   public decimal AdvancePayment { get; set; }
   ```

3. **`QuotationService.cs` — projection trong `ListAsync`**: trong block `.Select(q => new QuotationListItemDto { ... })`, thêm sau `Total = q.Total,`:
   ```csharp
   AdvancePayment = q.AdvancePayment,
   ```

4. **`QuotationService.cs` — aggregate query**: trong block `.Select(g => new QuotationListAggregates { ... })`, thêm sau `Total = g.Sum(q => q.Total),`:
   ```csharp
   AdvancePayment = g.Sum(q => q.AdvancePayment),
   ```

## Verification

```bash
cd backend && dotnet build src/OrderMgmt.Application --no-restore
cd backend && dotnet build src/OrderMgmt.WebApi --no-restore
```

Không có lỗi compile.

## Exit Criteria

- `QuotationListItemDto` có property `AdvancePayment`
- `QuotationListAggregates` có property `AdvancePayment`
- `ListAsync` select và sum `AdvancePayment` từ entity
- Build pass
