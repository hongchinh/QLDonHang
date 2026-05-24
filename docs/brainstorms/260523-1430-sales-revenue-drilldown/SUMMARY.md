# Drill-Down Báo cáo doanh thu theo Sale

**Ngày:** 2026-05-23  
**Trạng thái:** Thiết kế đã duyệt

---

## Problem Framing

Trang báo cáo doanh thu theo sale (`/reports/sales-revenue`) hiện chỉ hiển thị bảng tổng hợp một dòng = một sale. Người dùng cần khả năng **drill down** từ dòng sale xuống bảng chi tiết dòng hàng hóa, và từ đó điều hướng sang trang chi tiết báo giá.

---

## Goals & Non-Goals

**Goals:**
- Click dòng sale trong bảng tổng hợp → điều hướng sang trang chi tiết của sale đó
- Trang chi tiết hiển thị danh sách dòng hàng hóa (QuotationLine) từ tất cả báo giá đã xác nhận của sale trong khoảng ngày lọc
- Click dòng hàng hóa → điều hướng sang trang chi tiết báo giá (`/quotations/:id`)
- Bảo toàn bộ lọc ngày (from/to) khi điều hướng
- Ẩn cột chi phí/lợi nhuận nếu user không có quyền `quotations.view_cost`

**Non-Goals:**
- Thêm bộ lọc riêng trong trang chi tiết (dùng lại filter từ trang tổng hợp)
- Export Excel/PDF cho trang chi tiết (chưa yêu cầu)
- Phân trang (trang chi tiết tải toàn bộ)

---

## Constraints & Assumptions

- Chỉ hiển thị báo giá đã xác nhận, chưa huỷ (giống logic hiện tại của summary)
- Trường chi phí (`unitCost`, `lineCost`, `lineProfit`) bị ẩn nếu user thiếu quyền `quotations.view_cost` — backend trả về `null`, frontend ẩn toàn bộ cột khi tất cả giá trị đều `null`
- Phí vận chuyển (`freight`) là cấp báo giá — chỉ hiển thị ở dòng đầu tiên của mỗi nhóm báo giá, các dòng sau để trống
- Project scope: chỉ báo giá, không có Order/Delivery/Payment

---

## Approaches Considered

| Hướng | Mô tả | Ưu | Nhược |
|---|---|---|---|
| **1 — Bảng phẳng dòng hàng** ✅ | 1 row = 1 QuotationLine, nhóm theo báo giá, click → navigate báo giá | Hiện đủ tất cả cột yêu cầu | Nhiều cột, cần scroll ngang |
| 2 — Bảng tóm tắt báo giá | 1 row = 1 Quotation, click → trang chi tiết báo giá | Đơn giản | Không thấy hàng hóa/lợi nhuận ở bước drill-down |
| 3 — Collapsible groups | Dòng báo giá có expand → dòng hàng hóa bên dưới | Gọn theo mặc định | Phức tạp hơn, khó tổng hợp |

**Chọn Hướng 1** — đáp ứng đúng các cột đã yêu cầu bao gồm hàng hóa, kích thước, lợi nhuận.

---

## Recommended Approach

### 1. URL & Navigation

```
/reports/sales-revenue                          → bảng tổng hợp (đã có)
/reports/sales-revenue/:saleUserId?from=&to=    → bảng chi tiết (mới)
/quotations/:quotationId                        → chi tiết báo giá (đã có)
```

- Trang chi tiết có nút "← Quay lại" dùng `useNavigate(-1)`
- `saleUserId` từ route param; `from`/`to` từ query string (giữ nguyên filter)

### 2. Backend

**Endpoint mới:**
```
GET /reports/sales-revenue/{saleUserId}/lines?from=YYYY-MM-DD&to=YYYY-MM-DD
Authorization: reports.revenue permission
```

**DTO: `SalesRevenueLineItemDto`**

| Field | Nguồn | Ghi chú |
|---|---|---|
| `quotationId` | Quotation.Id | Navigation key |
| `quotationCode` | Quotation.Code | Số báo giá |
| `quotationDate` | Quotation.QuotationDate | Ngày báo giá |
| `confirmedAt` | Quotation.ConfirmedAt | Ngày xác nhận |
| `customerName` | Quotation.CustomerName | |
| `customerAddress` | Quotation.CustomerAddress | |
| `contactPhone` | Quotation.ContactPhone | |
| `freight` | Quotation.Freight | Chỉ hiện dòng đầu (xử lý FE) |
| `isFirstLineOfQuotation` | computed | Backend đánh dấu |
| `productName` | QuotationLine.ProductName | Hàng hóa |
| `specification` | QuotationLine.Specification | Kích thước |
| `quantity` | QuotationLine.Quantity | |
| `unitPrice` | QuotationLine.UnitPrice | Đơn giá bán |
| `lineTotal` | QuotationLine.LineTotal | Số tiền |
| `unitCost` | QuotationLine.UnitCost | `null` nếu thiếu quyền |
| `lineCost` | QuotationLine.LineCost | `null` nếu thiếu quyền |
| `lineProfit` | QuotationLine.LineProfit | `null` nếu thiếu quyền |

**Sắp xếp:** `ConfirmedAt` giảm dần → `SortOrder` tăng dần

**Service:** Thêm method `GetSaleLineItemsAsync(saleUserId, from, to, canViewCost)` vào `SalesRevenueReportService`. Query confirmed, non-cancelled quotations theo `OwnerUserId` + date range, include `Lines`.

### 3. Frontend

**Thay đổi trang tổng hợp** (`sales-revenue-page.tsx`):
- Tbody rows thêm `cursor-pointer` + `onClick` → navigate sang trang chi tiết với query params
- Row tổng (footer) không clickable

**Trang chi tiết mới** (`sales-revenue-detail-page.tsx`):
- Header: tên sale, khoảng ngày, nút Quay lại
- Bảng phẳng với đầy đủ cột
- Cột cấp báo giá (số BG, ngày, khách hàng, địa chỉ, ĐT, vận chuyển): chỉ render nếu `isFirstLineOfQuotation === true`
- Cột chi phí/lợi nhuận: ẩn toàn bộ nếu tất cả rows đều có `null` ở các cột đó
- Mỗi row: `cursor-pointer` + `onClick` → navigate `/quotations/:quotationId`

**File mới:**
```
frontend/src/features/reports/sales-revenue-detail/
  api.ts      — salesRevenueDetailApi.getLines(saleUserId, params)
  hooks.ts    — useSalesRevenueDetail(saleUserId, params)
  types.ts    — SalesRevenueLineItemDto, SalesRevenueDetailParams
  keys.ts     — query key factory

frontend/src/pages/reports/
  sales-revenue-detail-page.tsx

backend/.../Reports/SalesRevenue/Models/SalesRevenueReportDtos.cs
  + SalesRevenueLineItemDto
  + SalesRevenueLineItemsRequest
```

**Route mới:** `/reports/sales-revenue/:saleUserId` → `SalesRevenueDetailPage`

---

## Open Questions

- Trang chi tiết có cần dòng **tổng kết** (sum row) ở cuối bảng không? (tổng lineTotal, lineCost, lineProfit của toàn bộ các dòng)
- Scroll ngang trên mobile/tablet có cần xử lý đặc biệt không?

---

## Next Steps

1. Viết implementation plan (write-plan)
2. Implement backend endpoint + DTO + service method
3. Implement frontend feature files + trang chi tiết
4. Thêm route mới
5. Thêm click handler vào trang tổng hợp
