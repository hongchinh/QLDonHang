# Phase 09 — Integration tests

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Lock the behavior we care about with integration tests against a real Postgres (Testcontainers, already configured in the test project). Cover the recompute correctness across the four pricing modes, state-machine guards, role/permission matrix, and soft-delete cascade.

## Files
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationCrudTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationRecomputeTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationStateMachineTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationPermissionTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationSoftDeleteCascadeTests.cs` (new)

(If the test project uses `IClassFixture<...>` or `CollectionFixture`, follow the existing pattern in the project. New files only — do not touch existing fixtures unless an assertion helper is missing.)

## Tasks

1. **CRUD smoke** (`QuotationCrudTests`):
   - Seed a customer + product (`PricingMode.PerUnit`) per test (or use shared fixture seeds if any).
   - POST `/api/quotations` with 1 line; assert 200 + `code` matches `BG-YYMMDD-0001`.
   - GET `/api/quotations/{id}`; assert lines snapshotted (productName == product.Name at create time).
   - PUT with modified line price; GET; assert recomputed `Total`.
   - DELETE; GET → 404.

2. **Recompute correctness** (`QuotationRecomputeTests`):
   - Case `PerUnit`: qty 5, unitPrice 12000 → lineTotal 60000.
   - Case `PerSquareMeter`: client supplies qty 8 (m²) and unitPrice 50000 → lineTotal 400000. (Backend does not derive qty from dimensions; it trusts client `Quantity`.)
   - Case `PerLinearMeter`: qty 10 (m), unitPrice 25000 → lineTotal 250000.
   - Case `PerCubicMeter`: qty 0.5 (m³), unitPrice 1_000_000 → lineTotal 500000.
   - Totals math: subtotal 100k, discount 10k, freight 5k, taxRate 8 → taxAmount 8000, total 103000.
   - Profit math: unitCost 8000 on a qty-5 line → lineCost 40000, lineProfit 20000.
   - Rounding: ensure taxAmount rounded to integer (e.g. subtotal 123 × 8% = 9.84 → expect 10).

3. **State machine** (`QuotationStateMachineTests`):
   - Allowed: Draft→Sent, Sent→Confirmed, Draft→Cancelled, Sent→Cancelled, Confirmed→Cancelled.
   - Forbidden: Draft→Confirmed (no skipping), Cancelled→Sent, Confirmed→Sent, ConvertedToOrder→Sent (unreachable yet but assert if reachable via service injection). Each returns 400 `DomainException` payload.
   - Update on a Cancelled quotation: PUT returns 400 with "đã hủy" message.

4. **Permission matrix** (`QuotationPermissionTests`):
   - Login as SALES role token; POST/PUT allowed; DELETE returns 403; GET allowed; transition allowed.
   - Login as ACCOUNTANT; GET allowed; POST/PUT/DELETE 403.
   - Login as WAREHOUSE; only `orders.*` perms — GET quotations returns 403 (or 200 depending on whether `quotations.view` is in their bundle; based on `DbSeeder.SeedRolesAsync`, Warehouse does NOT have `quotations.view` → expect 403).
   - Login as ADMIN; all actions succeed.

5. **Soft-delete cascade** (`QuotationSoftDeleteCascadeTests`):
   - Create quotation with 2 lines; DELETE quotation; query `_db.QuotationLines.IgnoreQueryFilters().Where(l => l.QuotationId == id)`; assert both `IsDeleted == true` and `DeletedAt`/`DeletedBy` set.
   - Restore-by-flipping bypassed: confirm `GET /quotations/{id}` returns 404 after delete (query filter hides it).

## Verification
```
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo
```

## Exit Criteria
- All five new test classes pass against the Testcontainers Postgres.
- Total project test count > previous baseline by the new test count; previous tests still pass.
- Test names follow the existing project's naming convention (`Method_State_Expectation` or `Describe_Should_When`).
- No flakes in 2 consecutive runs.
