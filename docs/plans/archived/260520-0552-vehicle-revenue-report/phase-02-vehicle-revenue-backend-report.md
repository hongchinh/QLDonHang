# Phase 02: Vehicle Revenue Backend Report

## Objective

- Add `GET /api/reports/vehicle-revenue` returning summary rows and monthly chart series by transport vehicle number.

## Preconditions

- Phase 01 is complete.
- `TransportVehicleNumber` is queryable on `Quotation`.

## Tasks

1. Create `backend/src/OrderMgmt.Application/Reports/VehicleRevenue/Models/VehicleRevenueReportDtos.cs`.
2. Define request fields:
   - `DateOnly? From` preferred, or nullable `DateTime? From` if existing model binding patterns require `DateTime`.
   - `DateOnly? To` preferred, or nullable `DateTime? To` if existing model binding patterns require `DateTime`.
   - `int Months = 6`
   - `int TopVehicles = 5`
3. Define response fields:
   - `From`, `To`, `Months`
   - `Items` grouped by vehicle with quotation count, gross revenue, net revenue.
   - `ChartVehicles`: ordered list of vehicle names included in the chart.
   - `MonthlySeries`: frontend-ready month rows, each with `Month` (`yyyy-MM`) and `Values`.
   - `Values`: list of `{ VehicleNumber, TotalRevenueGross }` for that month.
   - Grand totals.
4. Add `IVehicleRevenueReportService`.
5. Implement `VehicleRevenueReportService` using confirmed, non-cancelled quotations by `ConfirmedAt`.
6. Normalize grouping in query/projection with fallback `Xe khác` for null/blank values.
7. Keep aggregation database-side where practical:
   - Project a normalized vehicle key before grouping.
   - Group and sum in EF/SQL for summary and monthly aggregates.
   - Do not materialize all matching quotations just to normalize vehicle strings.
8. Build summary table from `from/to`.
9. Build monthly chart range anchored to the month of `To`, spanning `Months`.
10. Select top chart vehicles by chart-period gross revenue, default `TopVehicles`, and include `Xe khác` if present.
11. Add request validator:
    - `From` and `To` must be present.
    - `To >= From`.
    - Summary date range max 366 days, matching the existing sales revenue validator unless a stronger reason appears.
    - `Months` between `1` and `24`.
    - `TopVehicles` between `1` and `10`.
12. Register `IVehicleRevenueReportService` in `DependencyInjection.cs`.
13. Register validator if project conventions do not auto-register it.
14. Update `ReportsController` constructor to inject:
    - `IVehicleRevenueReportService`
    - `IValidator<VehicleRevenueReportRequest>`
15. Add endpoint to `ReportsController` with `[HasPermission(Permissions.Reports.Revenue)]`.
16. Add integration tests in `backend/tests/OrderMgmt.IntegrationTests/Reports/VehicleRevenueReportTests.cs`.

## Verification

- Commands:
  - `dotnet build`
  - `dotnet test --filter VehicleRevenue`
  - `dotnet test --filter Reports`
- Expected results:
  - API compiles and endpoint is permission-gated.
  - Tests prove explicit date validation, confirmed-only revenue, fallback `Xe khác`, date filtering, default 6 months, custom months, and top series behavior.

## Exit Criteria

- Endpoint returns deterministic summary and monthly data.
- Response shape is explicit enough for the frontend chart without dynamic contract guessing.
- Legacy null/blank vehicle values are reported as `Xe khác`.
- Existing sales revenue report behavior remains unchanged.
