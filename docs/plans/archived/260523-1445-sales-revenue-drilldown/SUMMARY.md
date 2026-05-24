# Drill-Down Báo cáo doanh thu theo Sale

## Goal

Thêm tính năng drill-down từ bảng tổng hợp báo cáo doanh thu theo sale (`/reports/sales-revenue`) xuống bảng chi tiết dòng hàng hóa của một sale (`/reports/sales-revenue/:saleUserId`), sau đó click dòng hàng điều hướng sang trang chi tiết báo giá (`/quotations/:id`). Bảng chi tiết hiển thị tất cả `QuotationLine` từ các báo giá đã xác nhận của sale trong khoảng ngày lọc, kèm thông tin nhóm cấp báo giá.

## Scope

**In scope:**
- Endpoint mới `GET /reports/sales-revenue/{saleUserId}/lines?from=&to=` với `reports.revenue` permission
- DTO `SalesRevenueLineItemDto` chứa đủ các cột yêu cầu; trường cost/profit null khi thiếu `quotations.view_cost`
- Trang React mới `SalesRevenueDetailPage` tại route `/reports/sales-revenue/:saleUserId`
- Rows trong trang tổng hợp trở thành clickable, điều hướng sang trang chi tiết
- Click dòng hàng trong trang chi tiết điều hướng sang `/quotations/:quotationId`
- Cột vận chuyển hiển thị giá trị từ API; frontend chỉ render ô khi `isFirstLineOfQuotation === true`
- Cột chi phí/lợi nhuận ẩn toàn bộ khi API trả về `null` (kiểm tra `items.some(i => i.unitCost !== null)`)

**Out of scope:**
- Export Excel/PDF cho trang chi tiết
- Phân trang (tải toàn bộ)
- Bộ lọc bổ sung trong trang chi tiết
- Dòng tổng kết trong bảng chi tiết

## Assumptions

- Sắp xếp: `ConfirmedAt` giảm dần → `SortOrder` tăng dần trong từng báo giá
- Backend tính `isFirstLineOfQuotation` — frontend không cần tự suy luận
- Trường `contactPhone` lấy từ `Quotation.ContactPhone` (không phải `DeliveryPhone`)
- `SalesRevenueReportService` cần thêm `ICurrentUser` vào constructor để kiểm tra quyền cost
- Validator cho request mới dùng lại cùng rule (from ≤ to, tối đa 366 ngày)

## Risks

- Inject `ICurrentUser` vào `SalesRevenueReportService` thay đổi constructor — cần đảm bảo DI đã đăng ký `ICurrentUser` (đã có trong `QuotationService`, pattern đã được kiểm chứng)
- Báo giá không có dòng nào (empty `Lines`) sẽ không xuất hiện trong kết quả — đây là behavior mong muốn

## Phases

- [ ] Phase 01 — Backend API (M) — `phase-01-backend-api.md`
- [ ] Phase 02 — Frontend (M) — `phase-02-frontend.md`

## Final Verification

```bash
# Backend integration tests
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "SalesRevenueLineItems" --logger "console;verbosity=normal"

# Frontend type check
cd frontend && npx tsc --noEmit

# Manual smoke test
# 1. Mở /reports/sales-revenue, click một dòng sale
# 2. Xác nhận điều hướng sang /reports/sales-revenue/:saleUserId với từ/đến ngày đúng
# 3. Xác nhận bảng chi tiết hiển thị đủ cột, isFirstLineOfQuotation hoạt động đúng
# 4. Click một dòng hàng → điều hướng sang /quotations/:id đúng
```

## Rollback / Recovery

Các thay đổi không phá vỡ backward compatibility:
- Backend: chỉ thêm endpoint mới, không thay đổi endpoint cũ
- Frontend: chỉ thêm route mới và click handler trên table rows; table vẫn render nếu navigate không được gọi
- Revert: `git revert` các commit của plan này
