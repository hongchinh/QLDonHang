# Add "Thêm mới" and "Sửa" to ProductCatalogDialog

## Goal

Add Create and Edit capability to the existing `ProductCatalogDialog` so users can manage products without leaving the quotation workflow. A new `ProductFormDialog` component is built as a standalone, reusable dialog that will also serve a future `/products` management page. After a successful create, the new product is auto-selected into the quotation line. After a successful edit, the detail panel and list refresh automatically via React Query cache invalidation.

## Scope

- **In scope:**
  - New `ProductFormDialog` component (`features/products/components/product-form-dialog.tsx`) — reusable, self-contained
  - "Thêm mới" button in `ProductCatalogDialog` left-panel header (guarded by `Products.Create` permission)
  - "Sửa" button in `ProductCatalogDetail` right-panel header (guarded by `Products.Update` permission, only shown when a product is selected)
  - Auto-select newly created product (calls `onSelect` + closes catalog dialog)
  - Post-edit cache invalidation (list + detail queries)
  - Unit tests for `ProductFormDialog`
- **Out of scope:**
  - Delete button
  - `/products` management page itself (only the reusable form component is built here)
  - Backend changes (all API endpoints already exist)

## Assumptions

- `Products.Create` and `Products.Update` are valid permission strings recognized by `useAuthStore().hasPermission()`.
- `useProductGroups()` and `useUnits()` return `LookupItem[]` (`{ id, code, name }`).
- `useProduct(id)` result is already in cache when the user clicks "Sửa" (the detail panel fetched it).
- `productSchema`, `ProductFormValues`, `ProductFormParsed` in `schema.ts` are complete and do not need changes.
- The Radix UI `Select` component from `@/components/ui/select` is used for all dropdowns.
- The `ProductCatalogDialog` external Props interface must not change.

## Risks

- Nested Radix dialogs (form dialog on top of catalog dialog): Radix UI supports this via portals; no known issues, but should be manually verified.
- The `useProduct(selectedId)` call in `ProductCatalogDialog` fires unconditionally when `selectedId` is set — this is already happening in `ProductCatalogDetail` so no extra network cost.

## Phases

- [ ] Phase 01 — ProductFormDialog component (M) — `phase-01-product-form-dialog.md`
- [ ] Phase 02 — Wire into ProductCatalogDialog (S) — `phase-02-wire-catalog-dialog.md`

## Final Verification

```bash
cd frontend
npx vitest run src/features/products/components/product-form-dialog.test.tsx
npx tsc --noEmit
```

Manual smoke test:
1. Open a quotation, click a product cell → open catalog → "Thêm mới" button is visible → fill form → save → product auto-fills into the line
2. In catalog, select a product → "Sửa" button appears → edit name → save → detail panel shows updated name
3. As a user without `Products.Create`, verify "Thêm mới" button is absent

## Rollback / Recovery

All changes are frontend-only and additive. Revert by running:
```
git revert HEAD
```
or restore the three edited files from git history.
