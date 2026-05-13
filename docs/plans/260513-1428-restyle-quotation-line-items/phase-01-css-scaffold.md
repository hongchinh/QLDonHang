# Phase 01 — CSS scoped file

**Status:** [x] complete
**Complexity:** S

## Objective
Tạo file CSS scoped chứa toàn bộ rules `.accounting-grid` mirror mockup để các phase sau import. CSS đứng độc lập, có thể test bằng cách tạm import vào file bất kỳ — không phá UI hiện tại.

## Files
- `frontend/src/pages/quotations/components/line-items-grid.css` (mới)

## Tasks

1. Tạo file `frontend/src/pages/quotations/components/line-items-grid.css` với nội dung dưới đây (hardcode hex từ mockup, không dùng CSS variable toàn cục để cô lập):

```css
/* line-items-grid.css
 * Scoped styles cho bảng "Chi tiết hàng hóa" — mirror phần "Hạch toán"
 * của docs/bd/ui_form_them_moi_phieu_thu_html.html
 */

.accounting-grid-wrap {
  overflow-x: auto;
  border-top: 1px solid #d7dde5;
}

.accounting-grid {
  width: 100%;
  min-width: 1180px;
  border-collapse: collapse;
  table-layout: fixed;
  font-family: "Segoe UI", Arial, sans-serif;
  font-size: 13px;
  color: #1f2933;
}

.accounting-grid th,
.accounting-grid td {
  border-right: 1px solid #d7dde5;
  border-bottom: 1px solid #d7dde5;
  padding: 0;
  height: 34px;
  background: #fff;
}

.accounting-grid th {
  background: #eef2f6;
  color: #374151;
  font-weight: 650;
  text-align: center;
  padding: 8px 6px;
}

.accounting-grid tr:hover td {
  background: #fbfdff;
}

.accounting-grid td.row-no,
.accounting-grid th.row-no {
  width: 46px;
  text-align: center;
  color: #6b7280;
  background: #f8fafc;
  font-weight: 600;
}

.accounting-grid .cell-input {
  width: 100%;
  height: 33px;
  border: 0;
  padding: 0 8px;
  font-family: inherit;
  font-size: 13px;
  color: inherit;
  background: transparent;
  outline: none;
  box-sizing: border-box;
}

.accounting-grid .cell-input:focus {
  background: #fff8dc;
  box-shadow: inset 0 0 0 2px #2f80ed;
}

.accounting-grid .cell-number,
.accounting-grid .cell-number .cell-input {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.accounting-grid .cell-readonly {
  padding: 0 8px;
  display: flex;
  align-items: center;
  height: 33px;
  color: #6b7280;
  font-size: 12.5px;
}

.accounting-grid .cell-readonly.cell-number {
  justify-content: flex-end;
  color: #1f2933;
  font-size: 13px;
}

.accounting-grid .dxr-cell {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  height: 33px;
}

.accounting-grid .dxr-cell .cell-input {
  border-right: 1px solid #d7dde5;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.accounting-grid .dxr-cell .cell-input:last-child {
  border-right: 0;
}

.accounting-grid .cell-action {
  width: 46px;
  text-align: center;
}

.accounting-grid .cell-action button {
  width: 28px;
  height: 28px;
  border: 0;
  background: transparent;
  color: #6b7280;
  cursor: pointer;
  border-radius: 4px;
}

.accounting-grid .cell-action button:hover {
  background: #f2f7ff;
  color: #d93025;
}

/* Footer + toolbar mirror mockup */
.line-items-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: #fbfdff;
  border: 1px solid #d7dde5;
  border-top: 0;
  color: #6b7280;
  font-size: 12px;
}

.line-items-footer strong {
  color: #1f2933;
  font-size: 13px;
}

.line-items-footer .keyboard-guide {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.line-items-footer .keyboard-guide .kbd {
  min-width: 18px;
  padding: 1px 5px;
  border: 1px solid #b8c2cc;
  border-bottom-width: 2px;
  border-radius: 3px;
  background: #fff;
  color: #374151;
  font-size: 11px;
  font-family: Consolas, monospace;
  margin: 0 2px;
}

.line-items-toolbar {
  display: flex;
  gap: 8px;
  margin-top: 10px;
}

.line-items-toolbar .lib-btn {
  height: 30px;
  border: 1px solid #b8c2cc;
  border-radius: 4px;
  background: #fff;
  padding: 0 10px;
  cursor: pointer;
  font-family: "Segoe UI", Arial, sans-serif;
  font-size: 13px;
  color: #1f2933;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: border-color .15s, color .15s, background .15s;
}

.line-items-toolbar .lib-btn:hover,
.line-items-toolbar .lib-btn:focus {
  border-color: #2f80ed;
  color: #1f6fd1;
  background: #f7fbff;
  outline: none;
}

.line-items-toolbar .lib-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.line-items-toolbar .lib-btn:disabled:hover,
.line-items-toolbar .lib-btn:disabled:focus {
  border-color: #b8c2cc;
  color: #1f2933;
  background: #fff;
}

.line-items-toolbar .lib-btn-danger {
  color: #d93025;
}

.line-items-toolbar .lib-btn-danger:hover,
.line-items-toolbar .lib-btn-danger:focus {
  border-color: #d93025;
  color: #d93025;
  background: #fff5f5;
}

/* Cột "Loại" plain text muted */
.accounting-grid .cell-pricing-mode {
  padding: 0 8px;
  text-align: center;
  color: #6b7280;
  font-size: 12.5px;
}
```

2. Verify file đã tạo:
```
dir frontend\src\pages\quotations\components\line-items-grid.css
```

## Verification
- File tồn tại tại đường dẫn nêu trên.
- Mở file kiểm tra không có lỗi syntax thừa (không có `</css>` ở cuối).
- Chạy `npm run lint` (ở `frontend/`) — không có lỗi mới (file CSS không bị TS lint can thiệp).
- Chạy `npm run build` (ở `frontend/`) — build pass (file CSS chưa import ở đâu nên không ảnh hưởng).

## Exit Criteria
- File `line-items-grid.css` tồn tại với nội dung đúng spec.
- `.line-items-toolbar .lib-btn:disabled` có visual mờ và không đổi sang hover/focus active state.
- `npm run build` không có lỗi mới do phase này.
