# Phase 03: Frontend Tab & Activity List

## Objective

- Add a `Lịch sử` tab in quotation edit mode and render activity entries newest first with usable loading, empty, and error states.

## Preconditions

- Phase 02 endpoint is available.
- Existing quotation edit screen still loads successfully.

## Tasks

1. Extend frontend quotation types in `frontend/src/features/quotations/types.ts`.
   - Add `QuotationActivityAction` union:
     - `Created`
     - `Updated`
     - `Sent`
     - `Confirmed`
     - `Cancelled`
     - `OwnerTransferred`
     - `Cloned`
   - Add `QuotationActivity` interface matching the API DTO.
2. Extend query keys in `frontend/src/features/quotations/keys.ts`.
   - Add `activities(id: string)`.
3. Extend API in `frontend/src/features/quotations/api.ts`.
   - Add `listActivities: (id: string) => apiGet<QuotationActivity[]>(...)`.
4. Extend hooks in `frontend/src/features/quotations/hooks.ts`.
   - Add `useQuotationActivities(id, enabled)`.
   - Invalidate `activities(id)` after update, transition, transfer owner, and clone when relevant.
5. Update `frontend/src/pages/quotations/quotation-form-page.tsx`.
   - Import `Tabs`, `TabsContent`, `TabsList`, `TabsTrigger`.
   - In mode `new`, keep the existing `Thông tin chung` card without tabs.
   - In mode `edit`, wrap the general information content in tabs:
     - `Thông tin chung`
     - `Lịch sử`
   - Keep the current form fields unchanged inside the general tab.
6. Build a compact activity list inside the history tab.
   - Newest first as returned by API.
   - Show action label, timestamp, actor name, and description.
   - Use semantic icon colors consistent with project standards:
     - created/add: cyan
     - updated/save: blue
     - sent: indigo or cyan
     - confirmed: emerald
     - cancelled: red
     - owner transferred/cloned: violet
   - Empty copy: `Chưa có lịch sử phát sinh sau khi bật tính năng này.`
7. Add state handling.
   - Loading: small spinner or skeleton inside tab.
   - Error: concise error message with retry if simple to add.
   - Disabled query in mode `new`.
8. Check layout behavior with the existing sticky action bar and line item grid.

## Verification

- Commands:
  - `npm run typecheck`
- Manual checks:
  - Create a new quotation screen: no `Lịch sử` tab should appear.
  - Edit quotation with no activities: empty state appears.
  - Edit quotation with activities: activities render newest first.
  - Perform update/send/confirm/cancel/transfer/clone and verify the list refreshes after mutations.
  - Confirm no text overlaps in the tabs/card area on desktop and narrow viewport.
- Expected results:
  - TypeScript passes.
  - Existing form behavior remains unchanged.
  - Activity tab is readable and consistent with existing UI conventions.

## Exit Criteria

- Edit mode shows the two-tab general information card.
- History tab fetches and renders activities correctly.
- New mode remains simple and unchanged.
- Frontend typecheck passes.
