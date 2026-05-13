# Phase 02 — Apply 3-file diff + verification

**Status:** [x] complete
**Complexity:** S

## Objective
Áp dụng brand header treatment cho 3 surface: Customer popover thead, Product popover thead, LineItemsGrid accounting-grid thead. Verify static + manual UI.

## Files
- `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx` (modify — 1 className swap)
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx` (modify — 1 className swap)
- `frontend/src/pages/quotations/components/line-items-grid.css` (modify — 1 rule update + 1 new rule)

## Tasks

### 1. Edit `frontend/src/components/customer-autocomplete/customer-autocomplete.tsx`
Tìm `<thead className="sticky top-0 bg-muted/60 text-left text-xs text-muted-foreground">` (~ dòng 224).
Đổi thành:
```tsx
<thead className="sticky top-0 bg-[#005bac] text-left text-xs font-semibold text-white">
```
**Quan trọng**:
- Giữ `sticky top-0` (popover scroll affordance).
- KHÔNG đụng `<div>` meta band phía trên (`bg-muted/30 px-2 py-1.5 text-xs text-muted-foreground`) — out of scope.
- `<th>` con không cần sửa: bị override bởi parent `text-white font-semibold`.

### 2. Edit `frontend/src/pages/quotations/components/product-typeahead-cell.tsx`
Tìm `<thead className="sticky top-0 bg-muted/60 text-left text-xs text-muted-foreground">` (~ dòng 185, bên trong `createPortal` body).
Đổi y hệt task 1:
```tsx
<thead className="sticky top-0 bg-[#005bac] text-left text-xs font-semibold text-white">
```
Cùng nguyên tắc: giữ sticky, không đụng meta band.

### 3. Edit `frontend/src/pages/quotations/components/line-items-grid.css`

**(a) Sửa rule `.accounting-grid th`** (~dòng 35–41):
```css
/* before */
.accounting-grid th {
  background: hsl(var(--muted));
  color: hsl(var(--foreground));
  font-weight: 600;
  text-align: center;
  padding: 8px 6px;
}
/* after */
.accounting-grid th {
  background: #005bac;
  color: #ffffff;
  font-weight: 600;
  text-align: center;
  padding: 8px 6px;
  border-right-color: rgba(255, 255, 255, 0.12);
  border-bottom-color: transparent;
}
```
**Lý do 2 dòng border-color cuối**: rule `.accounting-grid th, .accounting-grid td { border-right: 1px solid hsl(var(--border)); border-bottom: 1px solid hsl(var(--border)); }` (~dòng 26–33) cascade vào header → sẽ vẽ sọc xám 1px qua band xanh. Override CHỈ phần `border-*-color` (không override shorthand) để giữ 1px thickness nhưng đổi màu.

**(b) Thêm rule MỚI `.accounting-grid th.row-no`** (đặt SAU rule combined `.accounting-grid td.row-no, .accounting-grid th.row-no` hiện tại, để override đúng cascade):
```css
.accounting-grid th.row-no {
  background: #005bac;
  color: #ffffff;
}
```
**KHÔNG sửa** rule combined hiện có (`.accounting-grid td.row-no, .accounting-grid th.row-no { ... }`) — rule đó giữ nguyên cho body `<td class="row-no">` muted aesthetic.

**Tuyệt đối không đụng** các rule khác trong file (.cell-input, .dxr-cell, .line-items-footer, v.v.).

### 4. Static verification (chạy ở `frontend/`)
```
npm run lint
npm run test
npm run build
```
Kỳ vọng:
- `lint`: 0 error/warning mới.
- `test`: 54/54 pass. CustomerAutocomplete tests check labels/keyboard — không assert color → không break.
- `build`: tsc + vite build success. `bg-[#005bac]` đã được Tailwind scan từ source list-page primitive trước, không cần thay đổi config.

### 5. Manual UI verification (chạy `npm run dev`)
- Mở `http://localhost:5173/quotations/new`.
- **Case A — Customer popover**:
  - Focus ô tìm Khách hàng → gõ "a".
  - Popover mở: meta band ("Tìm thấy N…" + kbd) giữ muted. Column thead nền `#005bac`, chữ trắng `semibold`.
- **Case B — Product popover**:
  - Trong bảng "Chi tiết hàng hóa", focus 1 cell "Mã hàng" → gõ "s".
  - Popover product mở qua portal: meta band muted. Column thead `#005bac` + trắng.
- **Case C — LineItemsGrid thead**:
  - Header row "# / Mã hàng / Tên hàng / ĐVT / Loại / D×R×Dày×Tấm / SL / Đơn giá / Giá vốn / Thành tiền" nền `#005bac` đều, kể cả ô `#`.
  - KHÔNG có sọc xám 1px giữa các header cell (border-color override hoạt động).
  - Cell `#` ở header: `#005bac` + trắng.
  - Cell `#` ở body (td.row-no): giữ muted (intentional). ← CHECK kỹ.
  - Cell focus trong body input vẫn vàng `#fff8dc` (unchanged).
- **Case D — Edit mode**:
  - Mở 1 báo giá đã lưu (`/quotations/{id}`) → 3 surface render đúng style mới, không vỡ layout.
- **Case E — Console**: F12 → không có warning CSS / React mới.

## Verification
- `npm run lint`: 0 error/warning.
- `npm run test`: 54/54 pass.
- `npm run build`: success.
- Case A + B + C pass visual.
- Case D + E không regression.

## Exit Criteria
- 3 file đã edit đúng theo task 1/2/3.
- Static checks green.
- Visual A/B/C confirmed.
- Body `<td class="row-no">` vẫn muted (negative check pass).
- Phase status → `[x] complete`.
