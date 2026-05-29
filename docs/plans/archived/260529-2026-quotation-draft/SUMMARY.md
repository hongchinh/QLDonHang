# Auto-save nháp báo giá mới

## Goal

Khi user đang nhập form tạo mới báo giá (`/quotations/new`) nhưng điều hướng sang chức năng khác trước khi nhấn Save, dữ liệu vừa nhập được tự động lưu vào `localStorage`. Lần sau khi user mở lại form tạo mới, dữ liệu được tự động điền lại, kèm badge "Nháp chưa lưu từ HH:mm" và nút "Xóa nháp".

## Scope

- In scope:
  - Route `/quotations/new` — chỉ form tạo mới
  - Lưu tất cả các field của `QuotationFormValues` + thông tin hiển thị customer autocomplete
  - Ghi debounce 1500ms sau lần thay đổi cuối
  - Phục hồi tự động khi mở lại (không hỏi xác nhận)
  - Xóa draft sau khi save thành công (mọi intent: save-stay, save-exit, save-print)
  - Nút "Xóa nháp" reset form về trạng thái trống
- Out of scope:
  - Route `/quotations/:id` (edit mode) — không có draft
  - Đồng bộ draft giữa nhiều thiết bị
  - Server-side draft
  - Cảnh báo khi điều hướng đi (unsaved changes prompt)

## Assumptions

- User đã authenticated khi render `QuotationFormPage`, tức là `useAuthStore.getState().user?.id` luôn có giá trị.
- `localStorage` luôn khả dụng trong môi trường trình duyệt (app đã có các tính năng khác dùng localStorage qua Zustand persist).
- Draft key: `quotation_draft_{userId}` — ngăn draft của user này hiển thị cho user khác trên cùng máy.
- Không cần validate schema của draft đã lưu — nếu parse thất bại hoặc thiếu `values`/`savedAt`, discard silently.

## Risks

- `form.watch()` subscription phải được unsubscribe đúng cách để tránh memory leak.
- Debounce timer phải bị cancel khi component unmount để tránh ghi vào localStorage sau unmount.
- `form.reset()` trong hook thay đổi `defaultValues` baseline của RHF, ảnh hưởng `isDirty`. Đây là behavior mong muốn: draft là điểm xuất phát mới.

## Phases

- [ ] Phase 01 — Core hook (S) — `phase-01-core-hook.md`
- [ ] Phase 02 — Form integration (M) — `phase-02-form-integration.md`

## Final Verification

```bash
cd frontend
npx vitest run src/features/quotations/
```

Sau đó kiểm tra thủ công:
1. Mở `/quotations/new`, nhập tên khách hàng + vài dòng hàng
2. Điều hướng sang `/quotations` (danh sách)
3. Click "Thêm báo giá" → form mở với dữ liệu cũ, badge "Nháp chưa lưu từ HH:mm" hiển thị
4. Click "Xóa nháp" → form reset trống, badge biến mất
5. Nhập lại, nhấn "Lưu và thoát" → chuyển về danh sách → mở lại "Thêm báo giá" → form trống (draft đã xóa)

## Rollback / Recovery

Nếu cần rollback:
- Xóa file `frontend/src/features/quotations/use-quotation-draft.ts`
- Xóa file `frontend/src/features/quotations/use-quotation-draft.test.ts`
- Revert `frontend/src/pages/quotations/quotation-form-page.tsx` về commit trước
