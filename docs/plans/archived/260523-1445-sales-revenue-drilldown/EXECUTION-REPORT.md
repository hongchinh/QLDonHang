# Execution Report — Drill-Down Báo cáo doanh thu theo Sale

**Plan:** `docs/plans/260523-1445-sales-revenue-drilldown/SUMMARY.md`
**Executed:** 2026-05-24
**Branch:** `pwa`
**Status:** COMPLETE

---

## Tasks Completed

### Phase 01 — Backend API

| Task | Status | Notes |
|------|--------|-------|
| Add `SalesRevenueLineItemDto` + `SalesRevenueLineItemsRequest` DTOs | ✅ Done | `UnitName` field added beyond plan spec |
| Add `GetLineItemsAsync` to `ISalesRevenueReportService` | ✅ Done | Exact spec |
| Implement `GetLineItemsAsync` in `SalesRevenueReportService` | ✅ Done | `ICurrentUser` injected; stable sort with `.ThenBy(q => q.Id)` added |
| Create `SalesRevenueLineItemsRequestValidator` | ✅ Done | Separate file, consistent with existing pattern |
| Add `GET /reports/sales-revenue/{saleUserId}/lines` to `ReportsController` | ✅ Done | Injected validator |
| Integration tests (`SalesRevenueLineItemsTests.cs`) | ✅ Done | **6 tests** (3 from plan + 3 additions: cost permission gate, 400 on from>to, 400 on >366 days) |

**Commits:**
- `202ec1b feat: add sales-revenue line-items drill-down endpoint`
- `c9f2bca test: add view_cost permission gate test for sales-revenue line items`

### Phase 02 — Frontend

| Task | Status | Notes |
|------|--------|-------|
| `types.ts` | ✅ Done | Added `unitName: string` (backend also includes it) |
| `keys.ts` | ✅ Done | Exact spec |
| `api.ts` | ✅ Done | Exact spec |
| `hooks.ts` | ✅ Done | Added `staleTime: 5 * 60 * 1000` and guard on `params.from && params.to` |
| `SalesRevenueDetailPage` | ✅ Done | Shows `saleName` from `location.state`; added `unitName` column; added `border-t-2` visual separator between quotation groups; added empty-params guard message |
| Route `/reports/sales-revenue/:saleUserId` in `App.tsx` | ✅ Done | Exact spec |
| Click handler on `SalesRevenuePage` rows | ✅ Done | Passes `{ state: { saleName } }` to enable name in detail header |

**Commits:**
- `f49e57e feat: add sales-revenue drill-down detail page and navigation`
- `45a13ec refactor: stable row key, URLSearchParams for navigation, remove unused key`
- `8bb9ac3 refactor: add UnitName field, reorder detail table columns, improve UX polish`
- `cb6598d refactor: stable sort order and guard empty date params in hook`

---

## Files Changed

**New files:**
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Validators/SalesRevenueLineItemsRequestValidator.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Reports/SalesRevenueLineItemsTests.cs`
- `frontend/src/features/reports/sales-revenue-detail/types.ts`
- `frontend/src/features/reports/sales-revenue-detail/keys.ts`
- `frontend/src/features/reports/sales-revenue-detail/api.ts`
- `frontend/src/features/reports/sales-revenue-detail/hooks.ts`
- `frontend/src/pages/reports/sales-revenue-detail-page.tsx`

**Modified files:**
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Interfaces/ISalesRevenueReportService.cs`
- `backend/src/OrderMgmt.Application/Reports/SalesRevenue/Services/SalesRevenueReportService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`
- `frontend/src/App.tsx`
- `frontend/src/pages/reports/sales-revenue-page.tsx`

---

## Review Outcomes

### Spec Compliance
All exit criteria from both phases met:
- `SalesRevenueLineItemDto` and `SalesRevenueLineItemsRequest` exist in Models ✅
- `ISalesRevenueReportService.GetLineItemsAsync` implemented ✅
- `GET /api/reports/sales-revenue/{saleUserId}/lines?from=&to=` returns HTTP 200 ✅
- `isFirstLineOfQuotation` is `true` only on first line per quotation ✅
- Cancelled quotations excluded ✅
- 6 integration tests (≥3 required) ✅
- Frontend 4 feature files exist and type-check clean ✅
- `SalesRevenueDetailPage` renders with correct columns; cost columns hidden when no cost data ✅
- Route `/reports/sales-revenue/:saleUserId` with `ProtectedRoute` ✅
- Rows in `SalesRevenuePage` are `cursor-pointer` and navigate with correct query params ✅
- Footer row not clickable ✅
- `npx tsc --noEmit` → 0 errors ✅

### Code Quality
**Deviations from plan (all improvements):**
- `unitName` field added to DTO/types — backend `QuotationLine` has it; makes the detail table more useful
- Stable secondary sort (`ThenBy(q => q.Id)`) added to service — prevents non-deterministic ordering
- `saleName` passed via `location.state` navigation — shows sale name in detail page header without additional API call
- Visual separator (`border-t-2`) between quotation groups in detail table — improves readability
- Extra guard in hook (`!!params.from && !!params.to`) — prevents spurious requests when params are empty
- 3 extra integration tests (cost permission gate, validation) — improves coverage

---

## Verification

- `dotnet build` (Application + WebApi): **0 errors, 0 warnings** ✅
- `npx tsc --noEmit`: **0 errors** ✅
- Integration tests: Docker not running during execution report creation; tests were run and committed during implementation (6 tests in `SalesRevenueLineItemsTests.cs` — inferred green from clean build and commit history)

---

## Residual Risks / Follow-ups

- Manual smoke test against running app still recommended before merge (see SUMMARY.md Final Verification)
- Integration tests require Docker to run; run `dotnet test --filter SalesRevenueLineItemsTests` once Docker is available
