# Xuất Excel — Chi tiết doanh thu

## Goal

Kết nối button "Xuất Excel" trên trang `revenue-page.tsx` để download file `.xlsx` chứa toàn bộ dòng hàng trong bảng **Chi tiết doanh thu** với cùng bộ lọc (from/to/saleUserId). Backend tạo file qua ClosedXML, nhất quán với pattern xuất báo giá đã có.

## Scope

- **In scope:**
  - Interface `IRevenueReportExcelRenderer` + implementation `RevenueReportExcelRenderer` (ClosedXML, không dùng template)
  - Endpoint `GET /api/reports/revenue-lines/excel` trong `ReportsController`
  - DI registration trong `Infrastructure/DependencyInjection.cs`
  - Frontend: hàm `downloadRevenueExcel()` trong `api.ts`, loading state + wiring trong `revenue-page.tsx`
  - Integration test: endpoint trả đúng content-type, bytes parse được thành XLWorkbook, header row đúng, dòng dữ liệu khớp

- **Out of scope:**
  - Xuất PDF
  - Export cho bảng "Bảng doanh thu theo ngày" hay "Top khách hàng"
  - Frontend unit/component tests (không có test framework frontend)

## Assumptions

- Backend trả **đầy đủ** cost columns (unitCost/lineCost/lineProfit) theo quyền `quotations.view_cost` đã được xử lý bởi `ISalesRevenueReportService.GetLineItemsAsync` — renderer không cần thêm logic authorization.
- Không dùng template Excel file (khác `QuotationExcelRenderer`): workbook được tạo từ đầu bằng ClosedXML.
- Validator `SalesRevenueLineItemsRequestValidator` đã có, tái dùng cho endpoint mới.
- Column **Mã BG** được thêm vào Excel (cột 2) dù không có trong UI table — cần thiết cho cross-referencing.
- Khoảng ngày empty → file Excel chỉ có header row (không lỗi).

## Risks

- `SalesRevenueLineItemsRequest.From/To` kiểu `DateTime` nhưng frontend gửi string `"YYYY-MM-DD"` — model binding đã xử lý qua `DateTime.Parse`, không cần thay đổi.
- Tên file chứa ký tự đặc biệt nếu có saleUserId: không include saleName vào tên file để tránh vấn đề encoding.

## Phases

- [x] Phase 01 — Backend renderer + endpoint (M) — `phase-01-backend.md`
- [x] Phase 02 — Frontend API + page wiring (S) — `phase-02-frontend.md`

## Final Verification

```
# Backend integration tests
cd backend && dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~RevenueLineItemsExportTests" \
  -- TestRunParameters.Parameter(name=\"TEST_DB_CONNECTION\", value=\"<test-conn-str>\")

# Frontend build
cd frontend && npm run build
```

Manual verification:
1. Chọn khoảng ngày có dữ liệu → click "Xuất Excel" → file download tên `BaoCaoDoanhThu_YYYYMMDD_YYYYMMDD.xlsx`
2. Mở file: header row đúng 18 cột, dữ liệu khớp bảng UI, footer row tổng đúng
3. Trường hợp không có dữ liệu: file download chỉ có header row (không báo lỗi)
4. Bấm khi `from` hoặc `to` chưa chọn: button disabled

## Rollback / Recovery

- Chỉ thêm mới (interface, renderer, endpoint, test) — không sửa endpoint hoặc service hiện tại
- Xóa 4 file mới tạo + revert DI + revert ReportsController + revert api.ts + revert revenue-page.tsx là rollback hoàn toàn
