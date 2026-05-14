# Phase 01 — Quotation ownership + migration

**Status:** [x] complete
**Complexity:** M

## Objective

Thêm trường `OwnerUserId` (NOT NULL, FK User) vào entity `Quotation`. Migration EF có SQL backfill an toàn cho dữ liệu dev. `QuotationService.CreateAsync` set owner = current user.

## Files

- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` (modify)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` (modify)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/<auto>_AddQuotationOwner.cs` (new, via `dotnet ef migrations add`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (modify — set OwnerUserId on Create)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` (modify — thêm `OwnerUserId`, `OwnerFullName`, `IsOwnerDeleted`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationListItemDto.cs` (modify — thêm `OwnerUserId`, `OwnerFullName`, `IsOwnerDeleted`)

## Tasks

1. Thêm property `Guid OwnerUserId { get; set; }` và navigation `User? Owner { get; set; }` vào `Quotation` entity.
2. Cập nhật `QuotationConfiguration.Configure`:
   - `b.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);`
   - `b.HasIndex(x => new { x.OwnerUserId, x.IsDeleted, x.QuotationDate }).HasDatabaseName("ix_quotations_owner_status_date");`
3. Trước khi apply migration trên DB đã có dữ liệu, chạy preflight:
   - Nếu `SELECT COUNT(*) FROM quotations WHERE created_by IS NULL` = 0 → không cần admin fallback.
   - Nếu có dòng `created_by IS NULL` → phải đảm bảo `SELECT id FROM users WHERE username = 'admin'` trả đúng 1 dòng trước khi chạy migration.
   - Không dựa vào `Database:AutoMigrateAndSeed` để tạo admin cho migration này, vì `DbSeeder` chạy `MigrateAsync()` trước khi seed admin.
4. Tạo migration:
   ```powershell
   dotnet ef migrations add AddQuotationOwner --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi --output-dir Persistence/Migrations
   ```
5. Sửa file migration sinh ra để thêm SQL backfill **giữa** `AddColumn` (nullable trước) và `AlterColumn` (NOT NULL sau):
   ```csharp
   migrationBuilder.AddColumn<Guid>(
       name: "owner_user_id", table: "quotations", type: "uuid", nullable: true);

   migrationBuilder.Sql(@"
       UPDATE quotations
       SET owner_user_id = COALESCE(
           created_by,
           (SELECT id FROM users WHERE username = 'admin' LIMIT 1)
       )
       WHERE owner_user_id IS NULL;
   ");
   migrationBuilder.Sql(@"
       DO $$
       BEGIN
           IF EXISTS (SELECT 1 FROM quotations WHERE owner_user_id IS NULL) THEN
               RAISE EXCEPTION 'AddQuotationOwner backfill failed: admin user missing or quotations.created_by null and no admin to fallback';
           END IF;
       END$$;
   ");

   migrationBuilder.AlterColumn<Guid>(
       name: "owner_user_id", table: "quotations", type: "uuid", nullable: false,
       oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);

   migrationBuilder.AddForeignKey(
       name: "fk_quotations_users_owner_user_id", table: "quotations", column: "owner_user_id",
       principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Restrict);

   migrationBuilder.CreateIndex(
       name: "ix_quotations_owner_status_date", table: "quotations",
       columns: new[] { "owner_user_id", "is_deleted", "quotation_date" });
   ```
6. Trong `QuotationService.CreateAsync`, set `quotation.OwnerUserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");` trước khi `Add`.
7. Thêm `OwnerUserId` (Guid), `OwnerFullName` (string?) và `IsOwnerDeleted` (bool) vào `QuotationDto` + `QuotationListItemDto`.
8. Cập nhật `QuotationService.ListAsync` select để map owner bằng `_db.Users.IgnoreQueryFilters().Where(u => u.Id == q.OwnerUserId)`, lấy cả `FullName` và `IsDeleted`. Không dùng `_db.Users.Where(...)` thường vì sẽ ẩn owner đã soft-delete.
9. Cập nhật `QuotationService.MapToDto` để map `OwnerUserId`, `OwnerFullName`, `IsOwnerDeleted`. Với detail, load owner bằng query riêng `IgnoreQueryFilters()` hoặc include trên query đã `IgnoreQueryFilters()` có kiểm soát; không phụ thuộc `.Include(q => q.Owner)` thường.

## Verification

```powershell
dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet ef database update --project backend/src/OrderMgmt.Infrastructure --startup-project backend/src/OrderMgmt.WebApi
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Quotation"
```

Database verify (psql):
```sql
SELECT COUNT(*) FROM quotations WHERE owner_user_id IS NULL;  -- expect 0
\d quotations  -- xác nhận FK và index
```

## Exit Criteria

- Migration `AddQuotationOwner` apply thành công trên DB dev có dữ liệu cũ.
- Mọi báo giá hiện hữu có `owner_user_id` = `created_by` (hoặc admin nếu null).
- Tạo báo giá mới qua API set đúng `OwnerUserId = current user`.
- `QuotationDto`/`QuotationListItemDto` có `OwnerUserId`, `OwnerFullName`, `IsOwnerDeleted`; owner đã soft-delete vẫn hiển thị được tên + flag.
- Toàn bộ test hiện tại của module Quotation vẫn pass (chưa thêm test owner-scoping ở phase này).
