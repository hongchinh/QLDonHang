# Phase 01 — Tạo util `money-input` + test

**Status:** [ ] pending
**Complexity:** S

## Objective
Tách logic format/parse tiền `vi-VN` ra một module độc lập, có test, để cả `LineItemsGrid` và `TotalsPanel` dùng chung. Không sửa file nào khác trong phase này.

## Files
- `frontend/src/pages/quotations/utils/money-input.ts` (mới)
- `frontend/src/pages/quotations/utils/money-input.test.ts` (mới)

## Tasks

1. Tạo file `frontend/src/pages/quotations/utils/money-input.ts` với nội dung:
   - Khai báo `const fmt = new Intl.NumberFormat('vi-VN');` ở module scope.
   - Export `parseMoneyInput(text: string): number | undefined` — copy logic từ [line-items-grid.tsx:487-497](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L487-L497):
     - Trim chuỗi; nếu rỗng → `undefined`.
     - Nếu chứa `,` → coi là phần thập phân vi-VN: bỏ tất cả `.`, đổi `,` → `.`.
     - Ngược lại, nếu match `^-?\d{1,3}(\.\d{3})+$` → bỏ tất cả `.` (kiểu phân tách ngàn).
     - Còn lại: dùng raw.
     - `Number()` rồi check `Number.isFinite`; không hữu hạn → `undefined`.
   - Export `formatMoneyForDisplay(value: unknown): string` — copy logic từ [line-items-grid.tsx:504-508](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L504-L508):
     - `undefined` / `null` / `""` → `""`.
     - Convert sang `number`. Không hữu hạn → `""`. Còn lại → `fmt.format(n)`.

2. Tạo file `frontend/src/pages/quotations/utils/money-input.test.ts` với các test case:
   - `parseMoneyInput`:
     - `"3.000.000"` → `3000000`
     - `"3000000"` → `3000000`
     - `"3,5"` → `3.5`
     - `"1.234,56"` → `1234.56`
     - `""` → `undefined`
     - `"   "` → `undefined`
     - `"abc"` → `undefined`
     - `"-1500"` → `-1500`
   - `formatMoneyForDisplay`:
     - `3000000` → `"3.000.000"`
     - `0` → `"0"`
     - `undefined` → `""`
     - `null` → `""`
     - `""` → `""`
     - `"1500"` → `"1.500"` (chấp nhận string số)
     - `NaN` → `""`

## Verification
Từ `frontend/`:
```bash
npx vitest run src/pages/quotations/utils/money-input.test.ts
npm run typecheck
```

## Exit Criteria
- File util tồn tại với 2 export đúng signature.
- 14 test case pass (8 cho parse, 6 cho format).
- `typecheck` không báo lỗi.
- Chưa có file nào trong codebase import util mới (Phase 02 mới import).
