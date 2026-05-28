# CustomerCatalogDialog — Popup danh mục khách hàng trong form báo giá

## Goal

Thêm button "Xem danh mục đầy đủ" ở cuối dropdown suggestion của `CustomerAutocomplete`. Click button mở `CustomerCatalogDialog` — dialog single-pane cho phép browse, search, filter theo nhóm khách hàng và chọn để điền vào dòng khách hàng trên form báo giá, bao gồm pre-fill địa chỉ giao hàng.

## Scope

In scope:
- 2 component mới trong `frontend/src/features/customers/components/`
- Sửa `customer-autocomplete.tsx` để thêm button và mount dialog
- Test cho cả 3 thay đổi
- Không thay đổi backend / API endpoints

Out of scope:
- Split-pane layout với customer detail panel
- Multi-select nhiều khách hàng cùng lúc
- Thêm mới khách hàng từ trong popup
- Thay đổi kiểu dữ liệu `CustomerSearchItem`

## Assumptions

- `useCustomers({ page, pageSize, search, group, status: 'Active' })` đủ để tải danh sách có phân trang + filter
- `customersApi.get(id)` trả về `Customer` đầy đủ bao gồm `companyAddress`, `defaultShippingAddress`
- `CustomerGroup` type cố định: `Company | Agent | Retail | Project` — không cần lookup API
- `Dialog`, `DialogContent`, `DialogTitle` từ `@/components/ui/dialog` đã có sẵn
- `Tabs`, `TabsList`, `TabsTrigger` từ `@/components/ui/tabs` đã có sẵn
- `useDebouncedValue` từ `@/lib/use-debounced-value` đã có sẵn
- Button catalog nằm trong `containerRef` của `CustomerAutocomplete` → `onDocClick` KHÔNG đóng dropdown khi click (vì `containerRef.current.contains()` = true)
- `toast` từ `@/lib/use-toast` — đã xác nhận export tên `toast` đúng ở `frontend/src/lib/use-toast.ts`

## Risks

- Việc gọi `customersApi.get(id)` async khi chọn cần error handling để tránh silent failure
- `CustomerCatalogDialog` được mount bên trong `CustomerAutocomplete` → các test của `CustomerAutocomplete` cần mock thêm `customersApi.list` để tránh unhandled query errors

## Phases

- [ ] Phase 01 — CustomerCatalogList component (M) — `phase-01-customer-catalog-list.md`
- [ ] Phase 02 — CustomerCatalogDialog wrapper (S) — `phase-02-customer-catalog-dialog.md`
- [ ] Phase 03 — Tích hợp vào CustomerAutocomplete (S) — `phase-03-autocomplete-integration.md`

## Final Verification

```bash
cd frontend
npm run typecheck
npm run build
```

Thủ công:
1. Mở form báo giá → gõ vào ô Mã khách hàng → dropdown xuất hiện → thấy button "Xem danh mục đầy đủ" ở cuối
2. Click button → dropdown đóng, dialog mở với search pre-fill
3. Tabs nhóm filter đúng (Tất cả / Công ty / Đại lý / Khách lẻ / Công trình)
4. Click dòng → dialog đóng, khách hàng được điền vào form
5. Địa chỉ giao hàng được pre-fill từ `defaultShippingAddress` hoặc `companyAddress`
6. Keyboard navigation trong dropdown suggestion vẫn hoạt động bình thường

## Rollback / Recovery

Chỉ có 2 file mới và 2 file sửa (component + test). Để rollback:
- Xóa: `frontend/src/features/customers/components/customer-catalog-list.tsx`
- Xóa: `frontend/src/features/customers/components/customer-catalog-dialog.tsx`
- Xóa: `frontend/src/features/customers/components/customer-catalog-list.test.tsx`
- Xóa: `frontend/src/features/customers/components/customer-catalog-dialog.test.tsx`
- Revert: `git checkout -- frontend/src/components/customer-autocomplete/customer-autocomplete.tsx`
- Revert: `git checkout -- frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx`
