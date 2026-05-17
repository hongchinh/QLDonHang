# Execution Report — Quotation List Enhancements (3 items)

**Plan:** `docs/plans/260517-0658-quotation-list-enhancements/SUMMARY.md`
**Mode:** Batch
**Date:** 2026-05-17

## Phases

| # | Phase | Status |
|---|------|--------|
| 01 | Backend DTO + Service + Validator | [x] completed |
| 02 | MultiSelect component | [x] completed |
| 03 | Frontend types + api + page integration | [x] completed |
| 04 | Integration test for multi-status filter | [x] completed (compile-only) |

## Files Changed

**Backend (modified)**
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
  - Added `Subtotal`, `Discount`, `Freight` to `QuotationListItemDto`.
  - Changed `QuotationListRequest.Status` from `QuotationStatus?` to `string?` (comma-separated values).
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
  - Added `using OrderMgmt.Application.Sales.Quotations.Helpers;`.
  - Replaced single-status filter with `QuotationStatusListParser.Parse(...)` + `Contains`.
  - Added 3 financial fields to the projection.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs`
  - Added validator on `Status` that delegates to `QuotationStatusListParser.IsValid`.

**Backend (new)**
- `backend/src/OrderMgmt.Application/Sales/Quotations/Helpers/QuotationStatusListParser.cs` — shared parse/validate helper.

**Frontend (modified)**
- `frontend/src/features/quotations/types.ts`
  - Added `subtotal/discount/freight` to `QuotationListItem`.
  - Renamed `status?: QuotationStatus` → `statuses?: QuotationStatus[]` on `QuotationListParams`.
- `frontend/src/features/quotations/api.ts`
  - `list()` now joins `statuses` array into a comma-separated `status` query param.
- `frontend/src/pages/quotations/quotation-list-page.tsx`
  - Added `STATUS_OPTIONS` constant + `VALID_STATUSES` whitelist.
  - URL param parsing through `useSearchParamString('status')` + memoized array.
  - Replaced `Select` with `MultiSelect`.
  - Added 3 financial columns (Tổng tiền hàng / Chiết khấu / VC) before existing `Tổng tiền`.
  - Refactored actions cell: kept `Sửa` (Pencil) inline; moved Clone / In PDF / Hủy into `⋯` dropdown.

**Frontend (new)**
- `frontend/src/components/ui/multi-select.tsx` — generic `MultiSelect<T extends string>` built on existing `DropdownMenuCheckboxItem`. No new deps.

**Tests (new)**
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationListFilterTests.cs` — 4 cases:
  - multi-status filter (`?status=Draft,Sent`)
  - legacy single-status (`?status=Draft`)
  - invalid status → 400
  - DTO includes Subtotal/Discount/Freight

## Verification

| Command | Outcome |
|--------|---------|
| `dotnet build OrderMgmt.Application.csproj` | ✅ 0 warnings, 0 errors |
| `dotnet build OrderMgmt.IntegrationTests.csproj --no-dependencies` | ✅ 0 warnings, 0 errors |
| `npm run typecheck` (frontend) | ✅ pass |
| `dotnet test --filter QuotationListFilter` | ⚠ blocked — Docker not running locally (Testcontainers) |

## Deviations from Plan

- **Phase 04 runtime verification deferred.** Testcontainers requires Docker; Docker was not available on the host at execution time. User accepted compile-only verification. Tests follow the same fixture/pattern as `QuotationStateMachineTests` so should pass when run in an environment with Docker.
- **MultiSelect prop typing tweak.** Plan specified `options: MultiSelectOption<T>[]`. Changed to `ReadonlyArray<MultiSelectOption<T>>` so the `STATUS_OPTIONS` `ReadonlyArray` constant in the page can be passed without a cast. Trivial relaxation; no behavior change.
- **WebApi build skipped** in line with project memory: WebApi was running and holds locks on its `bin/Debug/net9.0/*.dll`. The Application library build (which carries the actual API surface change) was clean.

## Residual Risks / Follow-ups

1. **Manual smoke test** still recommended once WebApi is restarted:
   - 4 financial columns formatted vi-VN.
   - Dropdown shows Clone (when canClone), In PDF, Hủy (when canCancel).
   - Multi-select filter writes `?status=Draft,Sent` and reloads correctly.
   - Legacy bookmark `?status=Draft` still works.
   - Cancel flow opens ConfirmDialog and succeeds.
2. **Integration tests** should be run in a Docker-enabled environment to confirm the 4 cases pass.
3. **`CanClone` semantics unchanged** — the `⋯` menu for normal quotations will only contain `In PDF` + `Hủy`. Tradeoff explicitly accepted in the plan; revisit later if user wants always-on clone.
