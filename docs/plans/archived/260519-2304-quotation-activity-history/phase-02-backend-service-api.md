# Phase 02: Backend Service & API

## Objective

- Record quotation activities for new actions and expose a read endpoint sorted newest first.

## Preconditions

- Phase 01 is complete.
- `QuotationActivity` is available through `IAppDbContext`.

## Tasks

1. Add DTOs in `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs`.
   - `QuotationActivityDto`
     - `Guid Id`
     - `Guid QuotationId`
     - `QuotationActivityAction Action`
     - `Guid? ActorUserId`
     - `string? ActorName`
     - `DateTimeOffset OccurredAt`
     - `string Description`
     - `string? MetadataJson`
2. Extend `IQuotationService`:
   - `Task<IReadOnlyList<QuotationActivityDto>> ListActivitiesAsync(Guid id, CancellationToken ct = default);`
3. Add a private helper in `QuotationService`.
   - Suggested shape: `AddActivity(Quotation q, QuotationActivityAction action, string description, object? metadata = null)`
   - Use `_currentUser.UserId` for `ActorUserId`.
   - Use `_clock.UtcNow` for `OccurredAt`.
   - Serialize metadata through `System.Text.Json` only when metadata is provided.
4. Record activities after each successful business action:
   - `CreateAsync`: `Created`, description like `Tạo báo giá`.
   - `UpdateAsync`: `Updated`, description like `Cập nhật báo giá`.
   - `TransitionAsync`:
     - `Send`: `Sent`
     - `Confirm`: `Confirmed`
     - `Cancel`: `Cancelled`
   - `TransferOwnerAsync`: `OwnerTransferred`, metadata includes old/new owner ids and optional reason.
   - `CloneAsync`: record `Cloned` on the cloned quotation, metadata includes source quotation id/code.
5. Ensure activities are persisted in the same save operation as the quotation change.
6. Implement `ListActivitiesAsync`.
   - Reuse existing ownership/access check by loading the quotation and applying `EnsureCanAccess`.
   - Query `QuotationActivities` by quotation id.
   - Sort `OccurredAt DESC`.
   - Join actor names from users with `IgnoreQueryFilters()`.
   - Return fallback actor label when user is missing or soft-deleted without a name.
7. Add controller endpoint in `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`.
   - Route: `GET /api/quotations/{id:guid}/activities`
   - Permission: `[HasPermission(Permissions.Quotations.View)]`
   - Response: `ApiResponse<IReadOnlyList<QuotationActivityDto>>`
8. Verify transition paths do not create duplicate activities when validation fails or exceptions are thrown.

## Verification

- Commands:
  - `dotnet build src/OrderMgmt.Application/OrderMgmt.Application.csproj`
  - `dotnet build`
- Manual checks:
  - Call create/update/transition/transfer/clone through the app or API.
  - Call `GET /api/quotations/{id}/activities` and verify newest-first ordering.
  - Verify a user without `quotations.view_all` cannot read activities for another user's quotation.
- Expected results:
  - Each successful business action creates exactly one matching activity.
  - Failed or forbidden actions create no activity.

## Exit Criteria

- Backend endpoint returns activities with actor names.
- Activities are written for all approved event types.
- Existing quotation APIs keep their response shape except for the new endpoint.
