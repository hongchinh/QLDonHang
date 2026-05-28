# Phase 02 — Wire into ProductCatalogDialog

**Status:** [ ] pending
**Complexity:** S

## Objective

Wire `ProductFormDialog` into the catalog dialog flow:
- Add "Thêm mới" button to the left-panel header (create mode, permission-gated)
- Add "Sửa" button to the right-panel header (edit mode, permission-gated, only when a product is selected)
- After create: auto-select the new product (calls `onSelect` + closes catalog)
- After edit: React Query cache invalidation already handled by `useUpdateProduct` — no extra work needed

## Files

- `frontend/src/features/products/components/product-catalog-detail.tsx` (update)
- `frontend/src/features/products/components/product-catalog-dialog.tsx` (update)

## Tasks

### Task 2.1 — Update ProductCatalogDetail: add "Sửa" button

Edit `frontend/src/features/products/components/product-catalog-detail.tsx`:

1. Add `onEdit?: () => void` to the `Props` interface:
   ```tsx
   interface Props {
     productId: string | null;
     onSelect: (product: ProductSuggestion) => void;
     onEdit?: () => void;
   }
   ```

2. Add permission check near the top of the component (after the existing `canViewCost` line):
   ```tsx
   const canEdit = useAuthStore((s) => s.hasPermission('Products.Update'));
   ```

3. Update the right-panel header section (the `<div className="border-b px-4 py-3 ...">` that currently contains only `<h2>Chi tiết sản phẩm</h2>`). Replace the `<h2>` wrapper content so the header shows an "Sửa" button when appropriate:

   **Before (lines 70–72 of product-catalog-detail.tsx):**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
     <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
   </div>
   ```

   **After:**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
     <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
     {canEdit && onEdit && productId && (
       <Button variant="outline" size="sm" onClick={onEdit}>
         Sửa
       </Button>
     )}
   </div>
   ```

   > This header is rendered by `ProductCatalogDialog`, which wraps `ProductCatalogDetail` inside its own right-panel header div. Looking at `product-catalog-dialog.tsx` lines 69–79: the right panel has its own `<div className="border-b px-4 py-3 ...">` header with `h2` and `ProductCatalogDetail` is inside a separate inner div. The "Sửa" button should go in that outer header div, NOT inside `ProductCatalogDetail`. Therefore, the cleaner placement is in `ProductCatalogDialog` itself (see Task 2.2). Skip adding the button to `ProductCatalogDetail` — just add the `onEdit` prop to its interface (it will be unused in the detail component itself; the button is rendered at the dialog level).

   **Revised approach for Task 2.1** — Only add the `onEdit` prop to the interface; the button itself is rendered in `ProductCatalogDialog` (Task 2.2). No JSX changes needed in `product-catalog-detail.tsx`.

   Updated `Props` interface in `product-catalog-detail.tsx`:
   ```tsx
   interface Props {
     productId: string | null;
     onSelect: (product: ProductSuggestion) => void;
     onEdit?: () => void;
   }
   ```
   The `onEdit` prop is kept for potential future use; it is not wired to a button inside this component.

4. Run type check: `cd frontend && npx tsc --noEmit`
   Expected: No new errors (adding an optional prop is non-breaking)

### Task 2.2 — Update ProductCatalogDialog: buttons + form wiring

Edit `frontend/src/features/products/components/product-catalog-dialog.tsx`:

1. Add new imports at the top:
   ```tsx
   import { ProductFormDialog } from './product-form-dialog';
   import { useProduct } from '@/features/products/hooks';
   import { useAuthStore } from '@/stores/auth-store';
   import { Plus, Pencil } from 'lucide-react';
   import { Button } from '@/components/ui/button';
   ```

2. Add new state after the existing state declarations (after line 18):
   ```tsx
   const [formOpen, setFormOpen] = useState(false);
   const [formMode, setFormMode] = useState<'create' | 'edit'>('create');
   ```

3. Add permission checks and selected-product fetch after state declarations:
   ```tsx
   const canCreate = useAuthStore((s) => s.hasPermission('Products.Create'));
   const canEdit = useAuthStore((s) => s.hasPermission('Products.Update'));
   const { data: selectedProduct } = useProduct(selectedId ?? undefined);
   ```

4. Add handler functions before `return`:
   ```tsx
   function handleOpenCreate() {
     setFormMode('create');
     setFormOpen(true);
   }

   function handleOpenEdit() {
     setFormMode('edit');
     setFormOpen(true);
   }

   function handleCreated(product: import('@/features/products/types').Product) {
     handleSelect({
       id: product.id,
       code: product.code,
       name: product.name,
       specification: product.specification,
       unitName: product.unitName,
       pricingMode: product.pricingMode,
       defaultPrice: product.defaultPrice,
       costPrice: product.costPrice,
     });
   }
   ```

5. Update the left-panel header (lines 49–51) to include the "Thêm mới" button:

   **Before:**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0">
     <h2 className="text-base font-semibold">Danh mục hàng hóa</h2>
   </div>
   ```

   **After:**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
     <h2 className="text-base font-semibold">Danh mục hàng hóa</h2>
     {canCreate && (
       <Button variant="outline" size="sm" onClick={handleOpenCreate}>
         <Plus className="h-4 w-4 mr-1" />
         Thêm mới
       </Button>
     )}
   </div>
   ```

6. Update the right-panel header (lines 70–72) to include the "Sửa" button:

   **Before:**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
     <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
   </div>
   ```

   **After:**
   ```tsx
   <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
     <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
     {canEdit && selectedId && (
       <Button variant="outline" size="sm" onClick={handleOpenEdit}>
         <Pencil className="h-4 w-4 mr-1" />
         Sửa
       </Button>
     )}
   </div>
   ```

7. Add `ProductFormDialog` just before the closing `</Dialog>` tag (after `</DialogContent>` on line 81):

   **Before:**
   ```tsx
       </DialogContent>
     </Dialog>
   );
   ```

   **After:**
   ```tsx
       </DialogContent>

       <ProductFormDialog
         open={formOpen}
         onOpenChange={setFormOpen}
         initial={formMode === 'edit' && selectedProduct
           ? {
               id: selectedProduct.id,
               code: selectedProduct.code,
               name: selectedProduct.name,
               status: selectedProduct.status,
               pricingMode: selectedProduct.pricingMode,
               specification: selectedProduct.specification,
               unitName: selectedProduct.unitName,
               defaultPrice: selectedProduct.defaultPrice,
               costPrice: selectedProduct.costPrice,
             }
           : undefined
         }
         onCreated={handleCreated}
       />
     </Dialog>
   );
   ```

   > `ProductFormDialog.initial` expects `ProductListItem`. `selectedProduct` is of type `Product` (full detail). The inline object maps `Product` → `ProductListItem` shape — all required fields (`id`, `code`, `name`, `status`, `pricingMode`) are present on `Product`.

8. Run type check: `cd frontend && npx tsc --noEmit`
   Expected: No errors

9. Commit:
   ```
   git add frontend/src/features/products/components/product-catalog-dialog.tsx \
           frontend/src/features/products/components/product-catalog-detail.tsx
   git commit -m "feat: add Thêm mới and Sửa buttons to ProductCatalogDialog"
   ```

## Verification

```bash
cd frontend
npx tsc --noEmit
npx vitest run src/features/products/
```

Manual smoke test (run dev server with `cd frontend && npm run dev`):
1. Open quotation → click product cell → open catalog dialog
2. **Create test:** Click "Thêm mới" → form opens → fill Tên hàng, chọn Nhóm, ĐVT, Loại tính giá → click "Tạo mới" → toast "Đã tạo hàng hóa" → product auto-fills in quotation line → catalog dialog closes
3. **Edit test:** Open catalog → select a product → "Sửa" button appears in right panel header → click → form opens pre-filled with name/price/pricingMode → change name → "Cập nhật" → toast → detail panel refreshes with new name
4. **Permission test:** Log in as a user without `Products.Create` → "Thêm mới" button is absent; without `Products.Update` → "Sửa" button is absent

## Exit Criteria

- TypeScript: no new errors (`npx tsc --noEmit`)
- All existing product feature tests still pass (`npx vitest run src/features/products/`)
- Manual: create flow auto-selects product, edit flow refreshes detail panel
- Manual: permission-gated buttons hidden for users lacking the respective permission
