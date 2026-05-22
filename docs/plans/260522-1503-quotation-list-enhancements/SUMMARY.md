# Quotation List Enhancements

## Goal
Cải tiến màn hình danh sách báo giá (`/quotations`) gồm 3 tính năng độc lập: thêm cột Tạm ứng vào grid và footer, thay 2 ô nhập ngày bằng preset date filter giống dashboard, và thêm trạng thái "KT xác nhận" vào bộ lọc status.

## Scope
- In scope:
  - Backend: thêm `AdvancePayment` vào `QuotationListItemDto` và `QuotationListAggregates`
  - Frontend: cột Tạm ứng trong grid + tổng trong footer
  - Frontend: promote `RangePicker` thành shared `components/ui/range-picker.tsx` với empty-state + clear
  - Frontend: `STATUS_OPTIONS` và `DEFAULT_ACTIVE_STATUSES` thêm `AccountingConfirmed`
- Out of scope:
  - Cột "Còn lại" (total - advancePayment)
  - Thay đổi permission model
  - Thay đổi pagination / sorting

## Assumptions
- `Quotation` entity đã có field `AdvancePayment` — chỉ cần thêm vào DTO và projection
- `AccountingConfirmed` đã có trong `QuotationStatus` enum và backend — không cần thay đổi backend cho phần 3
- `RangePreset` type hiện export từ `use-dashboard-params.ts` — sẽ move sang `components/ui/range-picker.tsx` và update import ngược lại

## Risks
- Move `RangePicker` có thể break dashboard nếu import chưa được cập nhật đầy đủ — verify bằng TypeScript build sau phase 2
- Aggregate query backend thêm `Sum(AdvancePayment)` — cần test với dữ liệu có và không có advance payment

## Phases
- [ ] Phase 01 — Backend AdvancePayment (S) — `phase-01-backend-advance-payment.md`
- [ ] Phase 02 — RangePicker shared component (M) — `phase-02-range-picker-shared.md`
- [ ] Phase 03 — Frontend quotation list (M) — `phase-03-frontend-quotation-list.md`

## Final Verification
```bash
# Backend build
cd backend && dotnet build src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj --no-restore -c Release

# Frontend TypeScript check
cd frontend && npx tsc --noEmit
```

## Rollback / Recovery
Toàn bộ thay đổi nằm ở 7 file rõ ràng. Nếu cần rollback: `git revert` commit sau mỗi phase, hoặc `git checkout` từng file về trạng thái trước.
