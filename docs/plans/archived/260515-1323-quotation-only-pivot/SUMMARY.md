# Quotation-only pivot — implementation plan

## Goal

Pivot product scope to "Báo giá là chứng từ duy nhất". Drop Đơn hàng và Bàn giao khỏi domain. Vòng đời báo giá: `Draft → Sent → Confirmed → Cancelled`. Khi flip sang `Confirmed`, snapshot `ConfirmedAt` + `ConfirmedByUserId` để ghi nhận doanh thu cho owner sale. Hủy báo giá đã `Confirmed` cần permission đặc biệt. Thêm báo cáo doanh thu theo sale (group theo `ConfirmedAt`, hiển thị cả Total và Subtotal). Tài liệu cập nhật theo scope mới.

Brainstorm context: [docs/brainstorms/260515-1249-quotation-only-pivot/SUMMARY.md](../../brainstorms/260515-1249-quotation-only-pivot/SUMMARY.md).

## Scope

### In scope
- Bỏ enum `QuotationStatus.ConvertedToOrder`; vòng đời mới 4 trạng thái.
- Thêm `ConfirmedAt`, `ConfirmedByUserId`, `CancelledAt` vào entity `Quotation`.
- Bỏ enum `OrderStatus`, `PaymentStatus`, `PaymentMethod`, `DocumentType` khỏi `Enums.cs` (chưa entity nào dùng).
- Bỏ `Permissions.Quotations.ConvertToOrder`, toàn bộ `Permissions.Orders.*`; thêm `Permissions.Quotations.CancelConfirmed`.
- Mở rộng `QuotationService.TransitionAsync` để snapshot timestamp + check `CancelConfirmed` permission khi hủy báo giá Confirmed.
- Thêm endpoint `GET /api/reports/sales-revenue` + service tương ứng (re-use `Permissions.Reports.Revenue`).
- EF migration: thêm cột mới + backfill (`Status=4 → 3`, `ConfirmedAt = updated_at` cho row đã Confirmed).
- DbSeeder: bỏ permission/role mapping liên quan Orders, thêm `CancelConfirmed` cho ADMIN + MANAGER.
- Frontend: gỡ `ConvertedToOrder` khỏi mọi component/type, thêm cảnh báo khi cancel báo giá Confirmed, build trang `/reports/sales-revenue`.
- Integration tests cho confirm/cancel-with-snapshot và sales revenue report.
- Tài liệu: tạo `docs/project-pdr/product-goals.md`, archive BD doc cũ, refresh `docs/SUMMARY.md`.

### Out of scope
- Không tạo entity `Order`/`Delivery`/`Payment`.
- Không tracking thanh toán nhiều đợt, công nợ.
- Không in BBBG, PXK.
- Không build báo cáo lợi nhuận, công nợ, giao hàng (giữ permission constants `Reports.Profit/Debt/Delivery` nguyên vẹn để reserve cho future, nhưng không seed endpoint mới).
- Không thay đổi logic `lock-at` ngoài việc loại trừ `ConvertedToOrder` khỏi danh sách hợp lệ.
- Không xóa permission `quotations.convert` khỏi DB tự động (chỉ stop seeding; orphan row trong bảng `permissions` không gây hại).

## Assumptions

- Database hiện tại có thể có row `Quotation` ở `Status=4 (ConvertedToOrder)`; migration phải map sang `3 (Confirmed)` an toàn.
- `updated_at` trên `Quotation` là proxy hợp lý cho `ConfirmedAt` khi backfill row Confirmed sẵn — không có timestamp chính xác hơn.
- WebApi đang chạy dev sẽ không restart; chỉ build các project library bị thay đổi (Domain, Application, Infrastructure) để verify (theo `feedback_build_skip_when_app_running`).
- `Permissions.Reports.Revenue` (đã seed) đủ làm gating cho endpoint sales-revenue mới, không cần thêm permission code riêng cho báo cáo này.
- Frontend menu/sidebar được thay đổi tại file routing/sidebar đã có; chưa cần redesign IA.

## Risks

- **EF migration backfill** chạy sai có thể mất dữ liệu Quotation đã Confirmed. Mitigation: dùng `UPDATE` với `WHERE` rõ ràng + run trên backup trước; review SQL bằng `dotnet ef migrations script`.
- **Type narrowing TypeScript**: gỡ `'ConvertedToOrder'` khỏi union type sẽ phát sinh hàng loạt compile error ở nơi switch/map. Mitigation: dựa vào danh sách 22 file đã grep ra (xem phase 3) để cover hết.
- **Frontend lock-at form**: bỏ option `ConvertedToOrder` khiến user đang chọn nó (nếu có) bị giá trị invalid. Mitigation: backend validator đã reject, frontend hiển thị "Không khoá" làm fallback.
- **Doanh thu ảo do sale tự confirm**: log `ConfirmedByUserId` đầy đủ; không xử lý ở plan này nhưng note trong product-goals doc.

## Phases

- [x] Phase 01 — Backend domain + EF migration (M) — [phase-01-backend-domain-and-migration.md](phase-01-backend-domain-and-migration.md)
- [x] Phase 02 — Application service, API, sales revenue report (L) — [phase-02-application-api-and-revenue-report.md](phase-02-application-api-and-revenue-report.md)
- [x] Phase 03 — Frontend cleanup + revenue report page (M) — [phase-03-frontend.md](phase-03-frontend.md)
- [x] Phase 04 — Integration tests + documentation (M) — [phase-04-tests-and-docs.md](phase-04-tests-and-docs.md) (tests written + compile; execution deferred — Docker not running)

## Final Verification

After all phases pass:

1. Backend builds:
   - `dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj`
   - `dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj`
   - `dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj`
   - `dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj`
2. EF migration script preview (read-only): `dotnet ef migrations script --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi --idempotent --no-build` — verify backfill SQL is correct.
3. Integration tests pass: `dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~Quotation|FullyQualifiedName~SalesRevenue"`.
4. Frontend type check + tests: `cd frontend ; npm run typecheck ; npm test`.
5. Manual smoke test (only if WebApi was restarted as part of Phase 1 verification):
   - Đăng nhập admin → tạo báo giá mới → chuyển Sent → chuyển Confirmed → kiểm tra `ConfirmedAt` lưu đúng.
   - Vào `/reports/sales-revenue` → thấy báo giá vừa Confirmed trong tổng doanh thu của user admin.
   - Sale (không có `CancelConfirmed`) thử hủy báo giá Confirmed → backend trả 403.

## Rollback / Recovery

- **Backend code**: revert commit chứa Phase 01-04. Migration mới chưa apply cho prod thì chỉ cần xóa file migration; nếu đã apply, viết migration ngược (drop 3 cột mới, restore enum value 4 — không khôi phục được semantic ConvertedToOrder cũ nếu đã có data flip).
- **Database backup**: chụp `pg_dump` bảng `quotations` + `permissions` + `role_permissions` trước khi apply migration.
- **Frontend**: revert commit; SPA cache clear.
- **Permission orphan**: nếu cần dọn `permissions` table, chạy thủ công `DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code IN ('quotations.convert', 'orders.view', 'orders.create', 'orders.update', 'orders.delete', 'orders.print', 'orders.deliver', 'orders.pay', 'orders.view_cost')); DELETE FROM permissions WHERE code IN (...);` (không bắt buộc, không gây hại nếu để lại).
