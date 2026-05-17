# Phase 02 — Refactor `line-items-grid` dùng util mới

**Status:** [ ] pending
**Complexity:** S

## Objective
Thay 2 helper cục bộ `parseMoney` và `moneyInput` trong `line-items-grid.tsx` bằng import từ `money-input.ts` ở Phase 01. Không thay đổi behavior, không đụng UI.

## Files
- `frontend/src/pages/quotations/components/line-items-grid.tsx`

## Tasks

1. Thêm import ở đầu file (cạnh các import khác):
   ```ts
   import { formatMoneyForDisplay, parseMoneyInput } from '@/pages/quotations/utils/money-input';
   ```

2. Xóa helper `parseMoney` ([line-items-grid.tsx:487-497](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L487-L497)).

3. Xóa helper `moneyInput` ([line-items-grid.tsx:504-508](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L504-L508)).

4. Xóa khai báo cục bộ `const fmt = new Intl.NumberFormat('vi-VN');` ở [line-items-grid.tsx:14](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L14) **chỉ khi** không còn chỗ nào trong file dùng `fmt` ngoài 2 helper trên. Kiểm tra:
   - [line-items-grid.tsx:383](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L383) — `fmt.format(lineTotal)` ở `cell-total-main`.
   - [line-items-grid.tsx:385](../../frontend/src/pages/quotations/components/line-items-grid.tsx#L385) — `fmt.format(lineTotal - lineCost)`.
   - **Kết luận:** `fmt` còn dùng ở 2 chỗ render → **giữ nguyên** khai báo cục bộ. (Không tách `fmt` ra util để tránh thay đổi không cần thiết.)

5. Trong các chỗ gọi `parseMoney(...)` (4 vị trí trong file — `unitPrice` × 2, `unitCost` × 2), đổi tên thành `parseMoneyInput(...)`.

6. Trong các chỗ gọi `moneyInput(line.unitPrice, ...)` và `moneyInput(line.unitCost, ...)`:
   - Thay bằng inline: `editingMoneyCellId === <id> ? (line.<field> ?? '') : formatMoneyForDisplay(line.<field>)`.
   - Lý do: util `formatMoneyForDisplay` không có flag `editing`; ràng buộc focus thuộc logic component, không thuộc util.

7. Chạy ESLint để bắt unused imports / variables.

## Verification
Từ `frontend/`:
```bash
npx vitest run src/pages/quotations/components/line-items-grid.test.tsx
npm run typecheck
npm run lint
```

## Exit Criteria
- `line-items-grid.tsx` không còn khai báo `parseMoney` hay `moneyInput`.
- `unitPrice` / `unitCost` vẫn format `vi-VN` khi không focus, raw khi focus.
- Test `line-items-grid.test.tsx` pass nguyên trạng (không đổi expectation).
- `typecheck` và `lint` sạch.
