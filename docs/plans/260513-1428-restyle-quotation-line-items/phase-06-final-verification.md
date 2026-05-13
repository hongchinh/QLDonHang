# Phase 06 — Final verification

**Status:** [-] in-progress
**Complexity:** S

## Objective
Verify tổng thể: build, lint, type-check pass; UI render đúng visual mockup; keyboard + "Xóa tất cả" + height stretch hoạt động đúng.

## Files
(Không sửa file production — chỉ chạy lệnh và test UI)

## Tasks

1. **Static checks** ở `frontend/`:
   ```
   npm run lint
   npm run test -- line-items-grid
   npm run build
   ```
   Cả ba phải pass. Nếu lint warning về `react-hooks/exhaustive-deps` ở handler keyboard → confirm đã `eslint-disable-next-line` đúng chỗ.

2. **Manual UI test** — chạy dev server:
   ```
   npm run dev
   ```
   Mở browser tới đường dẫn dev (thường `http://localhost:5173`). Đăng nhập nếu cần. Vào `Báo giá` → `Thêm mới` (`/quotations/new`).

3. **Test case A — Visual diff vs mockup**:
   - Header bảng nền xám `#eef2f6`, chữ `#374151`, font-weight ~650, text-align center.
   - Cell input borderless, font 13px Segoe UI; focus → nền vàng `#fff8dc` + viền xanh 2px inset.
   - Cột STT nền `#f8fafc`, chữ muted, centered.
   - Footer dưới bảng có kbd boxes bên trái + "Tổng: <số>" bên phải.
   - So sánh side-by-side với `docs/bd/ui_form_them_moi_phieu_thu_html.html` mở trong tab khác.

4. **Test case B — Tổng cộng = Thông tin chung height**:
   - Trên viewport ≥1024px, kéo screenshot 2 card top row. DevTools → kiểm tra `getBoundingClientRect().height` của 2 card bằng nhau.
   - Nhóm "Tổng giá vốn / Lợi nhuận gộp" neo ở đáy card "Tổng cộng".

5. **Test case C — Insert/Delete keyboard**:
   - Focus 1 input trong bảng, bấm `Insert` → thêm dòng mới ở cuối.
   - Focus input ở dòng 2, bấm `Ctrl+Delete` → dòng 2 bị xóa, bảng còn n-1 dòng.
   - Focus input bất kỳ, gõ vài ký tự, bấm `Delete` trần → chỉ xóa ký tự (không xóa dòng).
   - Click ngoài bảng, bấm `Insert` → KHÔNG thêm dòng (listener scope đúng).

6. **Test case D — Product typeahead dropdown**:
   - Focus/gõ vào ô "Mã hàng" trong bảng.
   - Suggestion dropdown mở đầy đủ phía trên bảng, không bị clip bởi `.accounting-grid-wrap` dù bảng có `overflow-x: auto`.
   - Scroll ngang bảng khi dropdown đang mở → dropdown vẫn bám đúng input hoặc tự cập nhật vị trí, không nằm sai lệch rõ rệt.
   - Chọn 1 sản phẩm → các field mã/tên/ĐVT/loại/giá vẫn auto-fill như trước.

7. **Test case E — "Xóa tất cả dòng"**:
   - Bảng có ≥1 dòng → bấm "Xóa tất cả dòng" → `ConfirmDialog` hiện với title `"Xóa tất cả dòng?"` và message `"Xóa toàn bộ N dòng?"`.
   - Bấm "Xóa" → bảng trống, hiển thị placeholder row "Chưa có dòng nào...".
   - Bảng trống → nút disabled (visual mờ).

8. **Test case F — Submit form vẫn lưu được**:
   - Tạo báo giá mới với ≥1 dòng đầy đủ thông tin → bấm "Tạo mới" → toast success → redirect tới trang detail.
   - Mở quotation vừa tạo ở chế độ edit → bảng render lại đúng các dòng đã lưu, layout không vỡ.

9. **Test case G — Responsive**:
   - Resize browser xuống <1024px (mobile/tablet) → top grid về 1 cột; Chi tiết hàng hóa scroll ngang nếu cần (do `min-width: 1180px` trên `.accounting-grid` + `overflow-x: auto` trên wrap).
   - Resize lên ≥1024px → quay lại 2 cột.

10. **Cleanup**:
   - Đóng dev server (Ctrl+C).
   - Confirm không còn console error nào liên quan thay đổi (React warning, missing key, etc.).

## Verification
- Tất cả test case A–G pass.
- `npm run lint` clean.
- `npm run test -- line-items-grid` pass.
- `npm run build` succeed.
- Không có console error / warning mới.

## Exit Criteria
- Visual match mockup (header, cell focus, font, kbd, footer).
- Card "Tổng cộng" cao bằng "Thông tin chung" ở viewport desktop.
- Keyboard Insert/Ctrl+Delete hoạt động đúng; Delete trần không xóa dòng.
- Product typeahead dropdown không bị clip trong bảng scroll ngang.
- Nút "Xóa tất cả dòng" có `ConfirmDialog` + disabled khi rỗng.
- Form submit/load không bị regression.
- Build/lint/test sạch.
