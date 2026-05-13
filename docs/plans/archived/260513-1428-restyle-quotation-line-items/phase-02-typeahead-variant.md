# Phase 02 — ProductTypeaheadCell variant prop

**Status:** [x] complete
**Complexity:** S

## Objective
Thêm prop `variant?: 'default' | 'cell'` vào `ProductTypeaheadCell`. Khi `variant="cell"`, render trigger là native `<input className="cell-input">` để hòa nhập style bảng `.accounting-grid`. Default giữ `<Input>` shadcn để backward compatible. Đồng thời render dropdown bằng portal để không bị clip bởi `.accounting-grid-wrap { overflow-x: auto; }`.

## Files
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`

## Tasks

1. Trước khi sửa, check xem `ProductTypeaheadCell` có được dùng nơi nào ngoài `LineItemsGrid` không:
```
Grep pattern: "ProductTypeaheadCell" trong frontend/src
```
Nếu có nơi khác → confirm default behavior không đổi (vẫn shadcn Input).

2. Sửa `product-typeahead-cell.tsx`:

   2.0. Thêm import portal và `useCallback`:
   ```ts
   import { useCallback, useEffect, useRef, useState } from 'react';
   import { createPortal } from 'react-dom';
   ```

   2.1. Thêm `variant` vào `Props`:
   ```ts
   interface Props {
     value: string;
     onChange: (value: string) => void;
     onSelect: (s: ProductSuggestion) => void;
     placeholder?: string;
     variant?: 'default' | 'cell';
   }
   ```

   2.2. Trong function component, destructure thêm `variant = 'default'`.

   2.3. Tách JSX trigger thành biến trung gian. Khi `variant === 'cell'`, render native `<input>`:
   ```tsx
   const triggerProps = {
     value,
     ref: inputRef,
     onFocus: () => {
       syncDropdownRect();
       setOpen(true);
     },
     onChange: (e: React.ChangeEvent<HTMLInputElement>) => {
       onChange(e.target.value);
       syncDropdownRect();
       setOpen(true);
     },
     onKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => {
       if (!open || items.length === 0) return;
       if (e.key === 'ArrowDown') { e.preventDefault(); setActiveIdx((i) => Math.min(i + 1, items.length - 1)); }
       else if (e.key === 'ArrowUp') { e.preventDefault(); setActiveIdx((i) => Math.max(i - 1, 0)); }
       else if (e.key === 'Enter') { e.preventDefault(); commit(items[activeIdx]); }
       else if (e.key === 'Escape') { setOpen(false); }
     },
     placeholder: placeholder ?? 'Mã / tên hàng',
   };

   const trigger = variant === 'cell'
     ? <input className="cell-input" {...triggerProps} />
     : <Input {...triggerProps} />;
   ```
   Sau đó replace block `<Input ... />` hiện tại bằng `{trigger}`.

   2.4. Thêm state/ref để định vị dropdown khi render qua portal:
   ```tsx
   const inputRef = useRef<HTMLInputElement>(null);
   const dropdownRef = useRef<HTMLDivElement>(null);
   const [dropdownRect, setDropdownRect] = useState<DOMRect | null>(null);

   const syncDropdownRect = useCallback(() => {
     setDropdownRect(inputRef.current?.getBoundingClientRect() ?? null);
   }, []);
   ```

   Đặt block ref/state này trước `triggerProps` để `inputRef` và `syncDropdownRect` sẵn sàng khi tạo trigger. Trong `onFocus`/`onChange`, gọi `syncDropdownRect()` trước `setOpen(true)`.

   2.5. Cập nhật click-outside effect hiện có để chấp nhận cả portal dropdown:
   ```tsx
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

   2.6. Thêm effect cập nhật vị trí khi scroll/resize trong lúc dropdown mở:
   ```tsx
   useEffect(() => {
     if (!open) return;
     syncDropdownRect();
     window.addEventListener('resize', syncDropdownRect);
     window.addEventListener('scroll', syncDropdownRect, true);
     return () => {
       window.removeEventListener('resize', syncDropdownRect);
       window.removeEventListener('scroll', syncDropdownRect, true);
     };
   }, [open, syncDropdownRect]);
   ```

   2.7. Thay dropdown inline hiện tại bằng portal:
   ```tsx
   {open && items.length > 0 && dropdownRect &&
     createPortal(
       <div
         ref={dropdownRef}
         className="z-50 max-h-72 w-[480px] overflow-auto rounded-md border bg-popover shadow-md"
         style={{
           position: 'fixed',
           left: dropdownRect.left,
           top: dropdownRect.bottom + 4,
           maxWidth: 'calc(100vw - 24px)',
         }}
       >
         {/* giữ nguyên table suggestion hiện tại */}
       </div>,
       document.body,
     )}
   ```

   Không giữ dropdown `absolute` bên trong cell, vì nó có thể bị clip bởi scroll container của bảng.

3. Save file.

## Verification
- Chạy `npm run lint` (ở `frontend/`) — không có lỗi TS mới.
- Chạy `npm run build` — pass.
- Nếu Grep ở bước 1 cho thấy `ProductTypeaheadCell` dùng nơi khác: mở trang đó (nếu có), confirm UI không thay đổi (vì không truyền `variant`).
- Manual `/quotations/new`: gõ vào ô "Mã hàng" trong bảng, suggestion dropdown phải nổi lên đầy đủ phía trên bảng, không bị cắt bởi vùng scroll ngang `.accounting-grid-wrap`.

## Exit Criteria
- File compile sạch.
- Prop `variant` available; default behavior identical với trước.
- Khi truyền `variant="cell"`, trigger render là `<input class="cell-input" />`.
- Dropdown suggestion render qua portal và không bị clip trong bảng line items.
