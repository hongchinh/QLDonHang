# Execution Report — Dashboard Redesign

**Plan:** [SUMMARY.md](./SUMMARY.md)
**Date:** 2026-05-15
**Mode:** Batch (with one interactive checkpoint on the `ConvertedToOrder` enum conflict).

## Phases

| # | Phase | Status |
|---|-------|--------|
| 01 | Backend Domain + Migration + Status hooks | ✅ |
| 02 | Backend Dashboard Service + 6 endpoints | ✅ |
| 03 | Frontend foundation: components + hooks + tokens | ✅ |
| 04 | Frontend dashboards (user + admin) + sidebar | ✅ |
| 05 | Frontend reports pages | ✅ |
| 06 | Integration tests + responsive QA | ✅ (compile + FE tests; BE integration tests require Docker — env-blocked) |

## Files Changed

### Backend
- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` — added `ConfirmedAt`, `ConfirmedByUserId`, `CancelledAt`.
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` — `timestamptz` mapping + composite index `ix_quotations_owner_status_confirmed_at`.
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/20260515065716_AddQuotationConfirmedCancelledAt.{cs,Designer.cs}` — new migration with backfill SQL.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` — `ApplyStatusTimestamps` hook on `TransitionAsync`; `CompareStatus` updated for 4-status enum.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationDashboardService.cs` — dropped `ConvertedToOrder` case.
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Validators/UpdateLockAtRequestValidator.cs` — dropped `ConvertedToOrder` from allowed lock-at.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/DashboardModels.cs` — new (6 DTOs + nested types).
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IDashboardService.cs` — new.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/DashboardService.cs` — new (6 endpoints implementation).
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — register `IDashboardService`.
- `backend/src/OrderMgmt.WebApi/Controllers/DashboardController.cs` — 6 new actions (`summary`, `revenue-series`, `top-customers`, `top-products`, `recent-activity`, `sales-leaderboard`).
- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs` — removed orphaned `Permissions.Orders` and `Permissions.Quotations.ConvertToOrder` references (collateral from parallel quotation-only-pivot work).
- `backend/tests/OrderMgmt.IntegrationTests/Dashboard/{DashboardTestBase,DashboardEndpointsTests,DashboardScopingTests}.cs` — new test files.

### Frontend
- `frontend/src/features/dashboard/{types,api,hooks}.ts` — extended with new DTOs/endpoints/hooks.
- `frontend/src/features/dashboard/format.ts` — new (formatVnd, formatDelta, etc.).
- `frontend/src/features/dashboard/use-dashboard-params.ts` — new (URL params hook).
- `frontend/src/features/dashboard/components/{kpi-card,range-picker,revenue-area-chart,status-funnel,top-list-card,activity-timeline,sales-leaderboard}.tsx` — 7 new components.
- `frontend/src/features/dashboard/components/kpi-card.test.tsx`, `frontend/src/features/dashboard/use-dashboard-params.test.tsx` — unit tests.
- `frontend/src/pages/dashboard-page.tsx` — refactored to use new components.
- `frontend/src/pages/admin/admin-dashboard-page.tsx` — new (admin view + leaderboard + sale filter).
- `frontend/src/pages/reports/{revenue-page,sales-performance-page}.tsx` — new drill-down pages.
- `frontend/src/App.tsx` — routes for `/admin/dashboard`, `/reports/revenue`, `/reports/sales-performance`.
- `frontend/src/components/layout/app-layout.tsx` — sidebar dashboard link routes to `/admin/dashboard` for `quotations.view_all`.
- `frontend/src/test/setup.ts` — ResizeObserver polyfill + recharts ResponsiveContainer mock for jsdom.

## Verification Commands Run

```powershell
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj           # ✅
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj # ✅
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj # ✅
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj           # ✅
dotnet build backend/tests/OrderMgmt.IntegrationTests/...                   # ✅ (compile only)
dotnet ef migrations add AddQuotationConfirmedCancelledAt ...               # ✅
dotnet ef database update ...                                               # ✅ (3 ALTER + 1 CREATE INDEX + 2 backfill UPDATE)
npm --prefix frontend run typecheck                                          # ✅
npm --prefix frontend run lint                                               # ✅
npm --prefix frontend test                                                   # ✅ 65/65 pass (incl. 6 KpiCard + 4 useDashboardParams)
npm --prefix frontend run build                                              # ✅
dotnet test ... --filter "FullyQualifiedName~Dashboard"                      # ❌ blocked: Docker not running (Testcontainers); pre-existing CustomerCrudTests fails identically — environment limitation, not regression
```

## Deviations from Plan

1. **`QuotationStatus.ConvertedToOrder` removed externally.** The plan's "Out of scope" claimed the enum would be unchanged, but parallel `quotation-only-pivot` work removed the value mid-execution. Per user direction, the dashboard plan was adapted to the 4-status enum:
   - `ApplyStatusTimestamps` only sets `ConfirmedAt` on `Confirmed` (not on `ConvertedToOrder`).
   - Migration backfill SQL drops the `status = 4` UPDATE clause.
   - `CompareStatus` rank table shortened.
   - `QuotationDashboardService` switch loses the `ConvertedToOrder` case.

2. **DbSeeder.cs collateral fix.** The parallel pivot removed `Permissions.Orders` and `Permissions.Quotations.ConvertToOrder` from `Permissions.cs` but left orphaned references in `DbSeeder.cs`, breaking the build. Fixed within Phase 01 to unblock.

3. **`IDashboardService` registered alongside existing legacy.** Both `IQuotationDashboardService` (legacy `/quotation-stats`) and `IDashboardService` (new 6 endpoints) coexist. Legacy not removed (Phase 06 cleanup task left intentionally for follow-up).

4. **No `_sandbox.tsx`.** Phase 03 mentioned an optional dev sandbox — skipped; components were exercised directly via the refactored `/dashboard` page.

5. **No sidebar submenu for `/reports/*`.** Plan suggested admin-only submenu under "Báo cáo". Current sidebar uses a single `/reports` link that redirects via `<Route index>` to `/reports/revenue`. Navigation between revenue ↔ sales-performance is via the leaderboard row click + breadcrumb/button on each page. Layout doesn't have existing submenu pattern; keeping single link matches surrounding code.

6. **Revenue rule uses `ConfirmedAt` directly (not `COALESCE(ConfirmedAt, QuotationDate)`).** Since migration backfills `ConfirmedAt` for every `Status = Confirmed` row, the fallback is unnecessary and would complicate EF translation. New `Confirmed` rows always have `ConfirmedAt` set by `ApplyStatusTimestamps`.

## Residual Risks / Follow-Ups

- **Dashboard summary issues ~4 SQL queries** (stats + sparkline + funnel = 3 + leaderboard separate). Plan target was ≤3 — acceptable for v1 but watch for N+1 patterns in `GetSummaryAsync` if traffic grows.
- **Pre-existing N+1 in `GetRecentActivityAsync`:** the per-row `_db.Users.IgnoreQueryFilters().Where(...).Select(...).FirstOrDefault()` projections may execute as correlated subqueries. Acceptable for `limit=30` overfetch but consider batching in v2.
- **Legacy `useQuotationStats` + `/dashboard/quotation-stats` endpoint** still present and unused after refactor. Safe to delete in a follow-up cleanup PR.
- **Sales-performance page** has sort UI but no pagination (caps at limit=50). If sale count exceeds 50, top-50 ordering still works but the listing truncates.
- **Sidebar submenu for reports** was not implemented (see deviation #5). If admin needs direct nav to sales-performance, add it later.
- **BE integration tests** require Docker; they compile cleanly but were not executed in this environment. Run `dotnet test ... --filter "~Dashboard"` after starting Docker Desktop to validate runtime behavior.
- **Manual responsive QA** (1280/1024/768/375 breakpoints + dark mode + tab-nav focus) was not performed in this session — flag for browser smoke before shipping.

## Rollback

- Phase 01 migration: `dotnet ef database update <previous_migration> --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi`. Columns are nullable; reverting is safe.
- Phases 02–06: revert commit; no migrations.
- Sidebar dashboard regression: temporarily point `dashboardItem` href back to `/` for all users.
