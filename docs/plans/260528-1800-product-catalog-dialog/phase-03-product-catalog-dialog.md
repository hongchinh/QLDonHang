# Phase 03 — ProductCatalogDialog wrapper

**Status:** [ ] pending
**Complexity:** S

## Objective

Tạo `ProductCatalogDialog` — dialog wrapper kết hợp `ProductCatalogList` (55%) và `ProductCatalogDetail` (45%) trong một layout split-pane. Quản lý state nội bộ (query, groupId, page, selectedId). Khi `onSelect` được gọi (từ list double-click hoặc detail button), forward ra ngoài và đóng dialog.

## Files

- `frontend/src/features/products/components/product-catalog-dialog.tsx` ← **TẠO MỚI**

## Tasks

### Task 3.1 — Tạo ProductCatalogDialog

Tạo file `frontend/src/features/products/components/product-catalog-dialog.tsx`.

**Imports:**
```tsx
import { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from '@/components/ui/dialog';
import { ProductCatalogList } from './product-catalog-list';
import { ProductCatalogDetail } from './product-catalog-detail';
import type { ProductSuggestion } from '@/features/products/types';
```

**Props interface:**
```tsx
interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialQuery?: string;
  onSelect: (product: ProductSuggestion) => void;
}
```

**State:**
```tsx
const [query, setQuery] = useState(initialQuery ?? '');
const [groupId, setGroupId] = useState<string | undefined>(undefined);
const [page, setPage] = useState(1);
const [selectedId, setSelectedId] = useState<string | null>(null);
```

**Sync `initialQuery` khi dialog mở:**
```tsx
useEffect(() => {
  if (open) {
    setQuery(initialQuery ?? '');
    setGroupId(undefined);
    setPage(1);
    setSelectedId(null);
  }
}, [open, initialQuery]);
```

**`handleSelect` — forward ra ngoài và đóng dialog:**
```tsx
function handleSelect(product: ProductSuggestion) {
  onSelect(product);
  onOpenChange(false);
}
```

**Reset page khi query thay đổi:**
```tsx
function handleQueryChange(q: string) {
  setQuery(q);
  setPage(1);
  setSelectedId(null);
}
```

**Render:**
```tsx
<Dialog open={open} onOpenChange={onOpenChange}>
  <DialogContent
    className="flex max-w-5xl overflow-hidden p-0 gap-0 h-[85vh]"
    showClose={true}
  >
    <DialogTitle className="sr-only">Danh mục hàng hóa</DialogTitle>
    {/* Left panel — 55% */}
    <div className="flex w-[55%] flex-col min-h-0">
      <div className="border-b px-4 py-3 flex-shrink-0">
        <h2 className="text-base font-semibold">Danh mục hàng hóa</h2>
      </div>
      <div className="flex-1 overflow-hidden min-h-0">
        <ProductCatalogList
          query={query}
          onQueryChange={handleQueryChange}
          groupId={groupId}
          onGroupChange={(id) => { setGroupId(id); setPage(1); setSelectedId(null); }}
          page={page}
          onPageChange={(p) => { setPage(p); setSelectedId(null); }}
          selectedId={selectedId}
          onSelectId={setSelectedId}
          onSelect={handleSelect}
        />
      </div>
    </div>
    {/* Divider */}
    <div className="w-px bg-border flex-shrink-0" />
    {/* Right panel — 45% */}
    <div className="flex w-[45%] flex-col overflow-hidden min-h-0">
      <div className="border-b px-4 py-3 flex-shrink-0 flex items-center justify-between">
        <h2 className="text-sm font-medium text-muted-foreground">Chi tiết sản phẩm</h2>
      </div>
      <div className="flex-1 overflow-hidden min-h-0">
        <ProductCatalogDetail
          productId={selectedId}
          onSelect={handleSelect}
        />
      </div>
    </div>
  </DialogContent>
</Dialog>
```

**Lưu ý về layout:**
- `DialogContent` mặc định có class `grid gap-4 p-6 max-w-lg`. Override toàn bộ bằng `className="flex max-w-5xl overflow-hidden p-0 gap-0 h-[85vh]"`.
  - `flex` overrides `grid`; `p-0` overrides `p-6`; `max-w-5xl` overrides `max-w-lg`; `gap-0` overrides `gap-4` (quan trọng — nếu để `gap-4` sẽ thêm khoảng trắng giữa panel và divider)
  - `h-[85vh]` thay cho `style={{ height: '85vh' }}`
- Left và right panel cần `min-h-0` để flex children có thể shrink đúng trong nested flex layout: thêm `min-h-0` vào `<div className="flex w-[55%] flex-col min-h-0">` và `<div className="flex w-[45%] flex-col overflow-hidden min-h-0">`.
- Wrapper `flex-1 overflow-hidden` quanh `ProductCatalogList` cũng cần `min-h-0`: `<div className="flex-1 overflow-hidden min-h-0">`.
- `showClose={true}` giữ nút X ở `absolute right-4 top-4`. Cả hai panel đều có header `py-3` nên X button nằm trong vùng header của right panel, không đè lên content.

**Export:** `export function ProductCatalogDialog({ open, onOpenChange, initialQuery, onSelect }: Props) { ... }`

### Task 3.2 — Kiểm tra typecheck

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep product-catalog
```

Expected: không có lỗi liên quan đến 3 files catalog mới.

### Task 3.3 — Commit

```
git add frontend/src/features/products/components/product-catalog-dialog.tsx
git commit -m "feat: add ProductCatalogDialog split-pane wrapper"
```

## Verification

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep -E "error TS"
```

Expected: 0 TypeScript errors toàn project.

## Exit Criteria

- File `product-catalog-dialog.tsx` tồn tại và export `ProductCatalogDialog`
- TypeScript compile không lỗi
- State reset đúng khi `open` chuyển thành `true`
- `handleSelect` đóng dialog (`onOpenChange(false)`) và forward `onSelect`
- Layout: `ProductCatalogList` chiếm 55%, `ProductCatalogDetail` chiếm 45%
- Left panel có header "Danh mục hàng hóa"; right panel có header "Chi tiết sản phẩm" — nút X close (absolute top-right) không đè lên nội dung
- `DialogTitle` có `className="sr-only"` để accessibility (title ẩn visual nhưng có cho screen reader), tiêu đề "Danh mục hàng hóa" hiển thị trong header của left panel
