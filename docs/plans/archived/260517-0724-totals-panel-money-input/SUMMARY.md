# Đồng bộ ô nhập số ở Tổng cộng + tối ưu width cho CK/VC

## Goal
Làm cho 3 ô số trong `TotalsPanel` (CK / VC / Thuế %) có hành vi nhập tiền giống ô Đơn giá / Giá vốn trong `LineItemsGrid` — format `vi-VN` khi blur, raw khi focus, parse linh hoạt — đồng thời tinh chỉnh width trong card 320px để CK / VC hiển thị trọn `99.999.999` không bị cắt, và thu nhỏ ô Thuế % để Tiền thuế có thêm chỗ.

## Scope
- **In scope:**
  - Tạo util `money-input.ts` dùng chung cho format/parse tiền vi-VN.
  - Refactor `line-items-grid.tsx` import từ util mới (gỡ helper cục bộ).
  - Sửa `EditableMetric` trong `totals-panel.tsx`: đổi `type="number"` → `type="text" inputMode="decimal"`, thêm state focus, dùng helper format/parse.
  - Tinh chỉnh 4 vị trí className trong `totals-panel.tsx` để nhường width cho CK/VC và thu nhỏ ô Thuế %.
  - Unit test cho `money-input.ts`.
- **Out of scope:**
  - Không đổi `lg:grid-cols-[1fr_320px]` ở [quotation-form-page.tsx:351](../../frontend/src/pages/quotations/quotation-form-page.tsx#L351).
  - Không stack dọc CK/VC.
  - Không đổi schema (`taxRate`, `discount`, `freight` vẫn là `number` trong `quotationSchema`).
  - Không refactor `TotalsPanel` sang dùng react-hook-form trực tiếp — vẫn dùng props callback hiện tại.
  - Không thay UI `<Input>` thành `cell-input`.

## Assumptions
- Helper `parseMoney` và `moneyInput` ở [line-items-grid.tsx:487-508](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L487-L508) là logic đầy đủ, có thể tách nguyên trạng.
- Form schema cho `discount`, `freight`, `taxRate` cho phép nhận `0` khi user xóa hết (mặc định trong `toFormDefaults` đã là `0`).
- Vitest đã được cấu hình cho `.test.ts` trong `frontend/src/` (đã có `line-items-grid.test.tsx` chạy được).
- Card Totals giữ width 320px → sau khi co label hàng từ 86 → 64px và padding input từ `px-2` → `px-1.5`, mỗi ô CK/VC còn ~98px → đủ chứa `"99.999.999"` (~80px ở `text-sm tabular-nums`).

## Risks
- **Format `vi-VN` cho Thuế % có thể gây nhầm:** Thuế 100 hiển thị "100", 1000 hiển thị "1.000" — chấp nhận, không thực tế đạt mốc đó.
- **Khi value = 0 blur thấy "0":** UX hơi "ồn" cho ô rỗng — đã đồng thuận chấp nhận trong brainstorm. Mitigation: auto-`select()` on focus để user gõ là đè ngay, không phải xóa "0" thủ công.
- **Tổng cộng "flash" khi gõ giữa chừng:** Gõ `"3.000.0"` (đang tiến tới `3.000.000`) → `parseMoneyInput` trả `undefined` (regex không match, `Number()` ra `NaN`) → `onChange(0)` → Tổng cộng tạm về subtotal rồi phục hồi khi gõ tiếp. Hành vi đồng nhất với `LineItemsGrid` hiện tại, chấp nhận. Nếu sau này muốn fix: chỉ gọi `onChange` khi `parsed !== undefined`, để giá trị cũ "stick" trong lúc parse fail; cần handle riêng case xóa hết → onBlur ép về 0.
- **Refactor import ở line-items-grid:** Nếu sót chỗ nào còn gọi `parseMoney` / `moneyInput` cục bộ → typecheck sẽ bắt được ngay.
- **Co label hàng 86 → 64px:** "Điều chỉnh" (9 ký tự) có thể wrap nếu user tăng font ≥ 18px. Mitigation: thêm `whitespace-nowrap`.
- **Width budget CK/VC tính khít (~10px buffer):** `"99.999.999"` ≈ 80–84px ở `text-sm tabular-nums`, cell ~96px sau khi trừ border Input. Ở zoom ≥ 110% hoặc khi OS render scrollbar overlay, có thể tràn. Mitigation: thêm bước test zoom 110% vào manual smoke (xem Final Verification bước 7).

## Phases
- [ ] Phase 01 — Tạo util `money-input` + test (S) — [phase-01-money-input-util.md](phase-01-money-input-util.md)
- [ ] Phase 02 — Refactor `line-items-grid` dùng util mới (S) — [phase-02-refactor-line-items-grid.md](phase-02-refactor-line-items-grid.md)
- [ ] Phase 03 — Cập nhật `TotalsPanel`: behavior + layout (M) — [phase-03-totals-panel-update.md](phase-03-totals-panel-update.md)

## Final Verification
Chạy từ `frontend/`:
```bash
npm run typecheck
npm run test
npm run lint
```
Manual smoke trên `npm run dev` tại trang `/quotations/new`:
1. Nhập `99999999` vào ô CK → blur → thấy `"99.999.999"` đầy đủ, không bị cắt.
2. Click lại ô CK (đang là `0`) → text được select sẵn, gõ `"5"` đè ngay thành `"5"`, không thành `"05"`.
3. Xóa hết → ô trống → blur → thấy `"0"`.
4. Lặp tương tự với VC.
5. Thuế % nhập `10` → thấy `"10"`, ô Tiền thuế bên cạnh rộng hơn trước.
6. Kiểm tra `LineItemsGrid`: ô Đơn giá / Giá vốn vẫn format & parse như cũ.
7. Zoom browser lên 110% (`Ctrl` + `+`) → lặp bước 1 → `"99.999.999"` vẫn không bị cắt ở CK và VC.

## Rollback / Recovery
- Mỗi phase chỉ chạm 1–2 file. Nếu Phase 03 hỏng UI, `git restore frontend/src/pages/quotations/components/totals-panel.tsx` để revert.
- Nếu Phase 02 refactor gây lỗi grid, revert `git restore frontend/src/pages/quotations/components/line-items-grid.tsx` — util mới (Phase 01) có thể giữ nguyên vì không bị import bởi file nào khác.
- Util `money-input.ts` đứng độc lập, gỡ luôn nếu cần: `rm frontend/src/pages/quotations/utils/money-input.ts frontend/src/pages/quotations/utils/money-input.test.ts`.
