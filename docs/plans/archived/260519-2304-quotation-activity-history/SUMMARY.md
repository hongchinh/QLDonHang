# Implementation Plan: Quotation Activity History

> Created: 2026-05-19 23:04:23

## Objective

- Add a first-class quotation activity history so users can review who performed key actions and when.
- In edit mode of `frontend/src/pages/quotations/quotation-form-page.tsx`, show `Thong tin chung` and `Lich su` as tabs inside the existing general information card.
- The `Lich su` tab displays activity entries as a list sorted newest first.

## Scope

### In scope

- Add a persisted `QuotationActivity` model for future quotation events.
- Record new activity events after this feature is deployed for:
  - create quotation
  - update quotation
  - send quotation
  - confirm quotation
  - cancel quotation
  - transfer owner from the quotation detail/list flow
  - bulk transfer owner from admin user settings
  - clone quotation
- Add backend API to list activities for a quotation.
- Add frontend quotation activity types, API call, query hook, cache invalidation, and tabbed UI.
- Show loading, empty, and error states in the activity tab.

### Out of scope

- No backfill for old quotations.
- No inferred activity from existing audit fields.
- No detailed field-level diff of update changes.
- No export/print changes.
- No separate activity management screen.

## Architecture & Approach

- Follow the existing Clean Architecture flow:
  - Domain entity under `OrderMgmt.Domain`.
  - `DbSet` and EF configuration through `OrderMgmt.Infrastructure`.
  - DTO and service method under `OrderMgmt.Application`.
  - Thin controller endpoint in `OrderMgmt.WebApi`.
  - Frontend query and API wiring under `frontend/src/features/quotations`.
- Persist activities in a new table rather than overloading `Quotation` audit fields.
- Activity listing uses the same access rules as quotation detail: the caller must be able to view the quotation.
- Activities are sorted by `OccurredAt DESC` so newest entries appear first.
- User display names should be resolved from `Users.IgnoreQueryFilters()` so soft-deleted users can still be shown. Fallback to a neutral label when unavailable.
- Old quotations will show an empty activity state until new actions happen after deployment.
- Activity writes must happen before the same `SaveChangesAsync` call as the business change. If the business operation rolls back, the activity must not survive on its own.

### Activity data contract

`QuotationActivity` should store only stable audit facts needed by the history UI and future reporting:

- `Id`: primary key from `BaseEntity`.
- `QuotationId`: required FK to the quotation whose history is being viewed.
- `Type`: `QuotationActivityType` enum with `Created`, `Updated`, `Sent`, `Confirmed`, `Cancelled`, `OwnerTransferred`, and `Cloned`.
- `ActorUserId`: nullable `Guid`; normally the current user, nullable only for system/future automated events.
- `OccurredAt`: `DateTimeOffset` from `IDateTime.UtcNow`.
- `OldStatus` / `NewStatus`: nullable `QuotationStatus` for transition events.
- `OldOwnerUserId` / `NewOwnerUserId`: nullable owner ids for transfer events, including bulk transfer.
- `SourceQuotationId`: nullable source quotation id for clone events.
- `Reason`: nullable text, populated from transfer reason or future action notes.

`QuotationActivityDto` should expose those ids plus resolved display fields:

- `Type`, `OccurredAt`, and optional metadata above.
- `ActorDisplayName`, `OldOwnerDisplayName`, `NewOwnerDisplayName`, and `SourceQuotationCode`.
- Use `Users.IgnoreQueryFilters()` for user display names and fallback to `Nguoi dung khong xac dinh` when no user is found.
- Do not include cost, line detail, or field-level diffs.

## Phases

- [x] **Phase 1 [M]: Data Model & Migration** - Add persisted quotation activity storage and EF wiring.
- [x] **Phase 2 [M]: Backend Service & API** - Record activities in quotation workflows, including bulk transfer, and expose listing endpoint.
- [x] **Phase 3 [M]: Frontend Tab & Activity List** - Add API/query/types/cache invalidation and render the edit-mode history tab.

## Key Changes

- Backend domain:
  - `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationActivity.cs`
  - `backend/src/OrderMgmt.Domain/Enums/Enums.cs`
- Backend application:
  - `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs`
  - `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/QuotationBulkTransferService.cs`
  - `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`
  - `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`
  - `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- Backend infrastructure/API:
  - `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs`
  - `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs`
  - `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/*`
  - `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`
- Frontend:
  - `frontend/src/features/quotations/types.ts`
  - `frontend/src/features/quotations/api.ts`
  - `frontend/src/features/quotations/hooks.ts`
  - `frontend/src/features/quotations/keys.ts`
  - `frontend/src/pages/quotations/quotation-form-page.tsx`

## Backend Implementation Notes

- Add `DbSet<QuotationActivity> QuotationActivities` to `IAppDbContext` and `AppDbContext`.
- Configure the table in `SalesConfiguration.cs`:
  - table name `quotation_activities`
  - required `QuotationId`, `Type`, and `OccurredAt`
  - enum conversions as integers to match quotation status conventions
  - `Reason` max length 500
  - indexes on `{ QuotationId, OccurredAt }` and `ActorUserId`
  - delete behavior `Restrict`; quotation deletion is soft-delete based
- Add a private helper in `QuotationService` such as `AddActivity(...)` and call it before each successful `SaveChangesAsync` path for create, update, transition, transfer owner, and clone.
- For create and clone retry loops, detach the activity together with the quotation and lines if a unique-code retry occurs.
- In `QuotationBulkTransferService.TransferAllAsync`, add one `OwnerTransferred` activity per affected quotation in the same loop that writes `QuotationOwnerHistory`.
- Add `ListActivitiesAsync(Guid quotationId, CancellationToken ct)` to `IQuotationService`; first load the quotation and call the existing access guard, then return activities ordered by `OccurredAt DESC`.
- Add `GET /api/quotations/{id:guid}/activities` guarded by `[HasPermission(Permissions.Quotations.View)]`.

## Frontend Implementation Notes

- Add `QuotationActivity`, `QuotationActivityType`, and related metadata fields to `frontend/src/features/quotations/types.ts`.
- Add `quotationsApi.listActivities(id)` mapped to `/quotations/{id}/activities`.
- Add `quotationKeys.activities(id)` and `useQuotationActivities(id, enabled)`.
- Invalidate `quotationKeys.activities(id)` after update, transition, and transfer owner mutations because the activity tab can already be mounted on the edit page.
- After create and clone, list invalidation is sufficient for the source screen, but the destination edit page should fetch activities using its own detail id.
- Render the tab only in edit mode. Keep the existing general information fields in the `Thong tin chung` tab and put loading, error, empty, and newest-first activity list states in the `Lich su` tab.

## Verification Strategy

- Backend:
  - Add integration/service coverage for:
    - create writes `Created`
    - update writes `Updated`
    - send/confirm/cancel write the correct transition activity once after validation succeeds
    - single transfer and bulk transfer write `OwnerTransferred` with old/new owner metadata
    - clone writes `Cloned` on the cloned quotation with `SourceQuotationId`
    - list endpoint enforces the same owner scoping as quotation detail
    - listing sorts by `OccurredAt DESC`
    - soft-deleted actor/owner names resolve through `IgnoreQueryFilters()`
  - `dotnet build src/OrderMgmt.Application/OrderMgmt.Application.csproj`
  - `dotnet build`
  - Manual API checks only for flows not covered by automated tests.
- Frontend:
  - `npm run typecheck`
  - Add a targeted component/query test if the project already has a nearby quotation-page test harness; otherwise document the manual UI check result in the execution report.
  - Manual UI checks in quotation edit mode.
- Manual flows:
  - Create a quotation and verify the new quotation has `Created`.
  - Edit it and verify `Updated`.
  - Send, confirm, cancel and verify corresponding events.
  - Transfer owner and verify actor/metadata.
  - Bulk transfer quotations and verify each affected quotation has `OwnerTransferred`.
  - Clone and verify the cloned quotation has a `Cloned` activity.
  - Open an old quotation with no events and verify the empty state.

## Dependencies

- No new packages expected.
- Requires EF Core migration generation in the backend project.

## Risks & Mitigations

- Activity write failures could block business actions -> write activities inside the same transaction/save path so the operation remains consistent; keep activity payload simple.
- Bulk transfer could silently bypass activity history -> add activity creation to `QuotationBulkTransferService` in the same loop as `QuotationOwnerHistory`.
- Activity tab can show stale data after in-page mutations -> add `quotationKeys.activities(id)` and invalidate it from update, transition and transfer owner mutations.
- Ambiguous metadata could make events hard to render later -> use the explicit activity data contract above and keep free-form text limited to `Reason`.
- Soft-deleted users might display blank actor names -> resolve users with `IgnoreQueryFilters()` and provide fallback text.
- Existing quotations have no history -> use explicit empty copy: `Chua co lich su phat sinh sau khi bat tinh nang nay.`
- Duplicate or misleading events during status transition -> record one activity per action branch after status validation succeeds.
- Migration folder inconsistency exists in the repo -> place the new migration under `Infrastructure/Persistence/Migrations`, matching current documented convention.

## Open Questions

- None for the approved scope.
