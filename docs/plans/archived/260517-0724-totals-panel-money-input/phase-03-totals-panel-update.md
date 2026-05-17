# Phase 03 — Cập nhật `TotalsPanel`: behavior + layout

**Status:** [ ] pending
**Complexity:** M

## Objective
Sửa `EditableMetric` để 3 ô CK / VC / Thuế % nhập như tiền (text + format vi-VN + parse linh hoạt), đồng thời tinh chỉnh 4 className để CK/VC chứa được `99.999.999` và Thuế % thu nhỏ nhường chỗ cho Tiền thuế.

## Files
- `frontend/src/pages/quotations/components/totals-panel.tsx`

## Tasks

### Behavior (EditableMetric)

1. Thêm import ở đầu file:
   ```ts
   import { useState } from 'react';
   import { formatMoneyForDisplay, parseMoneyInput } from '@/pages/quotations/utils/money-input';
   ```

2. Viết lại function `EditableMetric` ([totals-panel.tsx:94-110](../../frontend/src/pages/quotations/components/totals-panel.tsx#L94-L110)):
   - Thêm state: `const [editing, setEditing] = useState(false);` và `const [draft, setDraft] = useState('');`.
   - Khi focus:
     - `setDraft(String(value ?? ''));`
     - `setEditing(true);`
     - `e.currentTarget.select();` — auto-select toàn bộ text để user gõ đè ngay (tránh case ô `"0"` thành `"05"` khi user gõ `"5"`).
   - Khi blur: `setEditing(false);`.
   - Value của `Input`:
     - Nếu `editing` → `draft`.
     - Nếu không → `formatMoneyForDisplay(value)`.
   - `onChange`:
     - `setDraft(e.target.value);`
     - `const parsed = parseMoneyInput(e.target.value);`
     - `onChange(parsed ?? 0);` — chấp nhận flash Tổng cộng khi parse fail giữa chừng (đồng nhất với `LineItemsGrid`, đã ghi nhận trong SUMMARY Risks).
   - Đổi `<Input>` props:
     - `type="number"` → `type="text"`
     - Bỏ `step="any"`
     - Thêm `inputMode="decimal"`
     - Thêm `autoComplete="off"`

### Layout (4 chỉnh nhỏ)

3. `SummaryRow` className ([totals-panel.tsx:80](../../frontend/src/pages/quotations/components/totals-panel.tsx#L80)):
   - Từ: `'grid grid-cols-[86px_1fr] items-center gap-3'`
   - Thành: `'grid grid-cols-[64px_1fr] items-center gap-2'`

4. Label `<span>` trong `SummaryRow` ([totals-panel.tsx:81](../../frontend/src/pages/quotations/components/totals-panel.tsx#L81)):
   - Thêm `whitespace-nowrap` vào cả hai nhánh className.
   - Ví dụ: `emphasized ? 'text-sm font-medium whitespace-nowrap' : 'text-sm text-muted-foreground whitespace-nowrap'`.

5. Hàng "Điều chỉnh" inner grid ([totals-panel.tsx:27](../../frontend/src/pages/quotations/components/totals-panel.tsx#L27)):
   - Từ: `'grid min-w-0 grid-cols-2 gap-2'`
   - Thành: `'grid min-w-0 grid-cols-2 gap-1'`

6. Hàng "Thuế" inner grid ([totals-panel.tsx:44](../../frontend/src/pages/quotations/components/totals-panel.tsx#L44)):
   - Từ: `'grid min-w-0 grid-cols-[92px_1fr] items-end gap-2'`
   - Thành: `'grid min-w-0 grid-cols-[56px_1fr] items-end gap-2'`

7. `Input` trong `EditableMetric` ([totals-panel.tsx:106](../../frontend/src/pages/quotations/components/totals-panel.tsx#L106)):
   - Từ: `'h-8 px-2 text-right tabular-nums'`
   - Thành: `'h-8 px-1.5 text-right tabular-nums'`

## Verification

Từ `frontend/`:
```bash
npm run typecheck
npm run lint
```

Manual smoke (`npm run dev` → http://localhost:5173/quotations/new):

| # | Bước | Kết quả mong đợi |
|---|---|---|
| 1 | Click ô CK, gõ `99999999`, blur | Ô hiển thị `"99.999.999"` đầy đủ, không bị cắt phải, không tràn cột VC |
| 2 | Click lại ô CK | Hiển thị `"99999999"` raw, toàn bộ text được select sẵn |
| 3 | Xóa hết ô CK, blur | Ô hiển thị `"0"`; ô Tổng cộng cập nhật |
| 3b | Click ô CK đang là `"0"`, gõ `"5"` | Field ngay thành `"5"`, KHÔNG thành `"05"` (auto-select on focus) |
| 4 | Lặp 1–3 với VC | Tương tự CK |
| 5 | Click Thuế %, gõ `10`, blur | Hiển thị `"10"`; Tiền thuế cập nhật và hiển thị rộng hơn |
| 6 | Click Thuế %, gõ `8,5`, blur | Hiển thị `"8,5"` (vi-VN); Tiền thuế phù hợp |
| 7 | Kiểm tra label "Điều chỉnh", "Thuế", "Tổng cộng" | Không bị wrap, vẫn nằm 1 dòng |
| 8 | Zoom browser 110% (`Ctrl` + `+`), lặp bước 1 | `"99.999.999"` vẫn không bị cắt ở CK và VC |
| 9 | Mở trang Chi tiết hàng hóa, gõ Đơn giá `2500000` | Vẫn format `"2.500.000"` khi blur (Phase 02 không phá vỡ) |

## Exit Criteria
- 3 ô số ở Tổng cộng dùng `type="text"`, không còn spinner browser.
- `99.999.999` ở CK/VC hiển thị đầy đủ trong card 320px hiện tại.
- Thuế % thu nhỏ (~44px text width), Tiền thuế chiếm hơn 60% vùng phải của hàng Thuế.
- `typecheck` và `lint` sạch.
- `LineItemsGrid` không hồi quy.
