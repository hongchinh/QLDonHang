# Phase 07 — ProductTypeaheadCell upgrade theo Misa-pattern (TK Nợ/TK Có)

**Status:** [x] complete
**Complexity:** M

## Objective
Nâng cấp UX nhập "Mã hàng" trong bảng line items để khớp pattern thao tác nhập của TK Nợ/TK Có trong mockup [ui_form_them_moi_phieu_thu_html.html](../../bd/ui_form_them_moi_phieu_thu_html.html) (Misa-style ERP). Tham chiếu hiện thực: [customer-autocomplete.tsx](../../../frontend/src/components/customer-autocomplete/customer-autocomplete.tsx) — popover rộng có meta header + kbd hints + Tab/Shift+Tab navigate trong list + state loading/empty/error + active row outline xanh. Thêm auto-jump focus sang ô "Tên hàng" sau khi chọn sản phẩm.

**Phase này là extension của Phase 02 (portal-based dropdown).** Phase 02 đảm bảo dropdown không bị clip — Phase 07 đảm bảo CONTENT + KEYBOARD bên trong dropdown khớp pattern Misa.

## Files
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx` (sửa lớn)
- `frontend/src/pages/quotations/components/line-items-grid.tsx` (sửa nhẹ — gán id + truyền nextFocusId)

## Tasks

### 1. Verify `useProductSearch` có debounce nội tại không
```
Grep pattern: "useProductSearch" trong frontend/src/features/products
```
Mở file hook, đọc implementation:
- Nếu đã debounce → bỏ qua bước thêm `useDebouncedValue`.
- Nếu chưa debounce → import `useDebouncedValue` từ `@/lib/use-debounced-value` (CustomerAutocomplete đã có) và dùng:
  ```ts
  const debouncedKeyword = useDebouncedValue(value, 250);
  const search = useProductSearch(debouncedKeyword);
  ```

### 2. Sửa `product-typeahead-cell.tsx`

#### 2.1. Imports + ARIA listbox id
```ts
import { useCallback, useEffect, useId, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Input } from '@/components/ui/input';
import { cn } from '@/lib/utils';
import { useProductSearch } from '@/features/products/hooks';
import type { ProductSuggestion } from '@/features/products/types';
// (Optional) import { useDebouncedValue } from '@/lib/use-debounced-value';
```

#### 2.2. Props mở rộng
```ts
interface Props {
  value: string;
  onChange: (value: string) => void;
  onSelect: (s: ProductSuggestion) => void;
  placeholder?: string;
  variant?: 'default' | 'cell';
  /** ID của input cần focus ngay sau khi commit. */
  nextFocusId?: string;
}
```

#### 2.3. State chính (rename `activeIdx` → `highlightedIndex` cho thống nhất với CustomerAutocomplete)
```ts
const [open, setOpen] = useState(false);
const [highlightedIndex, setHighlightedIndex] = useState(0);
const containerRef = useRef<HTMLDivElement>(null);
const inputRef = useRef<HTMLInputElement>(null);
const dropdownRef = useRef<HTMLDivElement>(null);
const [dropdownRect, setDropdownRect] = useState<DOMRect | null>(null);
const reactId = useId();
const listboxId = `product-listbox-${reactId}`;
```

#### 2.4. `syncDropdownRect` + effects
(Đã có ở Phase 02 — giữ nguyên: sync khi focus, change, resize, scroll capture.)

#### 2.5. Reset `highlightedIndex` khi items đổi
```ts
useEffect(() => {
  if (highlightedIndex >= items.length) setHighlightedIndex(0);
}, [items.length, highlightedIndex]);
```

#### 2.6. Commit + auto-jump focus
```ts
const commit = useCallback((s: ProductSuggestion) => {
  onSelect(s);
  setOpen(false);
  if (nextFocusId) {
    setTimeout(() => document.getElementById(nextFocusId)?.focus(), 0);
  }
}, [onSelect, nextFocusId]);
```

#### 2.7. Keyboard handler (REPLACE handler cũ)
```ts
function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
  // Nếu popover đóng hoặc rỗng: chỉ xử lý Escape (đóng nếu mở)
  if (!open || items.length === 0) {
    if (e.key === 'Escape' && open) { e.preventDefault(); setOpen(false); }
    return;
  }
  switch (e.key) {
    case 'ArrowDown':
      e.preventDefault();
      setHighlightedIndex((i) => (i + 1) % items.length);
      break;
    case 'ArrowUp':
      e.preventDefault();
      setHighlightedIndex((i) => (i - 1 + items.length) % items.length);
      break;
    case 'Tab':
      e.preventDefault(); // KHÔNG cho Tab nhảy ra ngoài khi popover mở
      setHighlightedIndex((i) =>
        e.shiftKey ? (i - 1 + items.length) % items.length : (i + 1) % items.length,
      );
      break;
    case 'Enter':
      e.preventDefault();
      commit(items[highlightedIndex]);
      break;
    case 'Escape':
      e.preventDefault();
      setOpen(false);
      break;
  }
}
```
**Quan trọng**: handler này áp cho cả 2 trigger (`variant='default'` lẫn `'cell'`).

#### 2.8. Trigger với ARIA combobox
```tsx
const showDropdown = open && (isLoading || isError || items.length > 0 || /* always show meta */ true);
// Thực tế CustomerAutocomplete chỉ show khi keyword.length > 0. Giữ logic hiện tại: open khi focus + có suggestion.
// Quyết định: show dropdown khi `open && (isLoading || isError || items.length >= 0)` — tức luôn show khi open
// để user thấy state "Không tìm thấy". Khác với hiện tại (chỉ show khi items.length > 0).

const activeOptionId =
  open && items[highlightedIndex] ? `product-option-${items[highlightedIndex].id}` : undefined;

const triggerProps = {
  value,
  ref: inputRef,
  onFocus: () => { syncDropdownRect(); setOpen(true); },
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value);
    syncDropdownRect();
    setOpen(true);
  },
  onKeyDown: handleKeyDown,
  placeholder: placeholder ?? 'Mã / tên hàng',
  role: 'combobox' as const,
  'aria-expanded': open,
  'aria-controls': listboxId,
  'aria-autocomplete': 'list' as const,
  'aria-activedescendant': activeOptionId,
  autoComplete: 'off',
};

const trigger = variant === 'cell'
  ? <input className="cell-input" {...triggerProps} />
  : <Input {...triggerProps} />;
```

#### 2.9. Body dropdown (REPLACE table hiện tại — render qua portal)
```tsx
{open && dropdownRect && createPortal(
  <div
    ref={dropdownRef}
    className="z-50 min-w-[min(760px,calc(100vw-80px))] max-w-[calc(100vw-40px)] max-h-80 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md"
    style={{
      position: 'fixed',
      left: dropdownRect.left,
      top: dropdownRect.bottom + 4,
    }}
  >
    <div className="flex items-center justify-between border-b bg-muted/30 px-2 py-1.5 text-xs text-muted-foreground">
      <span>
        {isLoading
          ? 'Đang tìm kiếm...'
          : isError
          ? 'Lỗi tải danh sách'
          : `Tìm thấy ${items.length} sản phẩm`}
      </span>
      <span className="flex items-center gap-1.5">
        <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Tab</kbd> duyệt
        <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Enter</kbd> chọn
        <kbd className="rounded border bg-background px-1 py-0.5 font-mono text-[10px]">Esc</kbd> đóng
      </span>
    </div>
    <table className="w-full text-sm" id={listboxId} role="listbox">
      <thead className="sticky top-0 bg-muted/60 text-left text-xs text-muted-foreground">
        <tr>
          <th className="px-2 py-1 font-medium">Mã</th>
          <th className="px-2 py-1 font-medium">Tên</th>
          <th className="px-2 py-1 font-medium">Loại</th>
          <th className="px-2 py-1 font-medium">Quy cách</th>
          <th className="px-2 py-1 font-medium text-right">Giá bán</th>
        </tr>
      </thead>
      <tbody>
        {isLoading && (
          <tr><td colSpan={5} className="px-2 py-3 text-center text-muted-foreground">Đang tìm kiếm...</td></tr>
        )}
        {!isLoading && isError && (
          <tr><td colSpan={5} className="px-2 py-3 text-center text-destructive">Không thể tải danh sách. Vui lòng thử lại.</td></tr>
        )}
        {!isLoading && !isError && items.length === 0 && (
          <tr><td colSpan={5} className="px-2 py-3 text-center text-muted-foreground">Không tìm thấy sản phẩm phù hợp</td></tr>
        )}
        {!isLoading && !isError && items.map((s, i) => {
          const highlighted = i === highlightedIndex;
          return (
            <tr
              key={s.id}
              id={`product-option-${s.id}`}
              role="option"
              aria-selected={highlighted}
              onMouseDown={(e) => { e.preventDefault(); commit(s); }}
              onMouseEnter={() => setHighlightedIndex(i)}
              className={cn(
                'cursor-pointer border-t',
                highlighted && 'bg-blue-50 outline outline-1 -outline-offset-1 outline-blue-500',
              )}
            >
              <td className="px-2 py-1 font-mono">{s.code}</td>
              <td className="px-2 py-1">{s.name}</td>
              <td className="px-2 py-1">{PRICING_LABEL[s.pricingMode]}</td>
              <td className="px-2 py-1 text-xs text-muted-foreground">{s.specification ?? ''}</td>
              <td className="px-2 py-1 text-right tabular-nums">
                {s.defaultPrice != null ? currency.format(s.defaultPrice) : ''}
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  </div>,
  document.body,
)}
```

#### 2.10. Click-outside chấp nhận cả portal dropdown (giữ logic Phase 02)
```ts
useEffect(() => {
  function onDocClick(e: MouseEvent) {
    const target = e.target as Node;
    if (containerRef.current?.contains(target)) return;
    if (dropdownRef.current?.contains(target)) return;
    setOpen(false);
  }
  document.addEventListener('mousedown', onDocClick);
  return () => document.removeEventListener('mousedown', onDocClick);
}, []);
```

### 3. Sửa `line-items-grid.tsx`

#### 3.1. Trong block JSX của Phase 03 (đã viết), tìm input "Tên hàng":
```tsx
<input
  className="cell-input"
  value={(line.productName ?? '') as string}
  onChange={(e) => setLineField(idx, 'productName', e.target.value)}
/>
```

#### 3.2. Thêm `id` predictable:
```tsx
<input
  id={`line-name-${idx}`}
  className="cell-input"
  value={(line.productName ?? '') as string}
  onChange={(e) => setLineField(idx, 'productName', e.target.value)}
/>
```

#### 3.3. Truyền `nextFocusId` vào `<ProductTypeaheadCell>`:
```tsx
<ProductTypeaheadCell
  variant="cell"
  nextFocusId={`line-name-${idx}`}
  value={(line.productCode ?? '') as string}
  onChange={(v) => setLineField(idx, 'productCode', v)}
  onSelect={(s) => { /* giữ nguyên auto-fill logic */ }}
/>
```

### 4. Save cả 2 file

## Verification

### Static
- `npm run lint` (ở `frontend/`) — không lỗi mới.
- `npm run build` — pass.

### Manual (`/quotations/new`)

#### A. Visual diff
- Mở 2 popover song song: customer (top form) vs product (cell Mã hàng). 2 popover trông giống nhau về:
  - Layout 760px-ish responsive.
  - Header meta + kbd hints bên phải.
  - Active row outline xanh + bg blue-50.
  - State row centered text muted.

#### B. Keyboard
- Focus cell "Mã hàng" → gõ "SP" → sau ~250ms popover hiện danh sách.
- `ArrowDown` × 3 → highlight row 4.
- `ArrowUp` × 1 → highlight row 3.
- `Tab` → row 4; tiếp `Tab` đến cuối → wrap về row 1.
- `Shift+Tab` × 1 → row N (cuối list).
- `Enter` → commit row đang highlight → popover đóng → focus jump sang input "Tên hàng" cùng row (kiểm tra cursor blink trong ô đó).
- Gõ → popover lại mở → `Esc` → đóng popover (keyword giữ nguyên).
- Tab khi popover ĐÓNG → nhảy sang cell ĐVT (browser default).

#### C. State
- Gõ "zzzzz" → state "Không tìm thấy sản phẩm phù hợp" hiện.
- (Optional) tắt backend API → state "Không thể tải danh sách...".
- Trong khi đang search → state "Đang tìm kiếm...".

#### D. Mouse
- Hover row → outline xanh + bg blue-50.
- Click row → commit + auto-jump.

#### E. Auto-jump focus với multiple rows
- Tạo 3 dòng. Trên dòng 2, focus Mã hàng → chọn sản phẩm → focus phải nhảy sang Tên hàng dòng 2 (KHÔNG phải dòng 1 hoặc dòng 3). Verify ID predictable đúng.

#### F. Regression (kế thừa Phase 02/03)
- Popover không bị clip khi bảng scroll ngang.
- Chọn sản phẩm vẫn auto-fill productName, unitName, pricingMode, unitPrice, unitCost như cũ.

## Exit Criteria
- File compile sạch; type check pass.
- Popover Mã hàng visually matches popover Customer (header meta, kbd hints, state rows, active row outline).
- Tab/Shift+Tab navigate row khi popover mở; Tab khi đóng giữ default browser.
- Enter commit highlighted → auto-focus sang input `line-name-${idx}`.
- ARIA roles (combobox/listbox/option/aria-activedescendant/aria-selected) đầy đủ.
- Không regression visual/behavior từ Phase 02 (portal positioning, variant cell).
- Không regression auto-fill logic từ Phase 03.
