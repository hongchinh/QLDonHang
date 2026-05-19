# Phase 03: Quotation Form UI Field

## Objective

- Add the vehicle number input to the quotation form and frontend quotation contracts.

## Preconditions

- Phase 01 backend contract is available.

## Tasks

1. Add `transportVehicleNumber?: string` to `Quotation` and `UpsertQuotationRequest` in `frontend/src/features/quotations/types.ts`.
2. Add `transportVehicleNumber` to `quotationSchema` in `frontend/src/features/quotations/schema.ts` with max length `50`.
3. Add the field to `toFormDefaults` in `quotation-form-page.tsx`.
4. Add the field to `toPayload`.
5. Add `transportVehicleNumber` to `GENERAL_INFO_FIELD_ORDER` after `deliveryPhone`.
6. Update the delivery recipient row to include three fields in one desktop row:
   - `Người nhận`
   - `Điện thoại`
   - `Số xe`
7. Ensure responsive layout wraps cleanly on smaller viewports.
8. Keep blank input allowed in UI; backend owns fallback normalization.

## Verification

- Commands:
  - `npm run typecheck`
  - `npm run test`
  - `npm run build`
- Manual checks:
  - Create quotation with `Số xe` filled.
  - Create quotation with `Số xe` blank and confirm quotation detail/API shows `Xe khác`.
  - Edit quotation and update `Số xe`.
  - Tab/Enter focus order moves through the new field.
- Expected results:
  - No TypeScript errors.
  - Form submits the new property.
  - Layout does not overlap on desktop or mobile.

## Exit Criteria

- UI displays `Số xe` on the same desktop row as recipient and phone.
- Form create/update preserves vehicle value.
- Blank input remains valid.
