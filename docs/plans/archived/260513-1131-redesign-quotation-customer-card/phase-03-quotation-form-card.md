# Phase 03 — Restructure card "Thông tin khách hàng" trong quotation form

**Status:** [x] complete
**Complexity:** M

## Objective

Đổi bố cục card "Thông tin khách hàng" trong [quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx) sang inline-grid 4-cột: hàng 1 = Ngày báo giá, hàng 2 = Mã KH + Tên KH cùng dòng. Cập nhật state `selectedCustomerView` để chứa thêm `code`.

## Files

- `frontend/src/pages/quotations/quotation-form-page.tsx`

## Tasks

1. **Mở rộng type `selectedCustomerView`** (dòng 144-146):
   - Hiện tại: `useState<{ id: string; name: string } | null>(...)`
   - Đổi thành: `useState<{ id: string; code: string; name: string } | null>(...)`
   - Giá trị khởi tạo lấy thêm `code: initialSelectedCustomer.code`.
2. **Cập nhật `useEffect` (dòng 155-159)** đồng bộ `code` khi `initialSelectedCustomer` thay đổi:
   ```tsx
   useEffect(() => {
     if (initialSelectedCustomer) {
       setSelectedCustomerView({
         id: initialSelectedCustomer.id,
         code: initialSelectedCustomer.code,
         name: initialSelectedCustomer.name,
       });
     }
   }, [initialSelectedCustomer?.id]);
   ```
3. **Cập nhật `handleSelectCustomer` (dòng 161-178)** để set `code` vào state:
   - Đổi `setSelectedCustomerView({ id: c.id, name: c.name })` → `setSelectedCustomerView({ id: c.id, code: c.code, name: c.name })`.
4. **Restructure JSX card "Thông tin khách hàng"** (dòng 235-279):
   - Thay `<CardContent className="grid gap-4 md:grid-cols-2">` bằng:
     ```tsx
     <CardContent>
       <div
         className="form-inline-grid customer-row"
         style={{ gridTemplateColumns: '90px minmax(180px,1fr) 90px minmax(220px,2fr)' }}
       >
         {/* Hàng 1: Ngày báo giá */}
         <Label htmlFor="quotationDate" className="field-label required">Ngày báo giá</Label>
         <div style={{ gridColumn: 'span 3' }}>
           <Input id="quotationDate" type="date" {...form.register('quotationDate')} className="max-w-[200px]" />
         </div>
         {form.formState.errors.quotationDate && (
           <p className="field-message text-destructive">
             {String(form.formState.errors.quotationDate.message)}
           </p>
         )}

         {/* Hàng 2: Mã KH + Tên KH */}
         <Label htmlFor="customerId" className="field-label required">Mã KH</Label>
         <CustomerAutocomplete
           inputId="customerId"
           inputAriaLabel="Mã khách hàng"
           placeholder="Nhập mã / tên / MST / địa chỉ / SĐT..."
           value={selectedCustomerView}
           errorMessage={
             form.formState.errors.customerId
               ? String(form.formState.errors.customerId.message)
               : undefined
           }
           onSelect={handleSelectCustomer}
           onClear={handleClearCustomer}
           onAddNewClick={() => setQuickAddOpen(true)}
         />
         <Label htmlFor="customerName" className="field-label">Tên KH</Label>
         <Input
           id="customerName"
           {...form.register('customerName', {
             onBlur: (e) => {
               if (!e.target.value.trim() && selectedCustomerView) {
                 form.setValue('customerName', selectedCustomerView.name, { shouldDirty: true });
               }
             },
           })}
         />

         {/* Warning Inactive */}
         {isEdit && initialSelectedCustomer?.status === 'Inactive' && (
           <p className="field-message text-amber-600">Khách hàng đã ngừng sử dụng</p>
         )}
       </div>
     </CardContent>
     ```
5. **Verify** không còn `md:col-span-2` trong card này (đã xóa).
6. **Verify** import `Label` và `Input` vẫn còn ở top file (đã có sẵn).

## Verification

```bash
cd frontend && pnpm lint
cd frontend && pnpm typecheck
cd frontend && pnpm dev
```

Manual checklist:
1. `/quotations/new`:
   - Hàng 1: Label "Ngày báo giá" (phải) + input date (trái), span đến hết.
   - Hàng 2: Mã KH (label) + autocomplete + Tên KH (label) + input — trên 1 dòng.
   - Bỏ trống customerId → submit → error "field-message" hiển thị dưới Mã.
2. Chọn KH từ popover → ô Mã hiển thị `code` (vd `KH-001`); ô Tên auto-fill name.
3. Resize cửa sổ `< 1024px` → stack thành 2 dòng (Mã 1 dòng, Tên 1 dòng).
4. `/quotations/:id` (edit mode):
   - Load lại, ô Mã hiển thị code đúng.
   - Nếu KH Inactive → warning vàng "Khách hàng đã ngừng sử dụng" hiển thị dưới grid.
5. Bấm `×` xóa selection → cả Mã và Tên đều clear (do `handleClearCustomer`).

## Exit Criteria

- `selectedCustomerView` chứa `code` ở cả init, effect, và `handleSelectCustomer`.
- Card "Thông tin khách hàng" dùng class `form-inline-grid customer-row` với template 4 cột.
- Hàng 1 = Ngày báo giá, hàng 2 = Mã + Tên KH (theo đúng JSX ở Task 4).
- Responsive `< 1024px` stack đúng (do media query trong `form-inline.css`).
- Inactive warning hiển thị bằng `.field-message text-amber-600`.
- `pnpm lint`, `pnpm typecheck`, `pnpm dev` không lỗi.
