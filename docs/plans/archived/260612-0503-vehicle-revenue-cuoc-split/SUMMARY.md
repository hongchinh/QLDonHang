# Vehicle Revenue — Phân loại Xe Công Ty / Xe Ngoài theo ProductCode "cuoc"

## Goal

Thay thế hoàn toàn trang Doanh thu xe hiện tại để chỉ hiển thị 2 loại doanh thu dựa trên `QuotationLine.ProductCode = "cuoc"` (case-insensitive):
- **Xe công ty**: dòng có `ProductCode = "cuoc"` và `LineTotal > 0`
- **Xe ngoài**: dòng có `ProductCode = "cuoc"` và `LineTotal < 0`

Bảng kết quả vẫn nhóm theo `TransportVehicleNumber`, tô màu từng loại. Biểu đồ chuyển từ N series (theo xe) sang 2 series cố định (công ty vs ngoài).

## Scope

**In scope:**
- `VehicleRevenueReportDtos.cs` — xóa/thêm fields theo thiết kế mới
- `VehicleRevenueReportRequestValidator.cs` — bỏ rule `TopVehicles`
- `VehicleRevenueReportService.cs` — rewrite query sang `SelectMany` trên `q.Lines`
- Integration tests `VehicleRevenueReportTests.cs` — rewrite hoàn toàn
- `frontend/src/features/reports/vehicle-revenue/types.ts` — sync với DTO mới
- `frontend/src/pages/reports/vehicle-revenue-page.tsx` — table 3 cột + chart 2 series

**Out of scope:**
- Route, permission, API endpoint URL — giữ nguyên
- Export Excel cho vehicle-revenue — không có sẵn, không thêm
- Dark mode, responsive breakpoints ngoài những gì đã có

## Assumptions

- `ProductCode` trong `QuotationLine` là string có thể null; so sánh dùng `.ToLower() == "cuoc"` (EF Core translate sang `lower()` trong PostgreSQL).
- Quotation không có dòng nào với `ProductCode = "cuoc"` thì không xuất hiện trong báo cáo.
- Một xe có thể có cả doanh thu công ty lẫn xe ngoài nếu có cả dòng dương và âm.
- `UnitPrice` âm cho phép sau commit "cho phép nhập âm"; test dùng `UnitPrice = -5000, Quantity = 1`.
- `SelectMany` trên navigation property `q.Lines` trong EF Core 8 tạo INNER JOIN, không cần `.Include()`.

## Risks

- **EF Core translation**: `q.TransportVehicleNumber.Trim()` và `.ToLower()` bên trong `SelectMany` phải được EF Core 8/Npgsql dịch sang SQL. Đây là pattern đã có trong service cũ nên rủi ro thấp.
- **Compile break ở phase 1**: integration tests sẽ không compile ngay sau khi đổi DTO. Phase 1 fix cả hai cùng lúc.

## Phases

- [x] Phase 01 — Backend DTO + Validator + Tests rewrite (M) — `phase-01-backend-dto-tests.md`
- [x] Phase 02 — Backend Service rewrite (M) — `phase-02-backend-service.md`
- [x] Phase 03 — Frontend types + page (S) — `phase-03-frontend.md`

## Final Verification

```bash
# Backend
dotnet build backend/src/OrderMgmt.Application
dotnet build backend/src/OrderMgmt.WebApi
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~VehicleRevenueReportTests"

# Frontend
cd frontend && npm run type-check
```

## Rollback / Recovery

```bash
git revert HEAD~<n>   # hoặc
git checkout main -- backend/src/OrderMgmt.Application/Reports/VehicleRevenue/
git checkout main -- backend/tests/OrderMgmt.IntegrationTests/Reports/VehicleRevenueReportTests.cs
git checkout main -- frontend/src/features/reports/vehicle-revenue/types.ts
git checkout main -- frontend/src/pages/reports/vehicle-revenue-page.tsx
```
