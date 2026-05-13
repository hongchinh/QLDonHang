# Phase 02: General Info Keyboard Handler

## Objective

- Add scoped keyboard handling to `QuotationFormPage` so `Enter`, `Shift+Enter`, and `Ctrl+S` match the requested quotation-entry workflow.

## Preconditions

- Phase 1 is complete.
- `LineItemsGrid` exposes `ensureFirstLineAndFocusProductCode()`.
- Product-code cells have stable IDs.

## Tasks

1. Context: inspect `frontend/src/pages/quotations/quotation-form-page.tsx`.
2. Implement: import `useRef` and the `LineItemsGridHandle` type.
3. Implement: create `const lineItemsGridRef = useRef<LineItemsGridHandle>(null)`.
4. Implement: pass `ref={lineItemsGridRef}` to `<LineItemsGrid form={form} />`.
5. Implement: define an explicit general-info field order:
   - `quotationDate`
   - `deliveryDate`
   - `customerId`
   - `customerName`
   - `deliveryAddress`
   - `deliveryRecipient`
   - `deliveryPhone`
   - `deliveryNote`
   - `internalNote`
6. Implement: add a scoped `handleGeneralInfoKeyDown` on the **Thong tin chung** `CardContent`.
7. Implement: when `Enter` is pressed:
   - Ignore modified `Enter` except `Shift+Enter`.
   - If active target is a `textarea`, do not hijack newline behavior unless no textareas are present.
   - If active target is a combobox with `aria-expanded="true"`, return and preserve autocomplete behavior.
   - Prevent default submit behavior.
   - Move forward or backward based on `shiftKey`.
8. Implement: when forward navigation reaches beyond `internalNote`, call `lineItemsGridRef.current?.ensureFirstLineAndFocusProductCode()`.
9. Implement: add form-level `onKeyDown` or document-scoped form handler for `Ctrl+S`.
   - Only handle when focus is inside the quotation form.
   - `preventDefault()`.
   - Avoid duplicate submits when `submitting` is true.
   - Call `form.handleSubmit(onSubmit)()`.
10. Verify: make sure normal submit button still works.

## Verification

- Commands:
  - `cd frontend && npm run typecheck`
- Expected results:
  - `Enter` in general-info inputs does not submit the form.
  - `Shift+Enter` reverses through the explicit field order.
  - `Ctrl+S` triggers the same save path as the submit button.
  - Customer autocomplete keeps selection behavior while its dropdown is open.

## Exit Criteria

- All requested keyboard behaviors are implemented in `quotation-form-page.tsx`.
- Handler is scoped and does not affect unrelated pages.
- Autocomplete dropdown `Enter` behavior is preserved.
