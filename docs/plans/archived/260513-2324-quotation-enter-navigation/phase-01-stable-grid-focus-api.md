# Phase 01: Stable Grid Focus API

## Objective

- Make the line-items grid expose a deterministic way for the quotation form to focus the first product-code cell, creating an empty row first when the grid has no rows.

## Preconditions

- Current grid row creation remains handled by `LineItemsGrid`.
- Product-code cells are rendered by `ProductTypeaheadCell`.

## Tasks

1. Context: inspect `frontend/src/pages/quotations/components/line-items-grid.tsx` and `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`.
2. Implement: add an exported handle type, e.g. `LineItemsGridHandle`, with `ensureFirstLineAndFocusProductCode(): void`.
3. Implement: convert `LineItemsGrid` to `forwardRef<LineItemsGridHandle, Props>` and use `useImperativeHandle`.
4. Implement: add helper `focusProductCodeAt(index: number)` that focuses `line-product-code-${index}` after render.
5. Implement: update the existing `addLine` helper to optionally focus the newly added row or reuse the same focus helper where appropriate.
6. Implement: add optional `inputId?: string` to `ProductTypeaheadCell` and pass it to the rendered `input` / `Input`.
7. Implement: pass `inputId={`line-product-code-${idx}`}` from `LineItemsGrid` to the product-code cell.
8. Verify: run TypeScript typecheck after the phase or defer to Phase 3 if Phase 2 follows immediately.

## Verification

- Commands:
  - `cd frontend && npm run typecheck`
- Expected results:
  - TypeScript compiles with no new errors.
  - Existing Insert key add-row behavior still focuses the new row's first input.
  - The first product-code cell has a stable DOM ID: `line-product-code-0`.

## Exit Criteria

- `LineItemsGrid` accepts a ref from its parent.
- Parent code can call `ensureFirstLineAndFocusProductCode()`.
- Empty grid call appends one row and focuses row 0 product-code input.
- Non-empty grid call focuses row 0 product-code input without appending.
