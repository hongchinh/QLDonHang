# Brand-color headers for Customer popover, Product popover, and Line-items grid

## Goal
Áp dụng brand header treatment (`bg #005bac` + `text-white` + `font-semibold`) — vốn đã ship cho 3 trang list — sang 3 surface tabular header bên trong [quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx):
1. Column `<thead>` trong popover của [customer-autocomplete.tsx](../../../frontend/src/components/customer-autocomplete/customer-autocomplete.tsx).
2. Column `<thead>` trong popover của [product-typeahead-cell.tsx](../../../frontend/src/pages/quotations/components/product-typeahead-cell.tsx).
3. `.accounting-grid <thead>` trong [line-items-grid.css](../../../frontend/src/pages/quotations/components/line-items-grid.css) (header của bảng "Chi tiết hàng hóa").

Trước khi áp dụng, user confirm visual qua mockup HTML preview cả 3 surface cùng lúc.

## Scope
- **In scope**
  - 2 file `.tsx` — 1 dòng class change mỗi file (sticky thead trong popover listbox).
  - 1 file `.css` — sửa 1 rule (`.accounting-grid th`) + thêm 1 rule mới (`.accounting-grid th.row-no`).
  - Mockup HTML `mockup-quotation-form-headers.html` cho user confirmation.
- **Out of scope**
  - Meta band ("Tìm thấy N…" + kbd hints) trong cả 2 popover — giữ muted.
  - Body row, hover, cell-input focus highlight `#fff8dc`.
  - Line-items footer, toolbar, empty placeholder.
  - Body `<td class="row-no">` (intentional muted, giữ nguyên).
  - Shared shadcn `<Table>` primitive — đã ship plan trước.
  - Bất kỳ surface tabular nào khác trong app.
  - Thêm design token `--table-header` — user đã chốt dùng hex trực tiếp.
  - Adjustment text-color/icon ngoài white-on-blue.

## Assumptions
- `CustomerAutocomplete` chỉ dùng trong `quotation-form-page.tsx` (+ test riêng); `LineItemsGrid` + `ProductTypeaheadCell` cũng chỉ dùng trong quotation form → cross-page blast radius = 0.
- Tailwind arbitrary `bg-[#005bac]` đã được emit từ plan list-page trước → thêm usage không tăng bundle đáng kể.
- Contrast `#005bac` ↔ `#ffffff` = 5.69:1 (WCAG AA pass).
- Inner header seams trong `.accounting-grid` cần override `border-right-color` + `border-bottom-color` vì parent rule paint `hsl(var(--border))` (xám nhạt) đè qua header band — sẽ xuất hiện sọc xám rõ trên xanh nếu không override.
- Không thay đổi DOM/structure; chỉ class/CSS — không có behavior/keyboard/contract test nào cần update.

## Risks
- **Spreadsheet aesthetic shift**: `.accounting-grid <thead>` rời "Hạch toán Misa gray" sang brand blue. User đã consciously confirm trong brainstorm.
- **Seam color override**: nếu áp dụng `background: #005bac` mà quên override `border-right-color`, các vạch xám 1px sẽ chia ngang/dọc qua header band. Phase 02 task 3 chỉ rõ override.
- **`th.row-no` shared selector trap**: Rule cũ áp cho cả `<th>` và `<td>`. Phải thêm rule mới CHỈ cho `<th class="row-no">`, KHÔNG đụng rule cũ — nếu sửa rule chung sẽ phá body cells.
- **Sticky thead in popover**: Phải giữ `sticky top-0` class; chỉ swap bg/text/font-weight. Mất sticky sẽ làm column header trôi khi scroll trong popover.

## Phases
- [x] Phase 01 — Mockup HTML + user confirmation gate (S) — [phase-01-mockup-confirmation.md](phase-01-mockup-confirmation.md)
- [x] Phase 02 — Apply 3-file diff + verification (S) — [phase-02-apply-headers.md](phase-02-apply-headers.md)

## Final Verification
Chạy ở thư mục `frontend/`:
```
npm run lint
npm run test
npm run build
npm run dev
```
Sau đó mở `http://localhost:5173/quotations/new`:
- Focus ô tìm Khách hàng → popover mở → column thead (Mã / Tên / MST / Địa chỉ / SĐT / Loại) nền `#005bac`, chữ trắng `semibold`. Meta band phía trên giữ muted.
- Focus 1 cell "Mã hàng" trong bảng line items → popover product mở → column thead (Mã / Tên / Loại / Quy cách / Giá bán) cùng style.
- Header bảng "Chi tiết hàng hóa" (# / Mã hàng / Tên hàng / ĐVT / Loại / D×R×Dày×Tấm / SL / Đơn giá / Giá vốn / Thành tiền) toàn bộ nền `#005bac` + chữ trắng. Không có sọc xám 1px giữa các header cell.
- Body row, cell focus `#fff8dc`, hover row — không đổi.

## Rollback / Recovery
Đảo ngược 3 file:
```
git checkout -- frontend/src/components/customer-autocomplete/customer-autocomplete.tsx \
                 frontend/src/pages/quotations/components/product-typeahead-cell.tsx \
                 frontend/src/pages/quotations/components/line-items-grid.css
```
Mockup HTML chỉ là confirmation artifact, để nguyên trong thư mục plan.
