# Phase 01: Cell Identity and Empty Row Factory

## Objective

- Prepare `LineItemsGrid` for deterministic keyboard navigation by adding stable cell IDs and centralizing empty-line creation.

## Preconditions

- `frontend/src/pages/quotations/components/line-items-grid.tsx` exists and owns `useFieldArray`.
- `ProductTypeaheadCell` already supports an `inputId` prop.

## Tasks

1. Context: inspect `frontend/src/pages/quotations/components/line-items-grid.tsx`.
2. Implement: add a `LINE_FOCUS_FIELDS` constant with this exact order:
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
3. Implement: add a `LineFocusField` type from `LINE_FOCUS_FIELDS[number]`.
4. Implement: add helper `getLineCellId(field: LineFocusField, idx: number): string`.
5. Implement: add helper `createEmptyLine(sortOrder: number): QuotationLineFormValues`.
6. Implement: replace duplicated empty-line object literals in `addLine()` and `ensureFirstLineAndFocusProductCode()` with `createEmptyLine(...)`.
7. Implement: assign IDs to all editable line inputs using `getLineCellId(...)`.
8. Implement: keep the existing `line-product-code-${idx}` ID contract only if needed by existing parent code, or update `ensureFirstLineAndFocusProductCode()` to use `getLineCellId('product-code', 0)` consistently.

## Verification

- Commands:
  - `cd frontend && npm run typecheck`
- Expected results:
  - TypeScript passes.
  - No behavior change yet beyond stable IDs and refactored empty-line creation.

## Exit Criteria

- All editable grid cells in the planned navigation order have stable IDs.
- Empty-line creation is defined in one helper.
- Existing add-row behavior still compiles.
