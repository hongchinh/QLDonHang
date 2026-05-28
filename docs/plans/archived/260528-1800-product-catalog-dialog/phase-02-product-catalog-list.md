# Phase 02 — ProductCatalogList component

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo component `ProductCatalogList` — left panel của dialog, gồm: search input, tabs bộ lọc nhóm hàng, bảng danh sách sản phẩm có phân trang. Click row → set selectedId. Double-click row → chọn ngay (fill + đóng dialog).

## Files

- `frontend/src/features/products/components/product-catalog-list.tsx` ← **TẠO MỚI**

## Tasks

### Task 2.1 — Tạo ProductCatalogList

Tạo file `frontend/src/features/products/components/product-catalog-list.tsx`.

**Imports:**
```tsx
import { Search } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useProducts, useProductGroups } from '@/features/products/hooks';
import { useDebouncedValue } from '@/lib/use-debounced-value';
import { formatMoneyForDisplay } from '@/pages/quotations/utils/money-input';
import { cn } from '@/lib/utils';
import type { ProductSuggestion } from '@/features/products/types';
```

**Props interface:**
```tsx
interface Props {
  query: string;
  onQueryChange: (q: string) => void;
  groupId: string | undefined;
  onGroupChange: (id: string | undefined) => void;
  page: number;
  onPageChange: (p: number) => void;
  selectedId: string | null;
  onSelectId: (id: string) => void;
  onSelect: (product: ProductSuggestion) => void;
}
```

**Logic:**

1. `const debouncedQuery = useDebouncedValue(query, 300)`
2. `const groups = useProductGroups()` — `groups.data` là `LookupItem[]` (từ `/lookups/product-groups`)
3. `const products = useProducts({ search: debouncedQuery, productGroupId: groupId, page, pageSize: 20, status: 'Active' })`
4. `const items = products.data?.items ?? []`
5. `const totalPages = products.data?.totalPages ?? 1`

**Render structure:**
```tsx
<div className="flex h-full flex-col border-r">
  {/* Search input */}
  <div className="border-b p-3">
    <div className="relative">
      <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
      <Input
        className="pl-8"
        placeholder="Tìm mã, tên hàng..."
        value={query}
        onChange={(e) => onQueryChange(e.target.value)}
        autoFocus
      />
    </div>
  </div>

  {/* Group tabs */}
  {groups.data && groups.data.length > 0 && (
    <div className="border-b px-3 py-2 overflow-x-auto">
      <Tabs
        value={groupId ?? 'all'}
        onValueChange={(v) => {
          onGroupChange(v === 'all' ? undefined : v);
          onPageChange(1);
        }}
      >
        <TabsList className="h-auto flex-wrap gap-1 bg-transparent p-0">
          <TabsTrigger value="all" className="h-7 px-2 text-xs">
            Tất cả
          </TabsTrigger>
          {groups.data.map((g) => (
            <TabsTrigger key={g.id} value={g.id} className="h-7 px-2 text-xs">
              {g.name}
            </TabsTrigger>
          ))}
        </TabsList>
      </Tabs>
    </div>
  )}

  {/* Table */}
  <div className="flex-1 overflow-auto">
    <table className="w-full text-sm">
      <thead className="sticky top-0 bg-muted/80 backdrop-blur-sm">
        <tr className="border-b">
          <th className="px-3 py-2 text-left font-medium text-muted-foreground">Mã hàng</th>
          <th className="px-3 py-2 text-left font-medium text-muted-foreground">Tên hàng</th>
          <th className="px-3 py-2 text-left font-medium text-muted-foreground">ĐVT</th>
          <th className="px-3 py-2 text-left font-medium text-muted-foreground">Quy cách</th>
          <th className="px-3 py-2 text-right font-medium text-muted-foreground">Giá bán</th>
        </tr>
      </thead>
      <tbody>
        {products.isLoading && (
          <>
            {Array.from({ length: 8 }).map((_, i) => (
              <tr key={i} className="border-b animate-pulse">
                <td colSpan={5} className="px-3 py-2">
                  <div className="h-4 rounded bg-muted" />
                </td>
              </tr>
            ))}
          </>
        )}
        {!products.isLoading && items.length === 0 && (
          <tr>
            <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
              Không tìm thấy sản phẩm phù hợp
            </td>
          </tr>
        )}
        {!products.isLoading &&
          items.map((item) => (
            <tr
              key={item.id}
              className={cn(
                'cursor-pointer border-b transition-colors hover:bg-muted/50',
                selectedId === item.id && 'bg-primary/10 hover:bg-primary/15',
              )}
              onClick={() => onSelectId(item.id)}
              onDoubleClick={() =>
                onSelect({
                  id: item.id,
                  code: item.code,
                  name: item.name,
                  specification: item.specification,
                  unitName: item.unitName,
                  pricingMode: item.pricingMode,
                  defaultPrice: item.defaultPrice,
                  costPrice: item.costPrice,
                })
              }
            >
              <td className="px-3 py-2 font-mono text-xs">{item.code}</td>
              <td className="px-3 py-2">{item.name}</td>
              <td className="px-3 py-2 text-muted-foreground">{item.unitName ?? '—'}</td>
              <td className="px-3 py-2 text-xs text-muted-foreground">{item.specification ?? '—'}</td>
              <td className="px-3 py-2 text-right tabular-nums">
                {formatMoneyForDisplay(item.defaultPrice)}
              </td>
            </tr>
          ))}
      </tbody>
    </table>
  </div>

  {/* Pagination */}
  <div className="flex items-center justify-between border-t px-3 py-2 text-sm">
    <span className="text-muted-foreground">
      Trang {page} / {totalPages}
      {products.data && (
        <span className="ml-2 text-xs">({products.data.totalItems} sản phẩm)</span>
      )}
    </span>
    <div className="flex gap-1">
      <button
        className="rounded border px-2 py-1 text-xs disabled:opacity-40 hover:bg-muted"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        ← Trước
      </button>
      <button
        className="rounded border px-2 py-1 text-xs disabled:opacity-40 hover:bg-muted"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Sau →
      </button>
    </div>
  </div>
</div>
```

**Export:** `export function ProductCatalogList({ ... }: Props) { ... }`

**Lưu ý quan trọng:** `ProductListItem` (từ `useProducts`) có field `costPrice?: number` — đây là field optional nên cần map đúng khi tạo `ProductSuggestion` từ double-click.

### Task 2.2 — Kiểm tra typecheck

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep product-catalog-list
```

Expected: không có lỗi.

### Task 2.3 — Commit

```
git add frontend/src/features/products/components/product-catalog-list.tsx
git commit -m "feat: add ProductCatalogList component for product catalog popup"
```

## Verification

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep -E "(error|product-catalog)"
```

Expected: 0 TypeScript errors.

## Exit Criteria

- File `product-catalog-list.tsx` tồn tại và export `ProductCatalogList`
- TypeScript compile không lỗi
- Search input debounce 300ms hoạt động
- Tabs nhóm hàng hiện đúng từ `useProductGroups()`, chọn tab filter danh sách và reset page=1
- Bảng hiện loading state khi đang tải
- Bảng hiện "Không tìm thấy" khi `items.length === 0`
- Click row → `onSelectId(item.id)` được gọi, row highlight
- Double-click row → `onSelect(ProductSuggestion)` được gọi với đúng các field
- Phân trang: nút Trước/Sau disable đúng, `onPageChange` được gọi
