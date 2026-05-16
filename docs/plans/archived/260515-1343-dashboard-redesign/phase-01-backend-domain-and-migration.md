# Phase 01 — Backend Domain + Migration + Status hooks

**Status:** [ ] pending
**Complexity:** M

## Objective

Thêm 3 cột mới (`ConfirmedAt`, `ConfirmedByUserId`, `CancelledAt`) vào entity `Quotation`; cập nhật `QuotationService` set tự động khi status chuyển sang `Confirmed`/`Cancelled`; thêm composite index hỗ trợ query dashboard; viết migration backfill cho data cũ.

Không đổi enum `QuotationStatus` (giữ nguyên 5 giá trị). Dashboard Phase 02 sẽ dùng `COALESCE(ConfirmedAt, QuotationDate)` để vẫn tính được doanh thu lịch sử.

## Files

- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` (edit)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` (edit)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/<timestamp>_AddQuotationConfirmedCancelledAt.cs` (new, via `dotnet ef migrations add`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (edit — wire status hooks)

## Tasks

1. **Domain entity** — sửa [Quotation.cs](../../../backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs):
   ```csharp
   public DateTime? ConfirmedAt { get; set; }
   public Guid? ConfirmedByUserId { get; set; }
   public DateTime? CancelledAt { get; set; }
   ```
   Đặt sau property `Status` để giữ nhóm logical.

2. **EF Configuration** — sửa `SalesConfiguration.QuotationConfiguration.Configure`:
   - Map 3 cột mới với type `timestamp with time zone` (Npgsql mặc định cho `DateTime` không phải `DateTimeOffset`, dùng `timestamptz` rõ ràng).
   - Thêm index mới (giữ index `ix_quotations_owner_status_date` hiện có):
     ```csharp
     b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted, x.Status, x.ConfirmedAt })
         .HasDatabaseName("ix_quotations_owner_status_confirmed_at");
     ```

3. **EF Migration** — chạy:
   ```powershell
   dotnet ef migrations add AddQuotationConfirmedCancelledAt `
     --project backend/src/OrderMgmt.Infrastructure `
     --startup-project backend/src/OrderMgmt.WebApi `
     --output-dir Persistence/Migrations
   ```

4. **Backfill trong migration** — mở file migration vừa generate, ở method `Up(MigrationBuilder migrationBuilder)` sau khi `AddColumn`, thêm:
   ```csharp
   migrationBuilder.Sql(@"
       UPDATE quotations
       SET confirmed_at = updated_at
       WHERE status = 3 AND confirmed_at IS NULL;

       UPDATE quotations
       SET confirmed_at = updated_at
       WHERE status = 4 AND confirmed_at IS NULL;   -- ConvertedToOrder cũng coi như confirmed cho dashboard

       UPDATE quotations
       SET cancelled_at = updated_at
       WHERE status = 9 AND cancelled_at IS NULL;
   ");
   ```
   Chú ý column name dạng `snake_case` (theo convention Npgsql/EFCore.NamingConventions nếu repo dùng — kiểm tra trong file migration `20260514094818_AddQuotationOwner.cs` xem column naming style; nếu là `PascalCase` thì điều chỉnh).

5. **Status hooks trong QuotationService** — sửa method nào đang đổi `Status` (tìm bằng `Grep` `Status =` trong [QuotationService.cs](../../../backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs)):
   - Khi `entity.Status = QuotationStatus.Confirmed`: nếu `ConfirmedAt == null` → set `ConfirmedAt = _clock.Now.UtcDateTime`, `ConfirmedByUserId = _currentUser.UserId`.
   - Khi `entity.Status = QuotationStatus.Cancelled`: nếu `CancelledAt == null` → set `CancelledAt = _clock.Now.UtcDateTime`.
   - Nếu service hiện chưa có method dedicated cho status (chỉ có Update generic), thêm helper private `ApplyStatusTimestamps(Quotation q, QuotationStatus newStatus)` gọi trước khi `_db.SaveChangesAsync`.

6. **Apply migration vào DB dev**:
   ```powershell
   dotnet ef database update `
     --project backend/src/OrderMgmt.Infrastructure `
     --startup-project backend/src/OrderMgmt.WebApi
   ```

## Verification

```powershell
# 1. Library build (không restart WebApi)
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj

# 2. Migration sạch (script preview, không apply)
dotnet ef migrations script --idempotent `
  --project backend/src/OrderMgmt.Infrastructure `
  --startup-project backend/src/OrderMgmt.WebApi `
  --output backend/artifacts/migration-preview.sql
```

Mở `migration-preview.sql` kiểm tra:
- `ADD COLUMN confirmed_at timestamptz NULL`, `confirmed_by_user_id uuid NULL`, `cancelled_at timestamptz NULL`.
- `CREATE INDEX ix_quotations_owner_status_confirmed_at ...`.
- 3 `UPDATE` backfill cho status 3, 4, 9.

Manual SQL check sau khi apply migration:
```sql
SELECT COUNT(*) FROM quotations WHERE status IN (3, 4) AND confirmed_at IS NULL;  -- expect 0
SELECT COUNT(*) FROM quotations WHERE status = 9 AND cancelled_at IS NULL;        -- expect 0
```

## Exit Criteria

- 3 cột mới nullable tồn tại trong DB; index mới `ix_quotations_owner_status_confirmed_at` đã tạo.
- 100% row `Status ∈ {Confirmed, ConvertedToOrder}` có `confirmed_at` không null.
- 100% row `Status = Cancelled` có `cancelled_at` không null.
- `QuotationService` set `ConfirmedAt`/`CancelledAt` tự động khi status flip (sẽ verify tích hợp ở Phase 06).
- 3 dotnet build pass, không cảnh báo nullable/EF mới.
