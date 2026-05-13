# Implementation Plan: Quotation Form Enter Navigation

> Created: 2026-05-13 23:24:01

## Objective

- Add keyboard navigation for the quotation form so users can use `Enter` and `Shift+Enter` to move through the **Thong tin chung** controls, move from the last general-info field into **Chi tiet hang hoa**, auto-create an empty line when needed, and save the quotation with `Ctrl+S`.
- Preserve existing autocomplete behavior: when customer or product dropdowns are open, `Enter` continues to select the highlighted option; when dropdowns are closed, `Enter` moves focus to the next field.

## Scope

### In scope

- `Enter` moves to the next explicit general-info control.
- `Shift+Enter` moves to the previous explicit general-info control.
- `Enter` on the last general-info control moves to the first product-code cell in the line-items grid.
- If the line-items grid has no rows, the transition from general info to grid appends one empty row and focuses its product-code cell.
- `Ctrl+S` saves the quotation from anywhere inside the quotation form.
- Customer autocomplete keeps current `Enter` / `Shift+Enter` selection behavior while its dropdown is open.
- Product autocomplete keeps current `Enter` / `Shift+Enter` selection behavior while its dropdown is open.
- Both are guarded symmetrically via `e.defaultPrevented` — the same check applies to `Enter` and `Shift+Enter`.
- TypeScript-level verification and focused manual keyboard checks.

### Out of scope

- Changing quotation data model, API payloads, validation schema, backend code, or routing.
- Changing keyboard behavior on unrelated forms.
- Adding new shortcut-discovery UI unless requested separately.
- Reworking the line-items grid navigation beyond the requested first-cell focus handoff.

## Architecture & Approach

- Implement keyboard behavior with two scoped listeners, not one:
  - `Enter` / `Shift+Enter` → `onKeyDown` on the `<CardContent>` of the **Thông tin chung** section only. Attaching to the entire `<form>` would cause general-info navigation to fire when `Enter` is pressed inside **Chi tiết hàng hóa**.
  - `Ctrl+S` → `onKeyDown` on the `<form>` element so it works from anywhere, including the grid.
- Use an explicit field order for general-info navigation to avoid accidental focus on helper buttons such as clear/add-customer controls.
- Add a stable focus target for line product-code cells, e.g. `line-product-code-0`.
- Expose a small imperative focus API from `LineItemsGrid` via `forwardRef` / `useImperativeHandle`, with a method such as `ensureFirstLineAndFocusProductCode()`.
- Add a lightweight `inputId` prop to `ProductTypeaheadCell` so the grid can assign stable IDs without querying arbitrary DOM structure.
- Use `form.handleSubmit(onSubmit)()` (note the double call — `handleSubmit` returns a function) for `Ctrl+S`, called imperatively inside the `onKeyDown` handler with `e.preventDefault()` to suppress the browser Save Page dialog.
- Preserve autocomplete behavior by checking `e.defaultPrevented` at the start of the form-level handler: both `CustomerAutocomplete` and `ProductTypeaheadCell` call `e.preventDefault()` when they own the `Enter` key, so `e.defaultPrevented === true` is the reliable signal that an autocomplete already handled it. Do **not** read `aria-expanded` from the DOM — `CustomerAutocomplete` sets `aria-expanded = isOpen && keyword.length > 0`, which can be `false` while `isOpen` is still `true` (early-return guard differs from the attribute); `ProductTypeaheadCell` leaves `aria-expanded = true` when its dropdown is open but empty and does not stop `Enter` — so `aria-expanded` gives false negatives and false positives in edge cases.

## Phases

- [ ] **Phase 1 [S]: Stable Grid Focus API** - Add stable product-code cell IDs and expose a method to focus/create the first row.
- [ ] **Phase 2 [M]: General Info Keyboard Handler** - Add scoped `Enter` / `Shift+Enter` / `Ctrl+S` handling in the quotation form.
- [ ] **Phase 3 [S]: Verification and Polish** - Run typecheck/tests, manually validate keyboard flows, and update the keyboard shortcut guide in the grid footer to include `Enter` / `Shift+Enter` / `Ctrl+S`.

## Key Changes

- `frontend/src/pages/quotations/quotation-form-page.tsx`
  - Add refs and keyboard handler for the quotation form / general-info section.
  - Wire `Ctrl+S` to save.
  - Wire last general-info field to line-items focus API.
- `frontend/src/pages/quotations/components/line-items-grid.tsx`
  - Convert to `forwardRef` or equivalent imperative handle.
  - Add `ensureFirstLineAndFocusProductCode()`.
  - Assign stable IDs to product-code cells.
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`
  - Add optional `inputId` prop and pass it to the underlying input.
- Optional focused tests if existing test setup makes them practical:
  - `frontend/src/pages/quotations/...` test coverage for focus movement.

## Verification Strategy

- Commands:
  - `cd frontend && npm run typecheck`
  - `cd frontend && npm run test -- --runInBand` if supported by the local Vitest version; otherwise `cd frontend && npm run test`
- Manual checks in browser:
  - In **Thong tin chung**, `Enter` advances through all listed fields.
  - `Shift+Enter` moves backward.
  - `Enter` on `internalNote` focuses first **Ma hang** cell.
  - If no line rows exist, `Enter` on `internalNote` creates one empty row and focuses **Ma hang**.
  - `Ctrl+S` saves the quotation and does not open browser Save Page.
  - Customer dropdown open: `Enter` selects customer, not move focus.
  - Customer dropdown closed: `Enter` moves focus to `customerName`.
  - Product dropdown open: `Enter` selects product, not move to the next general-info field.

## Dependencies

- No new packages.
- Uses existing React, react-hook-form, and DOM focus APIs.

## Risks & Mitigations

- Risk: `Enter` unintentionally submits the form.
  - Mitigation: `preventDefault()` for handled `Enter`, and reserve submit for `Ctrl+S` or existing submit button.
- Risk: `Enter` on `type="date"` inputs (`quotationDate`, `deliveryDate`) interferes with the browser's native date-picker UI — calling `preventDefault()` on these may prevent the user from confirming a date via keyboard.
  - Mitigation: in the Enter handler, skip `preventDefault()` and focus movement when `(e.target as HTMLInputElement).type === 'date'`. Manual test required on Chrome: open date picker with space/F4, confirm with Enter, verify picker closes normally and focus does not jump.
- Risk: Autocomplete selection breaks.
  - Mitigation: check `e.defaultPrevented` at the top of every form-level key handler and return immediately if true — both autocomplete components call `e.preventDefault()` when they own the keystroke.
- Risk: Newly appended line is not focusable immediately due to React render timing.
  - Mitigation: focus after append using `setTimeout(..., 0)` or `requestAnimationFrame`, matching the existing `addLine` pattern.
- Risk: Imperative ref adds coupling between form and grid.
  - Mitigation: keep the API narrow and behavior-specific: `ensureFirstLineAndFocusProductCode()`.

## Open Questions

- None. Requirements were confirmed by the user in the brainstorm conversation.
