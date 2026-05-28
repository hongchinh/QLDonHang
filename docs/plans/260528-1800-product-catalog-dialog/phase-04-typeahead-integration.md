# Phase 04 — Tích hợp vào ProductTypeaheadCell

**Status:** [ ] pending
**Complexity:** S

## Objective

Sửa `product-typeahead-cell.tsx` để thêm button "Xem danh mục đầy đủ" ở cuối dropdown suggestion. Click button mở `ProductCatalogDialog` với `initialQuery` là text đang gõ. Khi user chọn sản phẩm từ dialog, gọi `commit()` (cùng logic với khi chọn từ suggestion) và đóng dialog.

## Files

- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx` ← **SỬA**

## Tasks

### Task 4.1 — Thêm state catalogOpen và import

Mở file `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`.

**Thêm import** (sau các import hiện có):
```tsx
import { LayoutList } from 'lucide-react';
import { ProductCatalogDialog } from '@/features/products/components/product-catalog-dialog';
```

**Thêm state** trong body của `ProductTypeaheadCell` (sau dòng `const [open, setOpen] = useState(false)`):
```tsx
const [catalogOpen, setCatalogOpen] = useState(false);
```

### Task 4.2 — Thêm button vào dropdown

Trong phần `createPortal(...)`, ngay sau thẻ đóng `</table>` và trước thẻ đóng `</div>` của dropdown wrapper, thêm:

```tsx
{/* Catalog browser button */}
<div className="border-t">
  <button
    type="button"
    className="flex w-full items-center gap-2 px-3 py-2 text-sm text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
    onMouseDown={(e) => {
      e.preventDefault();
      setOpen(false);
      setCatalogOpen(true);
    }}
  >
    <LayoutList className="h-3.5 w-3.5" aria-hidden="true" />
    Xem danh mục đầy đủ
  </button>
</div>
```

**Lý do dùng `onMouseDown` thay vì `onClick`:** `onDocClick` listener đang dùng `mousedown` để detect click ngoài dropdown. Nếu dùng `onClick`, `mousedown` sẽ fire trước và `onDocClick` sẽ set `open=false` trước khi button click được xử lý, khiến state `catalogOpen` không được set đúng. Dùng `onMouseDown` với `e.preventDefault()` ngăn focus rời khỏi input và xử lý trước `onDocClick`.

**Lưu ý vị trí trong code:** Button phải nằm bên trong `showDropdown ? createPortal(...)` (cùng `<div ref={dropdownRef}>` để `onDocClick` không đóng dropdown khi user di chuột vào button).

### Task 4.3 — Thêm ProductCatalogDialog vào render

Trong phần `return (...)` của `ProductTypeaheadCell`, sau `{dropdown}`, thêm:

```tsx
<ProductCatalogDialog
  open={catalogOpen}
  onOpenChange={setCatalogOpen}
  initialQuery={value}
  onSelect={(s) => {
    commit(s);
  }}
/>
```

**Lưu ý:** `commit(s)` đã gọi `onSelect(s)` (prop) và `setOpen(false)`. `ProductCatalogDialog.handleSelect` gọi `onOpenChange(false)` sau khi forward `onSelect`, nên dialog tự đóng — không cần `setCatalogOpen(false)` trong callback này.

Sau khi sửa, phần `return` trông như sau:
```tsx
return (
  <div ref={containerRef} className="relative">
    {trigger}
    {dropdown}
    <ProductCatalogDialog
      open={catalogOpen}
      onOpenChange={setCatalogOpen}
      initialQuery={value}
      onSelect={(s) => {
        commit(s);
        setCatalogOpen(false);
      }}
    />
  </div>
);
```

### Task 4.4 — Kiểm tra typecheck và build

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep -E "error TS"
```

Expected: 0 TypeScript errors.

```bash
cd frontend && npm run build 2>&1 | tail -20
```

Expected: build thành công, không có lỗi.

### Task 4.5 — Commit

```
git add frontend/src/pages/quotations/components/product-typeahead-cell.tsx
git commit -m "feat: integrate ProductCatalogDialog into ProductTypeaheadCell"
```

## Verification

**TypeScript:**
```bash
cd frontend && npx tsc --noEmit
```
Expected: exit code 0.

**Build:**
```bash
cd frontend && npm run build
```
Expected: build thành công.

**Thủ công (manual smoke test):**
1. Mở form báo giá
2. Click vào ô Mã hàng của một dòng line item
3. Gõ bất kỳ ký tự → dropdown xuất hiện
4. Xác nhận: button "Xem danh mục đầy đủ" hiện ở cuối dropdown
5. Click button → dropdown đóng, dialog mở, query được pre-fill với text đang gõ
6. Tabs nhóm hàng hiện (nếu có data) và filter hoạt động
7. Click một dòng → right panel hiện chi tiết sản phẩm
8. Double-click một dòng → dialog đóng, line item được điền đúng
9. Hoặc: click dòng → click "Chọn sản phẩm này" → dialog đóng, line item điền đúng
10. Keyboard navigation (ArrowUp/Down, Tab, Enter, Escape) trong dropdown vẫn hoạt động bình thường sau khi dialog đóng

## Exit Criteria

- `product-typeahead-cell.tsx` import và render `ProductCatalogDialog`
- State `catalogOpen` được thêm
- Button "Xem danh mục đầy đủ" nằm trong `createPortal` của dropdown, dùng `onMouseDown` với `e.preventDefault()`
- Click button: `setOpen(false)`, `setCatalogOpen(true)`
- `ProductCatalogDialog` render với đúng props
- `onSelect` từ dialog gọi `commit(s)`; dialog tự đóng qua `onOpenChange(false)` trong `handleSelect`
- TypeScript compile không lỗi
- `npm run build` thành công
