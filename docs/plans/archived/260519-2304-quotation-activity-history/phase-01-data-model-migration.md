# Phase 01: Data Model & Migration

## Objective

- Add persisted storage for quotation activities and wire it into EF Core without changing UI behavior yet.

## Preconditions

- Current backend builds before implementation, except for any known local DLL lock from a running WebApi process.
- The plan scope remains: no backfill and no inferred history for old quotations.

## Tasks

1. Inspect existing sales domain and EF configuration:
   - `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs`
   - `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationOwnerHistory.cs`
   - `backend/src/OrderMgmt.Domain/Enums/Enums.cs`
   - `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs`
2. Add `QuotationActivityAction` enum in `backend/src/OrderMgmt.Domain/Enums/Enums.cs` with values:
   - `Created`
   - `Updated`
   - `Sent`
   - `Confirmed`
   - `Cancelled`
   - `OwnerTransferred`
   - `Cloned`
3. Add `QuotationActivity` domain entity under `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationActivity.cs`.
   - Fields:
     - `Guid QuotationId`
     - `Quotation? Quotation`
     - `QuotationActivityAction Action`
     - `Guid? ActorUserId`
     - `DateTimeOffset OccurredAt`
     - `string Description`
     - `string? MetadataJson`
4. Add `ICollection<QuotationActivity> Activities` to `Quotation` if navigation is useful for EF consistency.
5. Add `DbSet<QuotationActivity> QuotationActivities` to:
   - `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs`
   - `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs`
6. Configure EF in `SalesConfiguration.cs`.
   - table name: `quotation_activities`
   - `Action` conversion to `int`
   - `Description` max length around 300-500 chars
   - `MetadataJson` column type `jsonb` if PostgreSQL json querying may be useful, otherwise `text`
   - FK to `Quotations` with cascade delete aligned with existing soft-delete conventions
   - index on `(QuotationId, OccurredAt DESC)`
   - index on `ActorUserId` if lookup/reporting is useful
7. Generate EF migration under `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations`.
8. Review generated migration for naming, column types, indexes, and no unintended model churn.

## Verification

- Commands:
  - `dotnet build src/OrderMgmt.Application/OrderMgmt.Application.csproj`
  - `dotnet build`
- Expected results:
  - New entity and DbSet compile.
  - Migration creates only the quotation activity table and intended indexes.
  - No production frontend files are changed in this phase.

## Exit Criteria

- `QuotationActivity` domain model exists.
- EF configuration and migration are present.
- Application project builds.
- Full backend build passes, or any failure is documented as unrelated DLL lock from a running process.
