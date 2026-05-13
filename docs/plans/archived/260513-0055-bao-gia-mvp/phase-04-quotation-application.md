# Phase 04 — Quotation application layer

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** L

## Objective
Implement the use-case service that drives Quotation behavior: CRUD with full snapshotting, deterministic server-side recompute across the four pricing modes, document code generation, state-machine guarded transitions, and FluentValidation contracts.

## Files
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (new — `QuotationDto`, `QuotationListItemDto`, `QuotationLineDto`, `UpsertQuotationRequest`, `UpsertQuotationLineRequest`, `QuotationListRequest`, `TransitionQuotationRequest`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs` (new)
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (register `IQuotationService`)

## Tasks

1. **DTOs** — `QuotationDto` exposes header + `Lines` (`QuotationLineDto`) + `CreatedAt` + `CreatedBy`. `QuotationListItemDto` lightweight columns: Id, Code, QuotationDate, CustomerName, ContactPhone, Total, Status, CreatedByName, CreatedAt. `UpsertQuotationRequest` carries header + `IReadOnlyList<UpsertQuotationLineRequest> Lines`. `QuotationListRequest : PageRequest` with `Status?`, `Guid? CustomerId`, `DateOnly? From`, `DateOnly? To`. `TransitionQuotationRequest { QuotationAction Action; }` where `QuotationAction` is a new enum `{ Send=1, Confirm=2, Cancel=9 }`.

2. **IQuotationService**:
   ```csharp
   Task<PagedResult<QuotationListItemDto>> ListAsync(QuotationListRequest r, CancellationToken ct = default);
   Task<QuotationDto> GetAsync(Guid id, CancellationToken ct = default);
   Task<QuotationDto> CreateAsync(UpsertQuotationRequest r, CancellationToken ct = default);
   Task<QuotationDto> UpdateAsync(Guid id, UpsertQuotationRequest r, CancellationToken ct = default);
   Task DeleteAsync(Guid id, CancellationToken ct = default);
   Task<QuotationDto> TransitionAsync(Guid id, QuotationAction action, CancellationToken ct = default);
   ```

3. **Validators** — `UpsertQuotationRequestValidator`:
   - `CustomerId` not empty.
   - `QuotationDate` not default.
   - `TaxRate` `[0, 100]`.
   - `Discount >= 0`, `Freight >= 0`.
   - `Lines` not empty (`Must(l => l.Count > 0).WithMessage("Báo giá phải có ít nhất 1 dòng.")`).
   - `RuleForEach(x => x.Lines).SetValidator(new UpsertQuotationLineRequestValidator())`.
   - Line validator: `ProductName` not empty (≤255), `UnitName` not empty (≤100), `Quantity > 0`, `UnitPrice >= 0`, `PricingMode IsInEnum`, dimensions `>= 0` when present, `UnitCost >= 0` when present.
   - `QuotationListRequestValidator : PageRequestValidator<QuotationListRequest>`.
   - `TransitionQuotationRequestValidator`: `Action IsInEnum`.

4. **QuotationService** (constructor injects `IAppDbContext`, `IDateTime`, `ICurrentUser`):
   - `private const string CodePrefix = "BG"; private const int MaxCreateAttempts = 5;` mirror `ProductService`.
   - `EnsureCustomerAsync` similar to `ProductService.EnsureReferencesAsync` — throws `NotFoundException(nameof(Customer), customerId)` if missing.
   - `ListAsync`: build query with filters, count, paginate, project to `QuotationListItemDto` (join `Customer` for snapshot name in case the customer was renamed afterward — but since we snapshot on save, `CustomerName` on the Quotation is the authoritative source for the list).
   - `GetAsync`: `.Include(q => q.Lines.OrderBy(l => l.SortOrder))` and map.
   - `CreateAsync`: code generation loop identical to ProductService; populate snapshot fields from Customer; populate each line snapshot from Product (if `ProductId` provided) else trust request fields; call `Recompute(quotation)` before save; on success return `GetAsync(id)`.
   - `UpdateAsync`: load existing with `Include(Lines)`; if `Status == Cancelled` throw `DomainException("Báo giá đã hủy không thể chỉnh sửa.")`; rebuild line collection — diff by `Id`: existing lines whose id appears in the request are mutated, missing ones are soft-deleted (`IsDeleted = true`), new requests without id are added; refresh snapshots; `Recompute(quotation)`; save.
   - `DeleteAsync`: load, set `IsDeleted = true / DeletedAt / DeletedBy`. The cascade in `AppDbContext.SaveChangesAsync` will propagate to lines.
   - `TransitionAsync`: load (no `AsNoTracking`); compute allowed transitions via a static map:
     ```
     Draft     → { Sent (Send), Cancelled (Cancel) }
     Sent      → { Confirmed (Confirm), Cancelled (Cancel) }
     Confirmed → { Cancelled (Cancel) }     // ConvertedToOrder reserved for v2
     Cancelled → ∅
     ```
     if request action is not in the allowed set → `DomainException("Không thể chuyển trạng thái …")`; otherwise set new status, save, return `GetAsync(id)`.

5. **Recompute logic** — private `static void Recompute(Quotation q)`:
   - For each line set `LineTotal = Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero)`.
   - If `UnitCost.HasValue` → `LineCost = Math.Round(Quantity * UnitCost.Value, 2, ...); LineProfit = LineTotal - LineCost;` else `LineCost = null; LineProfit = null`.
   - `q.Subtotal = sum(LineTotal)`; `q.TotalCost = sum(LineCost ?? 0)`.
   - `q.TaxAmount = Math.Round(q.Subtotal * q.TaxRate / 100m, 0, AwayFromZero);` (VND has no minor unit → integer-VND).
   - `q.Total = q.Subtotal - q.Discount + q.Freight + q.TaxAmount;`
   - `q.GrossProfit = q.Subtotal - q.TotalCost - q.Discount;` (freight excluded; matches BD §13.5 baseline. Document the choice in a comment so reviewers know it's intentional.)
   - Notes:
     - Pricing-mode-specific *quantity* computation (e.g. m² from L×W×SheetCount) is the **frontend's** job: it fills `Quantity` with the computed area before sending. Backend treats `Quantity` as authoritative. This keeps `Recompute` linear and lets manual overrides "just work".
     - Dimensions (`Length`/`Width`/`Thickness`/`Density`/`SheetCount`) are stored as a snapshot for the printout — not used by `Recompute`.

6. **Code generator** — `GenerateCodeAsync`: same shape as `ProductService.GenerateCodeAsync` but `CodePrefix = "BG"`.

7. **DI** — in `DependencyInjection.cs` add `services.AddScoped<IQuotationService, QuotationService>();` alongside the other scoped registrations.

## Verification
```
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
```

## Exit Criteria
- Application project builds clean.
- Cancellation tokens threaded through every async call.
- `Recompute` is `static` and pure (no DbContext access) so it can be unit-tested directly in the integration test project.
- State machine transitions are encoded in a single source of truth (a `static readonly Dictionary` or `switch` on `(status, action)`).
- No nullable warnings on `Customer?` navigation — service uses `Customer` only for `Include` + snapshot copy, never persists null into snapshot fields (Customer must exist by `EnsureCustomerAsync`).
