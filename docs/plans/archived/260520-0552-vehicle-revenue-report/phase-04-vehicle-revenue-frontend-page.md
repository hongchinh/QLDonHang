# Phase 04: Vehicle Revenue Frontend Page

## Objective

- Add the dedicated vehicle revenue report page with filters, summary table, and monthly line chart.

## Preconditions

- Phase 02 endpoint is available.
- Phase 03 frontend quotation types are updated.

## Tasks

1. Create `frontend/src/features/reports/vehicle-revenue/types.ts`.
2. Create `frontend/src/features/reports/vehicle-revenue/api.ts` calling `/reports/vehicle-revenue`.
3. Create `frontend/src/features/reports/vehicle-revenue/keys.ts`.
4. Create `frontend/src/features/reports/vehicle-revenue/hooks.ts`.
5. Create `frontend/src/pages/reports/vehicle-revenue-page.tsx`.
6. Model the chart response contract explicitly:
   - `chartVehicles: string[]`
   - `monthlySeries: { month: string; values: { vehicleNumber: string; totalRevenueGross: number }[] }[]`
   - Transform this response locally into Recharts row data keyed by vehicle names.
7. Add filters:
   - `from`, default start of current month.
   - `to`, default today.
   - `months`, default `6`, user-editable with sensible min/max.
8. Render summary table:
   - `Số xe`
   - `Số báo giá`
   - `Doanh thu (gồm thuế)`
   - `Doanh thu thuần`
   - footer totals.
9. Render line chart comparing monthly gross revenue by vehicle.
10. Reuse Recharts, which is already present in `frontend/package.json` and used by dashboard chart components.
11. Add empty, loading, and error states consistent with existing report pages.
12. Add route import and `/reports/vehicle-revenue` route in `App.tsx` behind `reports.revenue`.
13. Add sidebar item in `app-layout.tsx` with label `Doanh thu xe`.
14. Add route permission mapping in `route-permissions.ts`.
15. Extend route permission tests for `/reports/vehicle-revenue`.

## Verification

- Commands:
  - `npm run typecheck`
  - `npm run test`
  - `npm run build`
- Manual checks:
  - Sidebar shows `Doanh thu xe` only for users with `reports.revenue`.
  - Page loads with default 6-month chart.
  - Changing month count refreshes chart data.
  - Summary table uses selected date range.
  - Blank vehicle quotations appear under `Xe khác` in the report table and chart when applicable.
  - Chart and table handle no data without layout breakage.
- Expected results:
  - Frontend builds cleanly.
  - Existing route permission tests pass with the new path.

## Exit Criteria

- `/reports/vehicle-revenue` is navigable and permission protected.
- User can change chart month count.
- Summary table and line chart render from the new endpoint.
- No regressions in existing report routes.
