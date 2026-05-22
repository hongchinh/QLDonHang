# Thêm status AccountingConfirmed vào Quotation module

## Goal

Bổ sung trạng thái `AccountingConfirmed` vào state machine báo giá để kế toán có thể xác nhận đã nhận tiền sau khi khách confirm. Lưu timestamp xác nhận để tổng hợp doanh thu thực thu. Đồng thời bổ sung `QuotationSystemSettings` (lưu DB) cho phép admin cấu hình trường ngày dùng trong báo cáo doanh thu.

## Scope

### In scope
- Enum mới `QuotationStatus.AccountingConfirmed = 4`
- State machine: `Confirmed → AccountingConfirmed`, `AccountingConfirmed → Cancelled`
- 2 permission mới: `quotations.accounting_confirm`, `quotations.cancel_accounting_confirmed`
- DB: 2 cột mới trên `quotations`, 1 bảng singleton mới `quotation_system_settings`
- Service, DTO, validator, controller cho transition + settings
- Frontend: types, status-pill, form page buttons, settings admin page, dashboard count
- Integration tests cho state machine mới

### Out of scope
- Báo cáo doanh thu chi tiết (đã có trong `reports.revenue`)
- Email/notification khi accounting confirm
- Sửa lại `AccountingConfirmedAt` sau khi đã set (cần cancel + re-confirm)

## Assumptions

- `AccountingConfirmed` nằm sau `Confirmed` trong rank order → lock-at threshold hoạt động đúng
- `CompareStatus`: `AccountingConfirmed => 3` (Draft=0, Sent=1, Confirmed=2)
- Seeder ACCOUNTANT role nhận `quotations.accounting_confirm` khi role chưa có bất kỳ permission nào (fallback path); nếu đã có permission thì admin tự cấp qua UI
- `QuotationSystemSettings` singleton seed qua EF `HasData` trong configuration (như `SystemBranding`)
- Dashboard hiện tại filter theo `QuotationDate`; config `"QuotationDate"` là default → backward compat
- Frontend: `<Can permission="quotations.accounting_confirm">` wrap nút "KT xác nhận"
- `Badge variant='info'` đã tồn tại hoặc sẽ dùng variant gần nhất (xem `status-pill.tsx`)

## Risks

- **Migration concurrent**: migration mới thêm 2 cột + 1 bảng — thực hiện khi WebApi không chạy, hoặc dùng advisory lock đã có trong `DbSeeder`
- **Dashboard behavior change**: khi admin đổi config sang `"ConfirmedAt"` hoặc `"AccountingConfirmedAt"`, số liệu dashboard thay đổi ngay lập tức — cần note rõ trong UI
- **TotalRevenue thay đổi**: sau deploy, `TotalRevenue` = Draft + Sent + Confirmed + **AccountingConfirmed** (không double-count — GroupBy theo current status). Tuy nhiên các báo cáo hoặc export đang dựa vào giá trị cũ cần được verify
- **TransitionValidator**: validator hiện tại có thể validate `QuotationAction` enum values — cần kiểm tra sau khi thêm `AccountingConfirm = 3`
- **Existing ACCOUNTANT permissions**: seeder chỉ gán permission mới khi role chưa có permission nào — môi trường production cần admin cấp thủ công (xem Phase 06 release note)

## Phases

- [x] Phase 01 — Domain & Enums (S) — `phase-01-domain-enums.md`
- [x] Phase 02 — DB Migration (S) — `phase-02-db-migration.md`
- [x] Phase 03 — Application Layer (M) — `phase-03-application-layer.md`
- [x] Phase 04 — QuotationSystemSettings (M) — `phase-04-quotation-system-settings.md`
- [x] Phase 05 — Dashboard Service Update (S) — `phase-05-dashboard-service.md`
- [x] Phase 06 — Permission Seed (S) — `phase-06-permission-seed.md`
- [x] Phase 07 — Frontend Types & Status Pill (S) — `phase-07-frontend-types.md`
- [x] Phase 08 — Frontend Form Page Buttons (S) — `phase-08-frontend-form.md`
- [x] Phase 09 — Frontend Settings Admin Page (M) — `phase-09-frontend-settings.md`
- [x] Phase 10 — Integration Tests (M) — `phase-10-integration-tests.md`

## Final Verification

```bash
# Build affected backend projects (không full-sln khi WebApi đang chạy)
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -nologo --verbosity minimal

# Integration tests
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo --filter "Quotation"

# Frontend
cd frontend && npm run build && npm test -- --run
```

Smoke test thủ công (sau khi restart WebApi):
1. Login ACCOUNTANT → mở báo giá đang `Confirmed` → thấy nút "KT xác nhận" → bấm → status đổi sang "KT xác nhận"
2. Login ADMIN → `/settings/quotation` → đổi config → kiểm tra dashboard thay đổi theo
3. Login ADMIN → mở báo giá `AccountingConfirmed` → thấy nút "Hủy" → bấm → status `Cancelled`

## Rollback / Recovery

- Migration: `dotnet ef database update <tên migration trước> --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi`
- Xóa file migration mới + cập nhật `AppDbContextModelSnapshot.cs`
- Frontend: `git restore frontend/src/`
- Permission rows: xóa 2 rows trong bảng `permissions` với code `quotations.accounting_confirm` và `quotations.cancel_accounting_confirmed`
