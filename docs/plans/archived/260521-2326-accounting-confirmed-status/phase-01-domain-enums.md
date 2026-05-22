# Phase 01 — Domain & Enums

**Status:** [ ] pending
**Complexity:** S

## Objective

Bổ sung các enum values mới vào Domain layer và thêm 2 fields vào `Quotation` entity. Đây là nền tảng cho mọi phase sau.

## Files

- `backend/src/OrderMgmt.Domain/Enums/Enums.cs`
- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs`
- `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationSystemSettings.cs` ← file mới
- `backend/src/OrderMgmt.Domain/Constants/Permissions.cs`

## Tasks

1. **Enums.cs** — thêm vào enum `QuotationStatus`:
   ```csharp
   AccountingConfirmed = 4,
   ```
   Đặt sau `Confirmed = 3`, trước `Cancelled = 9`.

2. **Enums.cs** — thêm vào enum `QuotationActivityAction`:
   ```csharp
   AccountingConfirmed = 8,
   ```

3. **Quotation.cs** — thêm 2 properties sau `CancelledAt`:
   ```csharp
   public DateTime? AccountingConfirmedAt { get; set; }
   public Guid? AccountingConfirmedByUserId { get; set; }
   ```

4. **Permissions.cs** — trong `public static class Quotations` thêm 2 constants:
   ```csharp
   public const string AccountingConfirm = "quotations.accounting_confirm";
   public const string CancelAccountingConfirmed = "quotations.cancel_accounting_confirmed";
   ```

5. **Permissions.cs** — thêm inner class `System` (ngang hàng với `Quotations`, `Users`, v.v.):
   ```csharp
   public static class System
   {
       public const string ManageSettings = "system.manage_settings";
   }
   ```
   Dùng cho controller `QuotationSettingsController` thay vì `[Authorize(Roles=...)]` — đảm bảo nhất quán với pattern `[HasPermission]` của toàn bộ codebase.

6. **Tạo `QuotationSystemSettings.cs`** tại `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationSystemSettings.cs`:
   ```csharp
   namespace OrderMgmt.Domain.Entities.Sales;

   public class QuotationSystemSettings
   {
       public int Id { get; set; }
       // "QuotationDate" | "ConfirmedAt" | "AccountingConfirmedAt"
       public string RevenueReportingDateField { get; set; } = "QuotationDate";
       public DateTimeOffset UpdatedAt { get; set; }
       public Guid? UpdatedBy { get; set; }
   }
   ```
   Entity thuộc Domain layer — đặt ở đây để Infrastructure có thể reference mà không cần dependency ngược.

## Verification

```bash
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj -nologo --verbosity minimal
```

## Exit Criteria

- Build Domain thành công, không có warning mới
- `QuotationStatus.AccountingConfirmed` = 4, `QuotationActivityAction.AccountingConfirmed` = 8
- `Quotation` entity có `AccountingConfirmedAt` và `AccountingConfirmedByUserId`
- `Permissions.Quotations.AccountingConfirm` và `CancelAccountingConfirmed` tồn tại
- `Permissions.System.ManageSettings = "system.manage_settings"` tồn tại
- `QuotationSystemSettings` entity có đủ 4 properties: `Id`, `RevenueReportingDateField`, `UpdatedAt`, `UpdatedBy`
