# Redesign card "Thông tin khách hàng" trong Quotation Form

## Goal
Làm lại bố cục card "Thông tin khách hàng" trong [quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx): đặt Mã KH và Tên KH trên cùng một dòng theo style inline label tham khảo từ [ui_form_them_moi_phieu_thu_html.html](../../bd/ui_form_them_moi_phieu_thu_html.html); đổi `CustomerAutocomplete` hiển thị `code` thay vì `name` sau khi chọn; mở rộng popover suggestion và thêm meta header (số kết quả + keyboard hints).

## Scope

**In scope**
- Tạo helper CSS `frontend/src/styles/form-inline.css` và import vào `main.tsx`.
- Mở rộng prop `value` của `CustomerAutocomplete` từ `{ id; name }` → `{ id; code; name }`.
- Đổi hiển thị input đã chọn từ `name` sang `code`.
- Thêm meta header (số kết quả + kbd hints) cho popover và mở rộng width tối thiểu 760px.
- Restructure card "Thông tin khách hàng": hàng 1 = Ngày báo giá, hàng 2 = Mã KH + Tên KH (grid `90px minmax(180px,1fr) 90px minmax(220px,2fr)`).
- Responsive `< 1024px`: stack thành 2 dòng.
- Cập nhật `customer-autocomplete.test.tsx` cho hiển thị `code` và meta header.

**Out of scope**
- Các card khác trong quotation form (Giao hàng, Chi tiết hàng hóa, Ghi chú nội bộ) — giữ nguyên.
- Schema/types/API backend — không đổi.
- Các form khác sử dụng pattern tương tự (phiếu thu/chi/đơn hàng) — chỉ tạo helper class CSS dùng chung sẵn, không áp dụng cho form khác trong plan này.
- E2E test runtime — chỉ unit test với vitest.

## Assumptions

- `Customer.code` và `CustomerSearchItem.code` đều là `string` required (đã verify trong [types.ts](../../../frontend/src/features/customers/types.ts)).
- `CustomerAutocomplete` chỉ được dùng ở 1 nơi: [quotation-form-page.tsx](../../../frontend/src/pages/quotations/quotation-form-page.tsx) (đã grep verify).
- `main.tsx` đang import `index.css` (đã verify); thêm import CSS mới ngay sau.
- Test hiện tại không assert giá trị input sau khi chọn (chỉ assert callback `onSelect`) → không break test cũ khi đổi hiển thị từ `name` sang `code`.
- Project đang dùng Tailwind + shadcn; class custom được giữ tối thiểu, chỉ cho phần inline-grid mà Tailwind utility khó tái sử dụng.
- HSL variable `--muted-foreground`, `--destructive` đã được khai báo trong `index.css` (theo chuẩn shadcn).

## Risks

- **Test `customer-autocomplete.test.tsx` flakiness**: vài test mới (meta header) dùng text Tiếng Việt có ký tự đặc biệt (`·`). Mitigation: dùng regex hoặc text content cụ thể.
- **HSL variable không tồn tại**: nếu `--muted-foreground` không khai báo, class `.field-label` không hiển thị màu mong muốn. Mitigation: verify trong `index.css` trước khi merge; fallback dùng giá trị màu cố định.
- **Popover overflow viewport**: khi `min-width: 760px` mà card nằm ở mép phải màn hình hẹp, popover có thể tràn. Mitigation: dùng `min-w-[min(760px,calc(100vw-80px))]` đảm bảo không vượt viewport.
- **Auto-fill `customerName` race condition**: trong `handleSelectCustomer` (page-level) gọi `setValue('customerName', c.name)`. Nếu autocomplete vừa render `code`, vừa setValue tên KH cho input liền kề → an toàn vì hai input độc lập, không có conflict.

## Phases

- [x] Phase 01 — CSS helper + main.tsx import (S) — `phase-01-css-helper.md`
- [x] Phase 02 — CustomerAutocomplete: hiển thị code + popover (M) — `phase-02-autocomplete.md`
- [x] Phase 03 — Restructure card trong quotation form (M) — `phase-03-quotation-form-card.md`
- [x] Phase 04 — Cập nhật tests (S) — `phase-04-tests.md`

## Final Verification

Sau khi cả 4 phase hoàn tất:

```bash
# Lint + typecheck
cd frontend && pnpm lint && pnpm typecheck

# Unit tests
cd frontend && pnpm test customer-autocomplete

# Dev server smoke test
cd frontend && pnpm dev
# Mở /quotations/new:
# - Card "Thông tin khách hàng" có 2 hàng: Ngày báo giá → Mã KH + Tên KH
# - Gõ Mã KH → popover bung rộng ≥760px, có meta header
# - Chọn KH → input Mã hiển thị code (vd "KH-001"), Tên auto-fill
# - Resize < 1024px → stack 2 dòng
# - Inactive customer (edit mode) → warning vàng dưới ô Mã, không vỡ grid
```

## Rollback / Recovery

- Tất cả thay đổi chỉ ở frontend. Revert commit là đủ.
- Không có DB migration, không có API change.
- Nếu cần rollback một phần: phase 02 và 03 có thể revert độc lập (phase 02 chỉ đổi behavior + popover trong component; phase 03 chỉ đổi JSX trong page). Phase 01 là CSS helper — an toàn để giữ lại kể cả khi revert phase khác.
