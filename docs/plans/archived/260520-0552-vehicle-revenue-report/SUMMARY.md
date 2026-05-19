# Implementation Plan: Vehicle Revenue Report

> Created: 2026-05-20 05:52:05
> Timestamp note: project-preferred `bash date` command was unavailable because WSL has no installed distribution in this environment; equivalent PowerShell timestamp was used.

## Objective

- Add transport vehicle number tracking to quotations.
- Add a dedicated vehicle revenue report page that summarizes revenue by vehicle and compares vehicle revenue over a configurable monthly line chart.
- Preserve current quotation-first revenue semantics: revenue is counted from confirmed, non-cancelled quotations by confirmation time.

## Scope

### In scope

- Add a persisted quotation header field for transport vehicle number.
- Normalize blank vehicle values to `Xe khác`.
- Show the field on `frontend/src/pages/quotations/quotation-form-page.tsx` on the same row as `Người nhận` and `Điện thoại`.
- Add backend API `GET /api/reports/vehicle-revenue`.
- Add frontend page `/reports/vehicle-revenue` with:
  - Date range filters for the summary table.
  - Month count control for the line chart, default `6`.
  - Summary table grouped by vehicle.
  - Line chart comparing vehicle revenue by month.
- Add route permission and sidebar navigation using existing `reports.revenue`.
- Add focused backend/frontend verification.

### Out of scope

- Vehicle master data management.
- Vehicle autocomplete or validation against a fleet list.
- Export to Excel/PDF for the new report.
- Separate permissions beyond existing `reports.revenue`.
- Reworking existing sales revenue or dashboard reports.

## Architecture & Approach

- Store the vehicle number on `Quotation` because the value is part of the quotation header and revenue is already quotation-based.
- Use a simple string field, proposed backend name `TransportVehicleNumber`, max length `50`.
- Normalize empty create/update input to `Xe khác` in application service logic so report grouping is deterministic.
- Keep DB nullable for migration compatibility, but report queries must also coalesce null/blank legacy rows to `Xe khác`.
- Add a dedicated report feature beside existing `Reports/SalesRevenue`, following the same controller/service/DTO/validator pattern.
- Use confirmed quotation logic consistent with `SalesRevenueReportService`: `Status == Confirmed`, `CancelledAt == null`, `ConfirmedAt != null`, and date range by `ConfirmedAt`.
- Use nullable date request fields for the vehicle report (`DateOnly?` preferred, or nullable `DateTime?` if controller binding requires it) so missing `from/to` can be validated explicitly instead of silently becoming `0001-01-01`.
- Keep vehicle normalization and aggregation database-side where practical: project a normalized vehicle key before grouping and avoid loading all matching quotations into memory.
- For chart readability, return only top vehicle series by chart-period revenue, default top `5`, plus `Xe khác` if present. The summary table remains complete.
- Anchor monthly chart range to the selected `to` date month so user context remains consistent when filters change.
- Return a frontend-ready chart contract with explicit `chartVehicles` and `monthlySeries` entries so the Recharts page does not need to infer series names from arbitrary object keys.

## Phases

- [x] **Phase 1 [M]: Persist Vehicle Number On Quotations** - Add the data model, migration, API DTOs, validation, and quotation service mappings.
- [x] **Phase 2 [M]: Add Vehicle Revenue Backend Report** - Add report DTOs, validator, service, DI, controller endpoint, and backend tests.
- [x] **Phase 3 [M]: Add Quotation Form UI Field** - Add frontend quotation schema/types/payload/defaults and place the field beside recipient and phone.
- [x] **Phase 4 [M]: Add Vehicle Revenue Frontend Page** - Add feature API/hooks/types, route/sidebar/permission mapping, table, chart, and frontend verification.

## Key Changes

- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.Application/Reports/VehicleRevenue/`
- `backend/src/OrderMgmt.Application/DependencyInjection.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/ReportsController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Reports/`
- `frontend/src/features/quotations/schema.ts`
- `frontend/src/features/quotations/types.ts`
- `frontend/src/pages/quotations/quotation-form-page.tsx`
- `frontend/src/features/reports/vehicle-revenue/`
- `frontend/src/pages/reports/vehicle-revenue-page.tsx`
- `frontend/src/App.tsx`
- `frontend/src/components/layout/app-layout.tsx`
- `frontend/src/lib/route-permissions.ts`
- `frontend/src/lib/route-permissions.test.ts`

## Verification Strategy

- Backend:
  - `dotnet build`
  - `dotnet test`
  - Targeted integration tests around quotation create/update and vehicle revenue report.
- Frontend:
  - `npm run typecheck`
  - `npm run test`
  - `npm run build`
  - Manual browser check of quotation form layout and report page.

## Dependencies

- No new backend packages expected.
- No new frontend packages expected.
- Reuse `recharts`, which is already present in `frontend/package.json` and used by dashboard chart components.

## Risks & Mitigations

- Migration may affect production data -> keep the DB column nullable and coalesce legacy blanks to `Xe khác` in report queries.
- Chart can become unreadable with many vehicles -> limit chart series to top vehicles while keeping the table complete.
- Inconsistent fallback if only frontend fills `Xe khác` -> normalize in backend create/update and query paths.
- Date interpretation can diverge from sales revenue -> reuse `ConfirmedAt` date semantics from `SalesRevenueReportService`.
- Missing date query values can bind to default dates -> use nullable request fields and explicit validator rules.
- Vehicle grouping can become memory-heavy -> aggregate by normalized vehicle key in the database and only materialize grouped results.
- Form row may become too dense on small screens -> use responsive grid classes so the three fields sit in one row on desktop and wrap on narrow widths.

## Open Questions

- None blocking. Assumptions:
  - Label can be `Số xe`.
  - Stored fallback value is exactly `Xe khác`.
  - Chart default is 6 months and user can change it, with acceptable range 1-24 months.
  - Chart series limit default is 5 top vehicles plus `Xe khác` if present.
