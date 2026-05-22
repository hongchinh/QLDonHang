# Zalo OA — Gửi File Báo Giá vào Nhóm Zalo

## Goal

Thêm tính năng gửi file báo giá (Excel hoặc PDF) vào nhóm Zalo của khách hàng thông qua Zalo Official Account API. User bấm nút "Gửi Zalo" trên trang báo giá, chọn định dạng file, hệ thống tự động gửi text message + file vào nhóm Zalo đã cấu hình cho khách hàng đó. Token Zalo được quản lý trong DB với proactive refresh.

## Scope

In scope:
- Thêm `ZaloGroupId` (nullable) vào `Customer`
- Entity singleton `ZaloOaToken` lưu access/refresh token trong DB
- `IZaloTokenManager` — proactive refresh khi token còn < 24h
- `IZaloOaService` — upload file + gửi text + gửi file vào group
- `POST /quotations/{id}/send-zalo` endpoint (permission: `quotations.print`)
- `PUT /api/settings/zalo-token` endpoint (permission: `system.manage_settings`)
- Frontend: customer form field, `SendZaloDialog`, nút "Gửi Zalo" trên quotation form
- `ExternalServiceException` → HTTP 502 cho Zalo API failures

Out of scope:
- Gửi tự động khi chuyển trạng thái
- Lịch sử gửi Zalo
- Gửi vào nhiều nhóm cùng lúc
- Refresh token với long-lived refresh token management ngoài 3 tháng

## Assumptions

- Zalo OA đã được tạo; admin tự cấu hình token qua `PUT /api/settings/zalo-token` sau khi deploy
- OA được thêm thủ công vào các nhóm Zalo của khách hàng
- `ZaloGroupId` là `group_id` của Zalo Group (lấy từ Zalo Developer Portal hoặc API)
- Zalo upload API: `POST https://upload.zaloapp.com/v2/oa/upload/file`
- Zalo group message API: `POST https://openapi.zalo.me/v3.0/oa/group/sendmessage`
- Zalo token refresh API: `POST https://oauth.zaloapp.com/v4/oa/access_token`
- `expires_in` từ Zalo API là seconds (7776000 = 90 ngày)

## Risks

- Zalo API thay đổi endpoint/schema → `ZaloOaService` cần cập nhật
- Refresh token cũng hết hạn (sau ~3 tháng) → cần manual intervention
- Zalo file upload size limit chưa xác định → document trong `ZaloOaOptions`
- LibreOffice cần cài sẵn để gửi PDF (đã có từ feature export)

## Phases

- [ ] Phase 01 — Domain + Data (S) — `phase-01-domain-data.md`
- [ ] Phase 02 — Zalo Services (M) — `phase-02-zalo-services.md`
- [ ] Phase 03 — Backend API (S) — `phase-03-backend-api.md`
- [ ] Phase 04 — Frontend (M) — `phase-04-frontend.md`

## Final Verification

```bash
# Backend builds clean
dotnet build backend/OrderMgmt.sln

# All integration tests pass
dotnet test backend/tests/OrderMgmt.IntegrationTests \
  --environment TEST_DB_CONNECTION="Host=localhost;Port=5432;Database=qldonhang_integtest;Username=postgres;Password=1"

# Frontend builds clean
cd frontend && npm run build
```

## Rollback / Recovery

```bash
# Revert migration
dotnet ef database update <PreviousMigrationName> \
  --project backend/src/OrderMgmt.Infrastructure \
  --startup-project backend/src/OrderMgmt.WebApi

# Remove migration files
# backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/TIMESTAMP_AddZaloGroupIdToCustomers.cs
# backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/TIMESTAMP_AddZaloOaTokens.cs

# Revert Customer.cs, IAppDbContext.cs, AppDbContext.cs, CustomerDto.cs, CustomerService.cs
# Delete: Domain/Integrations/, Application/Integrations/, Infrastructure/Zalo/
```
