# Implementation Plan: Quotation Grid Enter Navigation

> Created: 2026-05-13 23:51:22

## Objective

- Add row-major keyboard navigation inside **Chi tiet hang hoa** on the quotation form.
- `Enter` moves left-to-right through editable cells in the current line, then moves to the next line's **Ma hang** cell.
- If the user presses `Enter` on the last editable cell of the last line, the grid appends one empty line and focuses the new line's **Ma hang** cell.
- `Shift+Enter` moves backward through the same order.
- Preserve product autocomplete behavior: when `ProductTypeaheadCell` owns `Enter`, it selects the product and the grid navigation must not also move focus.

## Scope

### In scope

- Implement row-major `Enter` / `Shift+Enter` navigation inside `LineItemsGrid`.
- Add stable IDs for all editable grid cells used by the navigation.
- Reuse a shared empty-line factory for manual add and automatic add-at-end.
- Keep existing shortcuts working:
  - `Insert` adds a row.
  - `Ctrl+Delete` deletes the active row.
  - `Ctrl+S` saves the quotation.
- Skip readonly/action cells in the Enter navigation:
  - `Loai`
  - `Thanh tien`
  - Delete row button
- Keep `ProductTypeaheadCell` autocomplete `Enter` selection behavior.
- Update the grid shortcut guide wording if needed.

### Out of scope

- Changing quotation payloads, schema, validation, totals calculation, backend API, or product search behavior.
- Implementing column-wise spreadsheet navigation.
- Adding new packages.
- Changing keyboard behavior on unrelated forms.

## Architecture & Approach

- Keep the navigation inside `frontend/src/pages/quotations/components/line-items-grid.tsx`, because the grid owns row indexes, fields, append/remove behavior, and cell layout.
- Use explicit field ordering instead of DOM order:
  - `product-code`
  - `name`
  - `unit`
  - `length`
  - `width`
  - `thickness`
  - `sheet-count`
  - `quantity`
  - `unit-price`
  - `unit-cost`
- Add stable cell IDs with a predictable pattern such as `line-${fieldKey}-${idx}`.
- Parse focused cell IDs to determine `{ fieldKey, rowIndex }`, then compute the next focus target.
- Move the grid keyboard handling from native `addEventListener` to a React `onKeyDown` handler on the grid wrapper. This keeps event ordering predictable: child React handlers such as `ProductTypeaheadCell` run first, then the grid wrapper can inspect `e.defaultPrevented`.
- Use `e.defaultPrevented` as the first guard in the React grid key handler. `ProductTypeaheadCell` calls `preventDefault()` when it handles `Enter` for dropdown selection, so the parent grid should return immediately in that case.
- Preserve `Insert` and `Ctrl+Delete` by porting the existing wrapper-scoped shortcut logic into the same React grid key handler.
- For recognized grid cells, call `preventDefault()` before moving focus or deciding a boundary no-op. This includes `Shift+Enter` on the first row's **Ma hang** cell, which should do nothing but must not submit the form or trigger browser defaults.
- Parse focused cell IDs with one anchored helper, for example `^line-(.+)-(\\d+)$`, then validate the parsed field key against `LINE_FOCUS_FIELDS`. Do not parse IDs with naive `split('-')` because several field keys contain hyphens.
- Append and focus after render with the existing `setTimeout(..., 0)` pattern already used in `LineItemsGrid`.
- Avoid moving navigation logic to `quotation-form-page.tsx`; the form page should remain responsible only for section-level wiring and save behavior.

## Phases

- [x] **Phase 1 [S]: Cell Identity and Empty Row Factory** - Add stable IDs and centralize empty row creation.
- [x] **Phase 2 [M]: Row-Major Enter Navigation** - Implement scoped grid `Enter` / `Shift+Enter` movement and auto-add-at-end.
- [x] **Phase 3 [S]: Verification and Guide Polish** - Verify lint/typecheck/tests/build and confirm manual keyboard checklist.

## Key Changes

- `frontend/src/pages/quotations/components/line-items-grid.tsx`
  - Add focus-field constants and cell ID helpers.
  - Add IDs to all editable grid inputs.
  - Replace the native wrapper `keydown` listener with a React wrapper `onKeyDown` handler.
  - Add row-major Enter handler inside that React handler.
  - Reuse shared empty-line factory for add and auto-add.
  - Keep existing Insert/Ctrl+Delete behavior in the React handler.
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`
  - No behavior change expected; existing `inputId` support and `preventDefault()` on handled autocomplete `Enter` should be enough once the parent grid uses React event bubbling.
- `frontend/src/pages/quotations/quotation-form-page.tsx`
  - No expected production change for this plan.

## Verification Strategy

- Commands:
  - `cd frontend && npm run lint`
  - `cd frontend && npm run typecheck`
  - `cd frontend && npm run test`
  - `cd frontend && npm run build`
- Automated checks if supported by the existing frontend test stack:
  - Product dropdown open with a highlighted item: pressing `Enter` selects the product and does not advance twice.
  - Pressing `Enter` on the last editable cell appends exactly one row and focuses the new row's `Ma hang`.
  - Pressing `Shift+Enter` on the first row's `Ma hang` prevents default behavior and does not submit the form.
- Manual checks:
  - `Enter` moves: `Ma hang -> Ten hang -> DVT -> Dai -> Rong -> Day -> Tam -> SL -> Don gia -> Gia von`.
  - `Enter` on `Gia von` moves to next row `Ma hang`.
  - `Enter` on last row `Gia von` appends one row and focuses new `Ma hang`.
  - `Shift+Enter` moves backward through the same order.
  - `Shift+Enter` on row 2 `Ma hang` moves to row 1 `Gia von`.
  - `Shift+Enter` on row 1 `Ma hang` does nothing and does not submit the form.
  - Product dropdown open with selectable item: `Enter` selects product and does not double-advance.
  - Product dropdown closed or not handling `Enter`: grid navigation advances.
  - `Insert`, `Ctrl+Delete`, and `Ctrl+S` still work.

## Dependencies

- No new packages.
- Uses existing React event handling, `useFieldArray`, and DOM focus APIs.

## Risks & Mitigations

- Risk: Product autocomplete selection double-advances focus.
  - Mitigation: implement the parent grid handler as a React `onKeyDown` wrapper handler and return when `e.defaultPrevented` is true.
- Risk: Auto-added line is not focusable immediately.
  - Mitigation: append first, then focus with `setTimeout(..., 0)`, matching existing grid behavior.
- Risk: ID parsing becomes fragile.
  - Mitigation: define cell ID helpers and one anchored parse helper in one place; validate parsed field keys against `LINE_FOCUS_FIELDS`; do not hand-build IDs in multiple formats.
- Risk: Boundary `Shift+Enter` falls through to form/default behavior.
  - Mitigation: for any recognized grid cell Enter/Shift+Enter event, call `preventDefault()` before moving focus or returning for a boundary no-op.
- Risk: Row indexes shift after delete.
  - Mitigation: IDs are derived from render indexes, so they remain consistent after React re-renders.
- Risk: Handler captures non-grid inputs or action buttons.
  - Mitigation: only handle target IDs that parse to known `LINE_FOCUS_FIELDS`.

## Open Questions

- None. User confirmed the row-major approach during brainstorm.
