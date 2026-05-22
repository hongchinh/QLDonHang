# In Biên Bản Bàn Giao Kiêm Phiếu Xuất Kho

**Date:** 2026-05-23  
**Status:** Design approved, ready for implementation

---

## Problem Framing

Phiếu báo giá hiện chỉ in được báo giá (có tiền). Cần bổ sung 2 loại chứng từ in thêm từ cùng một phiếu báo giá:
- Biên bản bàn giao kiêm phiếu xuất kho **có tiền** (hàng hóa, DVT, số lượng, đơn giá, thành tiền)
- Biên bản bàn giao kiêm phiếu xuất kho **không tiền** (hàng hóa, DVT, số lượng)

Mỗi loại có mẫu riêng, hỗ trợ cài đặt template theo từng sale (per-user), fallback về mẫu hệ thống nếu không có cài đặt.

---

## Goals & Non-Goals

**Goals:**
- Thêm 2 loại xuất file (Excel + PDF) cho biên bản bàn giao
- UI: button "In" và "Excel" thành dropdown 3 option
- Per-user template upload/download/delete cho 2 loại biên bản mới
- Dùng lại dữ liệu sẵn có trong `QuotationDto` (không thêm field mới)

**Non-Goals:**
- Lưu biên bản vào DB như một chứng từ riêng
- Popup nhập thêm dữ liệu khi in
- Số biên bản / ký hiệu chứng từ riêng

---

## Constraints & Assumptions

- Ngày bàn giao = `DeliveryDate` sẵn có trong phiếu báo giá
- Số tiền tạm ứng = `AdvancePayment` sẵn có trong phiếu báo giá
- Template files hệ thống đã có: `BIENBANBANGIAOKIEMPHIEUXUAT.xlsx` (có tiền), `BIENBANBANGIAO.xlsx` (không tiền)
- Cơ chế per-user template (upload/resolve/fallback) đã hoàn chỉnh, chỉ cần mở rộng

---

## Approaches Considered

### Option A — Query parameter `?withPrice=true` (rejected)
`GET /quotations/{id}/handover/excel?withPrice=true`

**Pros:** Ít endpoint hơn  
**Cons:** Logic phân nhánh ở backend phức tạp hơn, khó cache riêng

### Option B — Endpoint riêng biệt (chosen)
4 endpoint riêng: `/handover-with-price/excel`, `/handover-with-price/pdf`, `/handover-no-price/excel`, `/handover-no-price/pdf`

**Pros:** Rõ ràng, dễ phân quyền, theo đúng pattern hiện tại (`/excel`, `/pdf`)  
**Cons:** Nhiều endpoint hơn

---

## Recommended Approach

### UI — `quotation-form-page.tsx`

Button "Excel" và "In" chuyển thành `DropdownMenu` (Radix/shadcn) với 3 option:
1. Báo giá *(hành vi hiện tại)*
2. Biên bản bàn giao (có tiền)
3. Biên bản bàn giao (không tiền)

### Backend API — 4 endpoint mới

```
GET /quotations/{id}/handover-with-price/excel   → BienBanBanGiao_{code}.xlsx
GET /quotations/{id}/handover-with-price/pdf     → BienBanBanGiao_{code}.pdf
GET /quotations/{id}/handover-no-price/excel     → BienBanBanGiao_{code}.xlsx
GET /quotations/{id}/handover-no-price/pdf       → BienBanBanGiao_{code}.pdf
```

Permission: `Permissions.Quotations.Print` (giống các endpoint hiện tại)

### Template Resolution — mở rộng `QuotationExportPathResolver`

Thêm method `ResolveHandoverTemplatePathAsync(userId, withPrice, ct)`:
- User template: `templates/users/{userId}_handover_with_price.xlsx` hoặc `_no_price.xlsx`
- System fallback: `BIENBANBANGIAOKIEMPHIEUXUAT.xlsx` hoặc `BIENBANBANGIAO.xlsx`

### DB — mở rộng `UserQuotationSettings`

Thêm 6 column mới (nhất quán với `TemplateUploadedAt` hiện có của slot báo giá):
```
HandoverWithPriceTemplateFileName
HandoverWithPriceTemplateOriginalName
HandoverWithPriceTemplateUploadedAt
HandoverNoPriceTemplateFileName
HandoverNoPriceTemplateOriginalName
HandoverNoPriceTemplateUploadedAt
```

### Settings API — mở rộng với `templateType`

```
PUT    /me/quotation-settings/template?type=quotation|handover-with-price|handover-no-price
DELETE /me/quotation-settings/template?type=quotation|handover-with-price|handover-no-price
GET    /me/quotation-settings/template?type=quotation|handover-with-price|handover-no-price
```

### Excel Renderer — `HandoverExcelRenderer`

Class mới tách khỏi `QuotationExcelRenderer`:
- Input: `QuotationDto` + `bool withPrice`
- Điền: khách hàng, sản phẩm, DVT, số lượng, `DeliveryDate`, `AdvancePayment`
- `withPrice = false`: bỏ qua cột đơn giá / thành tiền
- PDF: dùng lại `_pdfConverter.ConvertAsync()` như báo giá

### Frontend Settings — `my-quotation-settings-page.tsx`

Thêm 2 section bên dưới section "Báo giá" hiện có:
- **Biên bản bàn giao (có tiền)** — upload/download/delete
- **Biên bản bàn giao (không tiền)** — upload/download/delete

---

## Open Questions

- Tên file xuất có nên theo mã phiếu không (`BienBanBanGiao_{code}`)? *(assumed yes)*
- Template `BIENBANBANGIAO.xlsx` và `BIENBANBANGIAOKIEMPHIEUXUAT.xlsx` đã có đúng format cần render chưa? *(cần xem trước khi implement renderer)*

---

## Next Steps

1. Mở rộng `UserQuotationSettings` entity + migration
2. Mở rộng Settings API (`MeQuotationSettingsController`)
3. Tạo `HandoverExcelRenderer`
4. Mở rộng `QuotationExportPathResolver`
5. Thêm 4 endpoint vào `QuotationsController`
6. Mở rộng `QuotationService` với 4 method mới
7. Frontend: chuyển button thành dropdown
8. Frontend: mở rộng settings page
