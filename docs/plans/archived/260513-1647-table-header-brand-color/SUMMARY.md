# Restyle shared Table header with brand color #005bac + white semibold text

## Goal
Đổi style header của shadcn `<Table>` primitive sang nền `#005bac` + chữ trắng `font-semibold`, áp dụng đồng thời cho 3 trang list (Khách hàng / Sản phẩm / Báo giá). Trước khi sửa code production, user confirm visual qua mockup HTML — mockup cũng demo 2 trạng thái (có/không `overflow-hidden` trên `<Card>`) để user quyết định có cần fix corner artifact hay không.

## Scope
- **In scope**
  - Sửa [frontend/src/components/ui/table.tsx](../../../frontend/src/components/ui/table.tsx): `TableHeader` thêm `bg-[#005bac] [&_tr]:border-b-0`; `TableHead` đổi từ `font-medium text-muted-foreground` → `font-semibold text-white`.
  - Conditional: thêm `overflow-hidden` lên `<Card>` (hoặc wrapper tương đương) của 3 trang list — CHỈ khi mockup hoặc manual verification cho thấy có "square blue corner" lộ trên rounded card.
  - Tạo `mockup-table-header.html` self-contained trong thư mục plan: render sample list (5 dòng KH), thể hiện 2 state Card có/không `overflow-hidden` side-by-side, annotate contrast 5.69:1 WCAG AA.
- **Out of scope**
  - Per-list color variants (xanh khác cho Sản phẩm, v.v.).
  - Body-row restyle, hover tuning, zebra stripes.
  - Sort/filter/pagination UX.
  - Thêm design token `--table-header` — user đã chốt dùng hex trực tiếp.
  - Dark-mode tinh chỉnh riêng — `#005bac` + white đạt AA cả 2 mode.

## Assumptions
- 3 trang list sử dụng shadcn `<Table>` primitive chung: [customer-list-page.tsx](../../../frontend/src/pages/customers/customer-list-page.tsx), [product-list-page.tsx](../../../frontend/src/pages/products/product-list-page.tsx), [quotation-list-page.tsx](../../../frontend/src/pages/quotations/quotation-list-page.tsx). User đã chấp nhận cả 3 trang inherit style mới.
- `cn(...)` trong `TableHead`/`TableHeader` merge `className` từ caller cuối → nếu page nào có override sẽ vẫn win, không phá.
- Lucide icons trong header (nếu có) dùng `currentColor` → tự đổi sang trắng cùng text.
- shadcn `<Card>` default class chứa `rounded-lg` nhưng KHÔNG `overflow-hidden`; nếu hiển thị "square blue corner" thì Phase 02 sẽ apply thêm fix.
- Contrast `#005bac` vs `#ffffff` = 5.69:1 (WCAG AA pass cho normal text).

## Risks
- **Cross-list visual surprise**: 3 trang đồng loạt đổi header — user đã confirm scope nên không phải rủi ro, chỉ là thay đổi diện rộng. Để giảm bất ngờ, mockup show đúng visual mục tiêu.
- **Per-page className conflict**: Nếu một page nào đó pass `className` cho `<TableHead>` chứa text-color → có thể override màu trắng. Phase 02 task 1 grep trước khi apply.
- **Corner artifact**: Header band đầy `#005bac` có thể spill ra ngoài rounded corner của Card. Mockup demo cả 2 state (có/không `overflow-hidden`) để user quyết định ngay trong Phase 01 — tránh phải sửa thêm sau khi đã apply primitive.
- **Tailwind arbitrary value cache**: `bg-[#005bac]` được Tailwind scan từ source nên cần `npm run build` (không HMR) để chắc utility emit đúng.

## Phases
- [x] Phase 01 — Mockup HTML + user confirmation gate (S) — [phase-01-mockup-confirmation.md](phase-01-mockup-confirmation.md)
- [x] Phase 02 — Apply table primitive change + conditional Card fix + verification (S) — [phase-02-apply-header-style.md](phase-02-apply-header-style.md)

## Final Verification
Chạy ở thư mục `frontend/`:
```
npm run lint
npm run test
npm run build
npm run dev
```
Sau đó mở browser:
- `http://localhost:5173/customers` — header `#005bac`, chữ trắng semibold, không bị square corner.
- `http://localhost:5173/products` — header style giống.
- `http://localhost:5173/quotations` — header style giống.
- (Optional) Lighthouse contrast audit trên `/customers` không flag header.

## Rollback / Recovery
Đảo ngược các file đã sửa:
```
git checkout -- frontend/src/components/ui/table.tsx
# Nếu Phase 02 cũng sửa Card overflow-hidden:
git checkout -- frontend/src/pages/customers/customer-list-page.tsx \
                 frontend/src/pages/products/product-list-page.tsx \
                 frontend/src/pages/quotations/quotation-list-page.tsx
```
Mockup HTML chỉ là confirmation artifact, để nguyên trong thư mục plan.
