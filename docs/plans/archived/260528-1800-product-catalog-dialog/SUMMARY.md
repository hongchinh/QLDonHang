# ProductCatalogDialog — Popup danh mục hàng hóa trong form báo giá

## Goal

Thêm button "Xem danh mục đầy đủ" ở cuối dropdown suggestion của `ProductTypeaheadCell`. Click button mở `ProductCatalogDialog` — một dialog split-pane (55% danh sách / 45% chi tiết) cho phép user browse, search, filter theo nhóm hàng, xem chi tiết sản phẩm và chọn để điền vào dòng line item hiện tại trên form báo giá.

## Scope

In scope:
- 3 component mới trong `frontend/src/features/products/components/`
- Sửa `product-typeahead-cell.tsx` để thêm button và mount dialog
- Sử dụng các hook/API đã có sẵn (không thay đổi backend)

Out of scope:
- Thay đổi backend / API endpoints
- Multi-select nhiều sản phẩm cùng lúc từ popup
- Tính năng thêm sản phẩm mới từ trong popup

## Assumptions

- `useProductGroups()` từ `@/features/products/hooks` (dùng `/lookups/product-groups`, trả về `LookupItem[]`) đủ dùng cho tabs bộ lọc
- `useProducts({ search, productGroupId, page, pageSize: 20, status: 'Active' })` đủ để tải danh sách có phân trang + filter
- `useProduct(id)` cho chi tiết sản phẩm
- `hasPermission('quotations.view_cost')` từ `useAuthStore()` điều khiển hiển thị giá nhập / lợi nhuận
- `formatMoneyForDisplay()` từ `@/pages/quotations/utils/money-input` dùng để format giá tiền
- `useDebouncedValue()` từ `@/lib/use-debounced-value` dùng để debounce input search trong popup
- `Tabs`, `TabsList`, `TabsTrigger` từ `@/components/ui/tabs` đã có sẵn
- `Dialog`, `DialogContent`, `DialogTitle` từ `@/components/ui/dialog` đã có sẵn
- Không có skeleton component sẵn — loading state dùng text hoặc inline spinner

## Risks

- `ProductTypeaheadCell` dùng `onDocClick` mousedown listener để đóng dropdown — cần đảm bảo click vào button catalog không trigger close dropdown trước khi state được set
- `DialogContent` mặc định có `p-6` và `max-w-lg` — cần override bằng className để đạt layout split-pane `max-w-5xl h-[85vh]`
- Khi dialog mở, focus management của Radix Dialog có thể conflict với focus của input trong list — cần kiểm tra

## Phases

- [x] Phase 01 — ProductCatalogDetail component (S) — `phase-01-product-catalog-detail.md`
- [x] Phase 02 — ProductCatalogList component (M) — `phase-02-product-catalog-list.md`
- [x] Phase 03 — ProductCatalogDialog wrapper (S) — `phase-03-product-catalog-dialog.md`
- [x] Phase 04 — Tích hợp vào ProductTypeaheadCell (S) — `phase-04-typeahead-integration.md`

## Final Verification

```bash
cd frontend
npm run typecheck
npm run build
```

Thủ công:
1. Mở form báo giá → gõ vào ô Mã hàng → dropdown xuất hiện → thấy button "Xem danh mục đầy đủ" ở cuối
2. Click button → dialog mở, query được pre-fill
3. Tabs nhóm hàng load và filter đúng
4. Click một dòng → right panel hiện chi tiết sản phẩm
5. Double-click dòng → dialog đóng, line item được điền
6. Nút "Chọn sản phẩm này" trong detail panel → dialog đóng, line item được điền
7. Keyboard navigation trong dropdown vẫn hoạt động bình thường

## Rollback / Recovery

Chỉ có 3 file mới và 1 file sửa. Để rollback:
- Xóa 3 files: `product-catalog-dialog.tsx`, `product-catalog-list.tsx`, `product-catalog-detail.tsx`
- Revert `product-typeahead-cell.tsx`: `git checkout -- frontend/src/pages/quotations/components/product-typeahead-cell.tsx`
