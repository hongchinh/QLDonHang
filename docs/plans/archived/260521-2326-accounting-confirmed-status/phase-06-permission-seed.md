# Phase 06 — Permission Seed

**Status:** [ ] pending
**Complexity:** S

## Objective

Seed 2 permission mới vào DB và gán `quotations.accounting_confirm` cho role ACCOUNTANT (vào default permission list của role đó).

## Files

- `backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs`

## Tasks

1. **`SeedPermissionsAsync`** — thêm 3 entries vào `permissionDefs`:
   ```csharp
   (Permissions.Quotations.AccountingConfirm, Permissions.SalesModule, "Kế toán xác nhận đã nhận tiền"),
   (Permissions.Quotations.CancelAccountingConfirmed, Permissions.SalesModule, "Huỷ báo giá đã kế toán xác nhận"),
   (Permissions.System.ManageSettings, Permissions.SystemModule, "Quản trị cấu hình hệ thống"),
   ```
   Đặt 2 quotation permissions sau `Permissions.Quotations.BypassLock`; `ManageSettings` theo nhóm system.

2. **`SeedRolesAsync` — role ACCOUNTANT** — thêm `Permissions.Quotations.AccountingConfirm` vào mảng permissions của `RoleCodes.Accountant`:
   ```csharp
   (RoleCodes.Accountant, "Kế toán", new[]
   {
       Permissions.Customers.View, Permissions.Products.View,
       Permissions.Quotations.View,
       Permissions.Quotations.ViewAll,
       Permissions.Quotations.AccountingConfirm,   // ← thêm
       Permissions.Reports.Revenue, Permissions.Reports.Debt,
   }),
   ```

   **Quan trọng**: Seeder hiện tại chỉ assign default permissions khi role ACCOUNTANT chưa có bất kỳ permission nào (`role.RolePermissions.Count == 0`). Nếu role đã có permission (môi trường đang chạy), admin phải tự cấp `quotations.accounting_confirm` qua UI roles management. Ghi note này vào comment trong seeder.

3. **Role ADMIN** tự động nhận tất cả permission (seeder re-apply `allPermissions` mỗi lần khởi động) → không cần làm gì thêm.

4. **Role MANAGER** tương tự ADMIN → tự động nhận `system.manage_settings`.

> **Release note cho môi trường đang chạy**: Seeder chỉ gán `quotations.accounting_confirm` cho ACCOUNTANT khi role chưa có bất kỳ permission nào. Trên env production/staging đang chạy, admin phải vào **UI Roles Management → ACCOUNTANT → thêm permission `quotations.accounting_confirm`** thủ công sau khi deploy. Ghi chú này cần được đưa vào changelog/deploy runbook của release này.

## Verification

```bash
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal
```

Sau restart WebApi: `GET /api/permissions` (nếu có endpoint đó) hoặc kiểm tra DB `SELECT code FROM permissions WHERE code LIKE 'quotations.%'` — phải có 2 rows mới.

Hoặc chạy integration tests (Testcontainers seed mới):
```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo --filter "Permission"
```

## Exit Criteria

- Infrastructure build thành công
- 3 permission mới xuất hiện trong DB sau seed: `quotations.accounting_confirm`, `quotations.cancel_accounting_confirmed`, `system.manage_settings`
- Role ACCOUNTANT có `quotations.accounting_confirm` trong default list (seed mới hoặc khi `RolePermissions.Count == 0`)
- Role ADMIN/MANAGER tự động nhận `system.manage_settings`
