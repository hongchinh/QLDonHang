# In Biên Bản Bàn Giao Kiêm Phiếu Xuất Kho

## Goal

Thêm 2 loại xuất chứng từ mới từ phiếu báo giá: "Biên bản bàn giao kiêm phiếu xuất kho (có tiền)" và "Biên bản bàn giao kiêm phiếu xuất kho (không tiền)". Mỗi loại có thể xuất Excel hoặc PDF, hỗ trợ template riêng theo từng sale (per-user), fallback về template hệ thống nếu chưa cài đặt. UI chuyển button "Excel" và "In" trong form báo giá thành dropdown 3 option.

## Scope

**In scope:**
- 6 column mới trong `UserQuotationSettings` cho 2 template slot biên bản
- Interface + implementation mở rộng `IQuotationExportPathResolver` cho handover types
- `HandoverExcelRenderer` mới (tách khỏi `QuotationExcelRenderer`)
- Settings service + API mở rộng với `QuotationTemplateType` enum
- 4 endpoint mới trong `QuotationsController`
- Frontend: `meSettingsApi`, `quotationsApi`, hooks, settings page, form dropdowns

**Out of scope:**
- Lưu biên bản vào DB như chứng từ riêng
- Popup nhập liệu khi in
- Số biên bản / ký hiệu riêng
- Thay đổi logic render của báo giá hiện tại

## Assumptions

- Template `BIENBANBANGIAOKIEMPHIEUXUAT.xlsx` (có tiền) và `BIENBANBANGIAO.xlsx` (không tiền) đã có sẵn trong `templates/` và đúng format cần render.
- Cell layout của biên bản templates khác báo giá — implementer cần mở file để xác định địa chỉ ô trước khi viết renderer.
- Dữ liệu cần thiết (ngày giao, tạm ứng, sản phẩm) đã có trong `QuotationDto`.
- `WithPrice=false`: bỏ điền cột đơn giá và thành tiền vào Excel (các ô để trống), không xoá cột.

## Risks

- Cell layout trong biên bản templates không theo cấu trúc "sample rows" như báo giá → `HandoverExcelRenderer` cần constants riêng, phải xác nhận qua mở file thực tế.
- Nếu templates chưa có "sample rows" pattern, logic insert/delete rows sẽ khác; plan tính đến việc này trong phase 03.

## Phases

- [ ] Phase 01 — DB + Domain (S) — `phase-01-db-domain.md`
- [ ] Phase 02 — Config + Template Resolver (S) — `phase-02-template-resolver.md`
- [ ] Phase 03 — Handover Excel Renderer (M) — `phase-03-handover-renderer.md`
- [ ] Phase 04 — Settings Service + DTO (M) — `phase-04-settings-service.md`
- [ ] Phase 05 — Settings API (S) — `phase-05-settings-api.md`
- [ ] Phase 06 — Quotation Service + Controller (S) — `phase-06-quotation-service-api.md`
- [ ] Phase 07 — Frontend API + Types + Hooks (S) — `phase-07-frontend-api-types.md`
- [ ] Phase 08 — Frontend Settings Page (S) — `phase-08-frontend-settings-page.md`
- [ ] Phase 09 — Frontend Form Dropdowns (S) — `phase-09-frontend-form-dropdowns.md`

## Final Verification

```bash
# Backend build
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj

# Integration tests (requires TEST_DB_CONNECTION env var pointing to a test DB)
dotnet test backend/tests/OrderMgmt.IntegrationTests/ \
  --filter "HandoverExport" \
  -e TEST_DB_CONNECTION="<test-db-connection-string>"

# Frontend type check
cd frontend && npx tsc --noEmit
```

## Rollback / Recovery

- Backend: revert EF migration với `dotnet ef database update <prev-migration-name>`, xoá migration file và Designer.cs, revert entity + code changes.
- Frontend: git revert các file thay đổi — không có DB side effect từ frontend changes.
- Templates trong `templates/` không bị thay đổi bởi plan này.
