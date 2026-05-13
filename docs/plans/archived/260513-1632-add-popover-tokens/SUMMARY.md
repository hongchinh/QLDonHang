# Add missing `--popover` design tokens to fix transparent search dropdowns

## Goal
Khôi phục `bg-popover` / `text-popover-foreground` cho 2 dropdown tìm kiếm — Khách hàng ([customer-autocomplete.tsx](../../../frontend/src/components/customer-autocomplete/customer-autocomplete.tsx)) và Mã hàng trong line items ([product-typeahead-cell.tsx](../../../frontend/src/pages/quotations/components/product-typeahead-cell.tsx)) — bằng cách bổ sung 2 design tokens (`--popover`, `--popover-foreground`) đang thiếu trong [index.css](../../../frontend/src/index.css) và đăng ký màu `popover` trong [tailwind.config.ts](../../../frontend/tailwind.config.ts). Trước khi áp dụng, user cần confirm visual qua mockup HTML.

## Scope
- **In scope**
  - Thêm 4 dòng vào [index.css](../../../frontend/src/index.css) (2 ở `:root`, 2 ở `.dark`).
  - Đăng ký `popover: { DEFAULT, foreground }` trong [tailwind.config.ts](../../../frontend/tailwind.config.ts).
  - Tạo mockup HTML `mockup-dropdown.html` trong thư mục plan để user confirm visual trước khi apply.
- **Out of scope**
  - Sửa JSX của `CustomerAutocomplete` hay `ProductTypeaheadCell` (class `bg-popover` đã có sẵn — chỉ cần token).
  - Layout, animation, max-height, shadow, border, z-index, keyboard behavior của dropdown.
  - Refactor sang shadcn `<Popover>` primitive.
  - Thêm regression test cho computed background (visual-only fix, không có DOM contract thay đổi).

## Assumptions
- Token values clone từ `--card` / `--card-foreground` (shadcn default):
  - Light: `--popover: 0 0% 100%` / `--popover-foreground: 222.2 84% 4.9%`
  - Dark: `--popover: 222.2 84% 4.9%` / `--popover-foreground: 210 40% 98%`
- Tailwind v3 với content scan đã cover `bg-popover` (class đang ở source code → sẽ generate utility ngay khi color được register).
- Không có nơi nào trong codebase đang `var(--popover)` trực tiếp (đã grep — 0 match) → tokens thuần additive.
- User sẽ confirm mockup HTML trước khi cho phép Phase 02 chạy.

## Risks
- **Visual mismatch với mockup**: Nếu hex `#fff` (light mode) trông quá "phẳng" so với background hiện tại, user có thể muốn shade khác (ví dụ `0 0% 99%`). Mockup chính là gate để bắt sớm trước khi sửa code.
- **Cache build CSS**: sau khi đổi tailwind.config.ts, Vite dev server đôi khi cần restart (không tự HMR config). Phase 02 verification dùng `npm run build` để bypass cache.
- **Future-bug surface**: Sau khi popover tokens tồn tại, mọi component dùng shadcn `<Popover>`/`<Tooltip>` trong tương lai sẽ tự lấy đúng màu — kỳ vọng đúng. Không phát sinh rủi ro mới.

## Phases
- [x] Phase 01 — Mockup HTML + user confirmation gate (S) — [phase-01-mockup-confirmation.md](phase-01-mockup-confirmation.md)
- [x] Phase 02 — Apply popover tokens + verification (S) — [phase-02-apply-tokens.md](phase-02-apply-tokens.md)

## Final Verification
Chạy ở thư mục `frontend/`:
```
npm run lint
npm run test
npm run build
npm run dev
```
Sau đó mở `http://localhost:5173/quotations/new`:
- Focus ô tìm Khách hàng → gõ "a" → dropdown nền **trắng đặc**, không thấy item phía dưới xuyên qua.
- Focus 1 cell "Mã hàng" trong bảng line items → gõ "s" → dropdown product cũng nền trắng đặc.

## Rollback / Recovery
Đảo ngược 2 file:
```
git checkout -- frontend/src/index.css frontend/tailwind.config.ts
```
Mockup HTML chỉ là confirmation artifact — không có production reference, để nguyên trong thư mục plan.
