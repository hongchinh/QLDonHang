# Quotation List Enhancements

## Goal

Thêm 3 cải tiến vào màn hình danh sách báo giá: (1) cột Tạm ứng và aggregate tương ứng, (2) thay 2 input ngày bằng preset buttons (7N / 30N / Tháng này / Tháng trước / Tuỳ chỉnh + "Tất cả" để clear), (3) thêm trạng thái "KT xác nhận" vào bộ lọc và mặc định hiển thị.

## Scope

In scope:
- Backend: thêm `AdvancePayment` vào `QuotationListItemDto`, `QuotationListAggregates`, và projection trong `ListAsync`
- Frontend types: thêm `advancePayment` vào `QuotationListItem` và `QuotationListAggregates`
- Frontend grid: cột "Tạm ứng" hiển thị cho tất cả users (không cần permission)
- Frontend footer: aggregate "Tạm ứng" trong `ListFooter`
- Frontend date filter: extract shared preset logic ra `src/lib/date-range-presets.ts`, tạo `QuotationDateFilter` component, cập nhật `RangePicker` dashboard để dùng lib chung
- Frontend status filter: thêm `AccountingConfirmed` vào `STATUS_OPTIONS` và `DEFAULT_ACTIVE_STATUSES`
- Integration test: thêm test verify `AdvancePayment` trong list item và aggregate

Out of scope:
- Thay đổi logic tính `advancePayment` (đã có sẵn ở entity)
- Sửa form nhập báo giá
- Thêm preset "Hôm nay" (không được yêu cầu)
- Sắp xếp cột / sorting mới

## Assumptions

- `Quotation.AdvancePayment` tồn tại ở domain entity (đã xác nhận)
- Backend `QuotationStatusListParser` đã handle `AccountingConfirmed` qua `Enum.TryParse` — không cần sửa
- `RangePicker` dashboard giữ nguyên behavior sau khi extract (chỉ đổi import source)
- Mặc định khi vào trang quotation list: không lọc ngày (`from=''`, `to=''`)
- `AdvancePayment` hiển thị cho tất cả users, không cần permission `view_cost`

## Risks

- Thay `DEFAULT_ACTIVE_STATUSES` sẽ thay đổi danh sách mặc định hiển thị khi user mở trang — cần kiểm tra không làm hỏng các URL bookmark cũ (không ảnh hưởng vì status được persist trên URL param)
- Extract `computePreset` / `matchActivePreset` ra lib chung cần đảm bảo `RangePicker` dashboard vẫn chạy đúng sau khi refactor import

## Phases

- [ ] Phase 01 — Backend AdvancePayment (S) — `phase-01-backend-advance-payment.md`
- [ ] Phase 02 — Frontend Types & Grid Column (S) — `phase-02-frontend-types-column.md`
- [ ] Phase 03 — Date Preset Filter Component (M) — `phase-03-date-preset-filter.md`
- [ ] Phase 04 — Status KT xác nhận & Integration Test (S) — `phase-04-status-kt-and-test.md`

## Final Verification

```bash
# Backend build
cd backend && dotnet build --no-restore -c Release

# Frontend type-check
cd frontend && npx tsc --noEmit

# Integration tests (dùng DB test riêng biệt)
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~QuotationListFilterTests" \
  --no-build -v n
```

## Rollback / Recovery

Tất cả thay đổi là additive (thêm field, thêm component). Để rollback:
- Backend: xóa property `AdvancePayment` vừa thêm vào `QuotationListItemDto` và `QuotationListAggregates`, remove khỏi projection
- Frontend: revert `types.ts`, xóa `quotation-date-filter.tsx`, restore 2 `<Input type="date">` trong list page, restore local definitions trong `range-picker.tsx`
- Không có migration database (chỉ đọc field đã có)
