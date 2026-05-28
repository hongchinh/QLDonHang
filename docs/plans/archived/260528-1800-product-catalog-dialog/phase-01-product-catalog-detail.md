# Phase 01 — ProductCatalogDetail component

**Status:** [ ] pending
**Complexity:** S

## Objective

Tạo component `ProductCatalogDetail` — right panel của dialog, hiển thị chi tiết đầy đủ của một sản phẩm khi user click vào dòng trong danh sách. Có nút "Chọn sản phẩm này" để fill vào line item.

## Files

- `frontend/src/features/products/components/product-catalog-detail.tsx` ← **TẠO MỚI**

## Tasks

### Task 1.1 — Tạo ProductCatalogDetail

Tạo file `frontend/src/features/products/components/product-catalog-detail.tsx` với nội dung sau:

```tsx
import { useProduct, useAuthStore } from './imports'; // xem chi tiết import bên dưới
```

**Imports cần dùng:**
```tsx
import { useProduct } from '@/features/products/hooks';
import { useAuthStore } from '@/stores/auth-store';
import { formatMoneyForDisplay } from '@/pages/quotations/utils/money-input';
import type { ProductSuggestion } from '@/features/products/types';
import { Button } from '@/components/ui/button';
```

**Props interface:**
```tsx
interface Props {
  productId: string | null;
  onSelect: (product: ProductSuggestion) => void;
}
```

**PricingMode label map** (define ở module scope):
```tsx
const PRICING_MODE_LABEL: Record<string, string> = {
  PerUnit: 'Theo đơn vị',
  PerSquareMeter: 'Theo diện tích (m²)',
  PerLinearMeter: 'Theo dài (m)',
  PerCubicMeter: 'Theo thể tích (m³)',
};
```

**Logic:**

1. `const { data: product, isLoading } = useProduct(productId ?? undefined)`
2. `const canViewCost = useAuthStore((s) => s.hasPermission('quotations.view_cost'))`
3. Hàm `handleSelect` — map `Product` sang `ProductSuggestion` rồi gọi `onSelect`:
   ```tsx
   function handleSelect() {
     if (!product) return;
     onSelect({
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

**Render cases:**

- `productId === null`: hiện `<div className="flex h-full items-center justify-center text-muted-foreground text-sm">← Chọn sản phẩm để xem chi tiết</div>`
- `isLoading`: hiện `<div className="flex h-full items-center justify-center text-muted-foreground text-sm">Đang tải...</div>`
- Khi có `product`: hiện các section:

```tsx
<div className="flex h-full flex-col overflow-y-auto p-4 gap-4">
  {/* Header */}
  <div>
    <p className="text-xs text-muted-foreground font-mono">{product.code}</p>
    <h3 className="text-base font-semibold leading-snug">{product.name}</h3>
    {product.productGroupName && (
      <p className="text-xs text-muted-foreground">Nhóm: {product.productGroupName}</p>
    )}
  </div>

  {/* Thông tin cơ bản */}
  <Section title="Thông tin cơ bản">
    <Row label="ĐVT" value={product.unitName} />
    <Row label="Quy cách" value={product.specification} />
    <Row label="Loại tính giá" value={PRICING_MODE_LABEL[product.pricingMode]} />
  </Section>

  {/* Kích thước — chỉ hiện nếu có ít nhất 1 field */}
  {(product.length || product.width || product.thickness || product.density) && (
    <Section title="Kích thước">
      {(product.length || product.width || product.thickness) && (
        <Row
          label="D × R × Dày"
          value={`${product.length ?? '—'} × ${product.width ?? '—'} × ${product.thickness ?? '—'} mm`}
        />
      )}
      {product.density != null && (
        <Row label="Mật độ" value={`${product.density} kg/m³`} />
      )}
    </Section>
  )}

  {/* Giá & Thuế */}
  <Section title="Giá & Thuế">
    <Row label="Giá bán" value={formatMoneyForDisplay(product.defaultPrice)} mono />
    {canViewCost && product.costPrice != null && (
      <Row label="Giá nhập" value={formatMoneyForDisplay(product.costPrice)} mono />
    )}
    {product.defaultTaxRate != null && (
      <Row label="Thuế" value={`${product.defaultTaxRate}%`} />
    )}
  </Section>

  {/* Nút chọn */}
  <div className="mt-auto pt-2">
    <Button onClick={handleSelect} className="w-full">
      Chọn sản phẩm này
    </Button>
  </div>
</div>
```

**Helper components** (define trong cùng file):
```tsx
function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <p className="mb-1 text-xs font-medium text-muted-foreground uppercase tracking-wide">{title}</p>
      <div className="rounded-md border divide-y text-sm">{children}</div>
    </div>
  );
}

function Row({ label, value, mono }: { label: string; value?: string | null; mono?: boolean }) {
  return (
    <div className="flex justify-between px-3 py-1.5">
      <span className="text-muted-foreground">{label}</span>
      <span className={mono ? 'tabular-nums' : ''}>{value ?? '—'}</span>
    </div>
  );
}
```

**Export:** `export function ProductCatalogDetail({ productId, onSelect }: Props) { ... }`

### Task 1.2 — Kiểm tra typecheck

Chạy:
```bash
cd frontend && npx tsc --noEmit 2>&1 | grep product-catalog-detail
```

Expected: không có lỗi liên quan đến file mới.

### Task 1.3 — Commit

```
git add frontend/src/features/products/components/product-catalog-detail.tsx
git commit -m "feat: add ProductCatalogDetail component for product catalog popup"
```

## Verification

```bash
cd frontend && npx tsc --noEmit 2>&1 | grep -E "(error|product-catalog)"
```

Expected: 0 TypeScript errors cho file mới.

## Exit Criteria

- File `product-catalog-detail.tsx` tồn tại và export `ProductCatalogDetail`
- TypeScript compile không lỗi
- Render placeholder khi `productId === null`
- Render chi tiết đầy đủ khi có `product` data
- Nút "Chọn sản phẩm này" gọi `onSelect` với `ProductSuggestion` được map từ `Product`
- Giá nhập chỉ render khi `canViewCost === true`
