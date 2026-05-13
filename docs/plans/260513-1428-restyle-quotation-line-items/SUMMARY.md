# Restyle "Chi tiết hàng hóa" & cân chiều cao "Tổng cộng" theo mockup Phiếu thu

## Goal
Restyle bảng "Chi tiết hàng hóa" trên trang [quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx) trông giống 100% phần "Hạch toán" của mockup [ui_form_them_moi_phieu_thu_html.html](../../bd/ui_form_them_moi_phieu_thu_html.html); đồng thời restructure layout để card "Tổng cộng" có chiều cao bằng card "Thông tin chung" (CSS Grid stretch). Không thay đổi schema, payload, hay data flow.

## Scope
- **In scope**
  - Tạo CSS scoped riêng cho bảng line items (hex hardcode từ mockup).
  - Refactor `LineItemsGrid` sang native `<table>`/`<input>` với class `.accounting-grid`, `.cell-input`, v.v.
  - Thêm nút "Xóa tất cả dòng" bằng `ConfirmDialog` hiện có + giữ "Thêm dòng" + giữ Trash từng dòng.
  - Wire `Insert` (thêm dòng), `Ctrl+Delete` (xóa dòng đang focus) — scope listener bên trong `.accounting-grid-wrap`.
  - Footer bảng với kbd hints + "Tổng: <subtotal>".
  - Cột "Loại" chuyển từ `Badge` sang text muted nhỏ.
  - Bỏ `Input` shadcn ở cột "Mã hàng": thêm prop `variant="cell"` cho `ProductTypeaheadCell`.
  - Render dropdown của `ProductTypeaheadCell` qua portal để không bị clip bởi vùng scroll ngang của bảng.
  - `TotalsPanel` bỏ `sticky top-4`, dùng `h-full flex flex-col`; nhóm "Tổng giá vốn / Lợi nhuận gộp" neo đáy bằng `mt-auto`.
  - Restructure layout `quotation-form-page.tsx`: top row 2 cột `[Thông tin chung | Tổng cộng]`, Chi tiết hàng hóa full-width dưới.
  - Thêm targeted unit tests cho `LineItemsGrid` covering add/remove/clear-all/subtotal/keyboard.
- **Out of scope**
  - Wire Tab/Enter navigation thật giữa các cell (chỉ visual hint trong footer).
  - Sticky header, max-height scroll (mockup có nhưng user chọn không).
  - Schema, payload, validation, data flow.
  - Style `CustomerAutocomplete` hay card "Thông tin chung".
  - Bottom-bar fixed footer (mockup có nhưng ngoài scope).
  - Thay đổi pricing mode (cột "Loại" vẫn read-only).

## Assumptions
- `frontend/src/components/ui/confirm-dialog.tsx` tồn tại → dùng `ConfirmDialog` cho "Xóa tất cả dòng" để giữ UI consistency; không dùng native `window.confirm()`.
- `ProductTypeaheadCell` hiện chỉ được dùng trong `LineItemsGrid` (sẽ verify trước khi sửa); thêm prop `variant?: 'default' | 'cell'` default = `'default'` để backward compatible nếu có nơi khác dùng.
- Shadcn `Card` cho phép `className` truyền thẳng qua `cn(...)` → `h-full flex flex-col` áp dụng đúng.
- CSS Grid mặc định `align-items: stretch` đảm bảo 2 card cùng row stretch bằng nhau khi viewport ≥ `lg` (1024px).
- Font Segoe UI có sẵn trên Windows (mockup target chính). Browser khác sẽ fallback sang Arial — chấp nhận.
- `useFieldArray` của react-hook-form vẫn là API chính cho `lines`; không thay state model.

## Risks
- **ProductTypeaheadCell backward compat**: Nếu có nơi khác dùng (chưa kiểm), default vẫn phải render shadcn Input. Phase 2 verify trước khi sửa.
- **ProductTypeaheadCell dropdown clipping**: `.accounting-grid-wrap` có `overflow-x: auto`, nên dropdown `absolute` trong cell có thể bị cắt. Phase 2 phải render dropdown qua portal và Phase 6 verify suggestion không bị clip.
- **`Card` flex stretch**: CardContent shadcn có `p-6 pt-0` cứng; thêm `flex flex-col flex-1` qua `className` phải lan đúng. Verify bằng DevTools trong Phase 6.
- **`Insert`/`Ctrl+Delete` listener leak**: Phải bind trên container `.accounting-grid-wrap` ref, không bind `document`. `e.preventDefault()` để Insert không kích hoạt overwrite mode native.
- **Build CSS import**: Vite/CRA cho phép `import './line-items-grid.css'`. Verify type config không chặn (`tsconfig.json` cho `.css` import). Nếu lỗi: dùng global `index.css` thay (fallback).
- **Mockup `min-width: 1180px` trên `.accounting-grid`**: nếu áp đúng số này, bảng có thể tràn ngang ở viewport hẹp. Plan dùng width tổng từ tổng các column width (~1188px), bọc `.accounting-grid-wrap` với `overflow-x: auto` để không vỡ layout responsive.

## Phases
- [ ] Phase 01 — CSS scoped file (S) — [phase-01-css-scaffold.md](phase-01-css-scaffold.md)
- [ ] Phase 02 — ProductTypeaheadCell variant prop (S) — [phase-02-typeahead-variant.md](phase-02-typeahead-variant.md)
- [ ] Phase 03 — LineItemsGrid refactor + keyboard + delete-all (L) — [phase-03-line-items-grid-refactor.md](phase-03-line-items-grid-refactor.md)
- [ ] Phase 04 — TotalsPanel stretch (S) — [phase-04-totals-panel-stretch.md](phase-04-totals-panel-stretch.md)
- [ ] Phase 05 — Layout restructure trong quotation-form-page (S) — [phase-05-form-page-layout.md](phase-05-form-page-layout.md)
- [ ] Phase 06 — Final verification (S) — [phase-06-final-verification.md](phase-06-final-verification.md)
- [ ] Phase 07 — ProductTypeaheadCell upgrade Misa-pattern (M) — [phase-07-product-typeahead-misa-upgrade.md](phase-07-product-typeahead-misa-upgrade.md)

## Final Verification
Chạy ở thư mục `frontend/`:
```
npm run lint
npm run test -- line-items-grid
npm run build
npm run dev
```
Sau đó mở `http://localhost:5173/quotations/new` (hoặc port dev tương ứng) và test các case ở Phase 06.

## Rollback / Recovery
Tất cả thay đổi nằm trong:
- `frontend/src/pages/quotations/quotation-form-page.tsx`
- `frontend/src/pages/quotations/components/line-items-grid.tsx`
- `frontend/src/pages/quotations/components/line-items-grid.test.tsx` (file mới)
- `frontend/src/pages/quotations/components/line-items-grid.css` (file mới)
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`
- `frontend/src/pages/quotations/components/totals-panel.tsx`

Rollback bằng `git checkout -- <file>` cho các file đã sửa + `del frontend\src\pages\quotations\components\line-items-grid.css` và `del frontend\src\pages\quotations\components\line-items-grid.test.tsx` cho file mới.
