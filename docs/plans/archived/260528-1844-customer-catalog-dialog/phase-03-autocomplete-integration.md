# Phase 03 — Tích hợp vào CustomerAutocomplete

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm button "Xem danh mục đầy đủ" vào cuối dropdown suggestion của `CustomerAutocomplete`. Click button đóng dropdown và mở `CustomerCatalogDialog`. Mount `CustomerCatalogDialog` bên trong component, `onSelect` callback từ dialog gọi thẳng `onSelect` prop của `CustomerAutocomplete`.

## Files

- `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx` ← sửa
- `frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx` ← sửa (thêm test)

## Dependency

Phase 01 và Phase 02 phải complete.

## Tasks

### Task 1 — Thêm test cho catalog button vào file test hiện tại

1. Mở `frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx`.

   Ở đầu file, cập nhật mock `@/features/customers/api` để thêm `list` và `get`:

   Tìm dòng:
   ```ts
   vi.mock('@/features/customers/api', () => ({
     customersApi: {
       search: (...args: unknown[]) => searchMock(...args),
     },
   }));
   ```
   Thay bằng:
   ```ts
   const listMock = vi.fn();
   const getMock = vi.fn();

   vi.mock('@/features/customers/api', () => ({
     customersApi: {
       search: (...args: unknown[]) => searchMock(...args),
       list: (...args: unknown[]) => listMock(...args),
       get: (...args: unknown[]) => getMock(...args),
     },
   }));
   ```

   Trong `beforeEach`, thêm reset cho các mock mới:
   ```ts
   beforeEach(() => {
     searchMock.mockReset();
     searchMock.mockResolvedValue(sample);
     listMock.mockReset();
     listMock.mockResolvedValue({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 1, hasNextPage: false, hasPreviousPage: false });
     getMock.mockReset();
   });
   ```

   Thêm 2 test cases mới vào cuối `describe('CustomerAutocomplete', ...)`:
   ```ts
   it('shows "Xem danh mục đầy đủ" button in dropdown when results are shown', async () => {
     renderWithClient(<CustomerAutocomplete {...baseProps()} inputAriaLabel="cust" />);
     const input = screen.getByRole('combobox', { name: /cust/i });
     await typeAndWaitForResults(input, 'cong');
     expect(screen.getByRole('button', { name: /xem danh mục đầy đủ/i })).toBeInTheDocument();
   });

   it('clicking catalog button closes dropdown', async () => {
     renderWithClient(<CustomerAutocomplete {...baseProps()} inputAriaLabel="cust" />);
     const input = screen.getByRole('combobox', { name: /cust/i });
     await typeAndWaitForResults(input, 'cong');
     const btn = screen.getByRole('button', { name: /xem danh mục đầy đủ/i });
     fireEvent.mouseDown(btn);
     expect(input).toHaveAttribute('aria-expanded', 'false');
   });
   ```

2. Chạy test để xác nhận 2 test mới FAIL (component chưa có button):
```bash
cd frontend && npx vitest run src/components/customer-autocomplete/customer-autocomplete.test.tsx 2>&1 | tail -30
```
Expected: 2 tests FAIL (`Unable to find an accessible element with the role "button" and name /xem danh mục đầy đủ/i`), còn lại PASS.

### Task 2 — Cập nhật CustomerAutocomplete component

3. Mở `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx`.

   **Bước 3a** — Thêm imports:
   - Tìm dòng `import { Plus, X } from 'lucide-react';`
   - Thay bằng: `import { LayoutList, Plus, X } from 'lucide-react';`
   - Thêm import sau dòng `import { useCustomersSearch } from '@/features/customers/hooks';`:
     ```ts
     import { CustomerCatalogDialog } from '@/features/customers/components/customer-catalog-dialog';
     ```

   **Bước 3b** — Thêm state `catalogOpen`:
   - Tìm dòng: `const [isOpen, setIsOpen] = useState(false);`
   - Thêm NGAY SAU dòng đó: `const [catalogOpen, setCatalogOpen] = useState(false);`

   **Bước 3c** — Cấu trúc lại dropdown và thêm button catalog dưới cùng (sticky):

   Dropdown hiện tại có `overflow-auto max-h-80` trên div ngoài cùng — tất cả nội dung (kể cả button) sẽ scroll. Button cần nằm NGOÀI vùng scroll, cố định ở đáy.

   Tìm toàn bộ khối dropdown:
   ```tsx
   <div className="absolute left-0 z-50 mt-1 min-w-[min(760px,calc(100vw-80px))] max-w-[calc(100vw-40px)] max-h-80 overflow-auto rounded-md border bg-popover text-popover-foreground shadow-md">
     <div className="flex items-center justify-between border-b bg-muted/30 px-2 py-1.5 text-xs text-muted-foreground">
       ...
     </div>
     {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-to-interactive-role -- WAI-ARIA combobox listbox pattern */}
     <table className="qldh-lookup-table" id={listboxId} role="listbox">
       ...
     </table>
   </div>
   ```

   Thay bằng (bỏ `overflow-auto` khỏi div ngoài, thêm `flex flex-col`; bọc `<table>` trong div scrollable; thêm button footer):
   ```tsx
   <div className="absolute left-0 z-50 mt-1 min-w-[min(760px,calc(100vw-80px))] max-w-[calc(100vw-40px)] max-h-80 flex flex-col rounded-md border bg-popover text-popover-foreground shadow-md">
     <div className="flex items-center justify-between border-b bg-muted/30 px-2 py-1.5 text-xs text-muted-foreground flex-shrink-0">
       ...
     </div>
     <div className="overflow-auto flex-1">
       {/* eslint-disable-next-line jsx-a11y/no-noninteractive-element-to-interactive-role -- WAI-ARIA combobox listbox pattern */}
       <table className="qldh-lookup-table" id={listboxId} role="listbox">
         ...
       </table>
     </div>
     <div className="border-t flex-shrink-0">
       <button
         type="button"
         className="flex w-full items-center gap-2 px-3 py-2 text-sm text-muted-foreground hover:bg-muted/50 hover:text-foreground transition-colors"
         onMouseDown={(e) => {
           e.preventDefault();
           setIsOpen(false);
           setCatalogOpen(true);
         }}
       >
         <LayoutList className="h-3.5 w-3.5" aria-hidden="true" />
         Xem danh mục đầy đủ
       </button>
     </div>
   </div>
   ```

   **Bước 3d** — Mount `CustomerCatalogDialog`:
   - Trong JSX của return, tìm thẻ đóng `</div>` cuối cùng (thẻ đóng của `<div className="space-y-1" ref={containerRef}>`)
   - Thêm `CustomerCatalogDialog` NGAY TRƯỚC thẻ đóng đó:
   ```tsx
   <CustomerCatalogDialog
     open={catalogOpen}
     onOpenChange={setCatalogOpen}
     initialQuery={keyword}
     onSelect={onSelect}
   />
   ```

4. Chạy test để xác nhận TẤT CẢ PASS:
```bash
cd frontend && npx vitest run src/components/customer-autocomplete/customer-autocomplete.test.tsx 2>&1 | tail -30
```
Expected: PASS — tất cả tests passed (bao gồm 2 tests mới)

5. Chạy typecheck:
```bash
cd frontend && npm run typecheck 2>&1 | tail -20
```
Expected: 0 errors

6. Commit:
```bash
git add frontend/src/components/customer-autocomplete/customer-autocomplete.tsx frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx
git commit -m "feat: add customer catalog dialog button to CustomerAutocomplete dropdown"
```

## Verification

```bash
cd frontend && npx vitest run src/components/customer-autocomplete/customer-autocomplete.test.tsx
cd frontend && npm run typecheck
cd frontend && npm run build
```

## Exit Criteria

- Tất cả tests trong `customer-autocomplete.test.tsx` PASS (không có regression)
- 2 tests mới về catalog button PASS
- `npm run typecheck` — 0 lỗi mới
- `npm run build` — build thành công
