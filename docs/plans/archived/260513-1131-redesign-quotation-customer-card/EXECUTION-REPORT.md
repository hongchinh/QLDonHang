# Execution Report — Redesign card "Thông tin khách hàng"

**Plan reference:** [SUMMARY.md](SUMMARY.md)
**Executed at:** 2026-05-13 13:42 (local)
**Mode:** Batch

## Phases

| # | Phase | Status | Complexity |
|---|---|---|---|
| 01 | CSS helper + main.tsx import | [x] complete | S |
| 02 | CustomerAutocomplete: hiển thị code + popover | [x] complete | M |
| 03 | Restructure card trong quotation form | [x] complete | M |
| 04 | Cập nhật tests | [x] complete | S |

## Files changed

- `frontend/src/styles/form-inline.css` *(new)* — helper class `.form-inline-grid`, `.field-label`, `.field-label.required`, `.field-message`, `.field-message-code`, `.field-span-rest` + media query `< 1024px`.
- `frontend/src/main.tsx` — thêm `import './styles/form-inline.css';` sau `index.css`.
- `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx`
  - Prop `value` mở rộng từ `{ id; name }` → `{ id; code; name }`.
  - Input hiển thị `value.code` khi đã chọn (thay vì `value.name`).
  - Popover: bỏ `right-0`, thêm `min-w-[min(760px,calc(100vw-80px))]` + `max-w-[calc(100vw-40px)]`, thêm meta header với 3 phím tắt.
- `frontend/src/pages/quotations/quotation-form-page.tsx`
  - `selectedCustomerView` state, init effect, `handleSelectCustomer` — đồng bộ `code`.
  - Card "Thông tin khách hàng" dùng `form-inline-grid customer-row`: hàng 1 = Ngày báo giá (span đến hết), hàng 2 = Mã KH + Tên KH.
- `frontend/src/components/customer-autocomplete/customer-autocomplete.test.tsx` — thêm 2 test: render `code` khi có selection, meta header hiển thị đúng.

## Verification commands run

| Command | Outcome |
|---|---|
| `npx tsc --noEmit` (sau phase 01) | ✓ pass |
| `npx eslint src/main.tsx` (sau phase 01) | ✓ pass |
| `npx tsc --noEmit` (sau phase 03, joint cho phase 02+03) | ✓ pass |
| `npx eslint src/components/customer-autocomplete/customer-autocomplete.tsx src/pages/quotations/quotation-form-page.tsx` | ✓ pass |
| `npx vitest run src/components/customer-autocomplete/customer-autocomplete.test.tsx` | ✓ 12/12 pass |
| `npx tsc --noEmit` (final, full project) | ✓ pass |
| `npx eslint . --ext .ts,.tsx` (final, full project) | ✓ pass |
| `npx vitest run` (final, full test suite) | ✓ 49/49 pass, 7/7 files |

## Deviations from plan

- **Per-phase verification của phase 02 bị defer**: Phase 02 đổi prop interface yêu cầu `code` nhưng page chưa được cập nhật cho đến phase 03. Vì vậy `tsc --noEmit` cho phase 02 đơn lẻ sẽ fail. Đã verify chung với phase 03 sau khi cả hai hoàn tất. Lý do: hai phase có ràng buộc type, không thể tách verify hoàn toàn.
- **Phase 01 thêm 2 class helper bonus** (`.field-message-code`, `.field-span-rest`): được người dùng cập nhật vào plan trước khi execute. Đã áp dụng đúng theo bản plan đã sửa. `.field-span-rest` được dùng trong phase 03 cho ô "Ngày báo giá".

## Residual risks / follow-ups

- **Manual visual QA chưa thực hiện**: dev server chưa được mở để verify visually. Khuyến nghị mở `/quotations/new` và `/quotations/:id` (edit mode) để kiểm tra:
  - Layout 4-cột render đúng.
  - Popover bung rộng ≥760px, có meta header.
  - Ô Mã KH hiển thị `code` (vd `KH-001`) sau khi chọn.
  - Responsive `< 1024px` stack thành 2 dòng.
  - Inactive customer hiện warning vàng "Khách hàng đã ngừng sử dụng".
- **Helper class `form-inline-grid` chưa dùng ở form khác**: sẵn sàng tái sử dụng cho phiếu thu/chi/đơn hàng tương lai.
- **Không có DB/API change** → không cần migration, không cần rollback.
