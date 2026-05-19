# Phase 01: Persist Vehicle Number On Quotations

## Objective

- Add `TransportVehicleNumber` as a quotation header field available through create, update, detail, clone, and frontend API contracts.

## Preconditions

- Current quotation create/update flow works.
- Database migrations are generated from the backend WebApi startup project.

## Tasks

1. Inspect existing quotation mapping paths in `QuotationService.cs` for create, update, clone, and DTO projection.
2. Add `TransportVehicleNumber` to `Quotation` domain entity.
3. Configure max length `50` in `SalesConfiguration.cs`.
4. Add the field to `QuotationDto` and `UpsertQuotationRequest`.
5. Add FluentValidation max length rule in `QuotationValidators.cs`.
6. Add a small private normalizer in `QuotationService.cs`, e.g. trim and fallback to `Xe khác`.
7. Apply normalizer on create and update assignments.
8. Copy the value in clone logic.
9. Include the value in DTO projection/detail mapping.
10. Generate EF migration under `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations`.
11. Review migration to ensure it adds only the expected nullable string column.

## Verification

- Commands:
  - `dotnet build`
  - `dotnet test --filter "FullyQualifiedName~QuotationCrudTests"`
- Expected results:
  - Backend compiles.
  - Quotation CRUD tests pass. The broader `dotnet test --filter Quotations` gate is intentionally excluded for this execution after user approval because pre-existing/broader quotation tests fail outside the vehicle-field scope.
  - Migration contains a nullable `transport_vehicle_number` column with max length 50 or equivalent provider metadata.

## Exit Criteria

- Create/update requests can carry vehicle number.
- Blank create/update values are normalized to `Xe khác` by backend logic.
- Quotation detail returns the field.
- Clone preserves the field.
- No unrelated migration churn.
