# Phase 01 — Backend AdvancePayment

**Status:** [ ] pending
**Complexity:** S

## Objective
Thêm field `AdvancePayment` vào `QuotationListItemDto`, `QuotationListAggregates`, projection query, và aggregate sum trong `QuotationService`.

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

3. **`QuotationService.cs` — aggregate query** (dòng ~196–204): thêm vào `Select(g => new QuotationListAggregates { ... })`:
   ```csharp
   AdvancePayment = g.Sum(q => q.AdvancePayment),
   ```

4. **`QuotationService.cs` — items projection** (dòng ~223–256): thêm vào `Select(q => new QuotationListItemDto { ... })`:
   ```csharp
   AdvancePayment = q.AdvancePayment,
   ```
   Đặt sau `Freight = q.Freight,` để giữ thứ tự nhất quán với `QuotationDto`.

## Verification
```bash
cd backend
dotnet build src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj --no-restore -c Release
```
Build phải pass không có lỗi.

## Exit Criteria
- `QuotationListItemDto` có property `AdvancePayment`
- `QuotationListAggregates` có property `AdvancePayment`
- Projection và aggregate sum đều map `AdvancePayment`
- `dotnet build` pass
