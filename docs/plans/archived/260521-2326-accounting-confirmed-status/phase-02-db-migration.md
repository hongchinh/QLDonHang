# Phase 02 — DB Migration

**Status:** [ ] pending
**Complexity:** S

## Objective

Tạo EF migration thêm 2 cột vào `quotations` và tạo bảng `quotation_system_settings` singleton. Migration phải chạy được khi WebApi không chạy (hoặc sau khi stop).

## Files

- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/QuotationSystemSettingsConfiguration.cs` ← file mới
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs`
- Migration file được generate bởi `dotnet ef migrations add`

> **Prerequisite**: `QuotationSystemSettings` entity đã được tạo ở Phase 01 — phase này chỉ thêm EF configuration và migration.

## Tasks

1. **Tạo EF configuration** tại `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/QuotationSystemSettingsConfiguration.cs` (pattern như `SystemBrandingConfiguration`):
   ```csharp
   using Microsoft.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore.Metadata.Builders;
   using OrderMgmt.Domain.Entities.Sales;

   namespace OrderMgmt.Infrastructure.Persistence.Configurations;

   public class QuotationSystemSettingsConfiguration : IEntityTypeConfiguration<QuotationSystemSettings>
   {
       public void Configure(EntityTypeBuilder<QuotationSystemSettings> b)
       {
           b.ToTable("quotation_system_settings");
           b.HasKey(x => x.Id);
           b.Property(x => x.Id).ValueGeneratedNever();
           b.Property(x => x.RevenueReportingDateField).HasMaxLength(50).IsRequired();

           b.HasData(new QuotationSystemSettings
           {
               Id = 1,
               RevenueReportingDateField = "QuotationDate",
               UpdatedAt = DateTimeOffset.UnixEpoch,
           });
       }
   }
   ```

2. **`IAppDbContext`** — thêm:
   ```csharp
   DbSet<QuotationSystemSettings> QuotationSystemSettings { get; }
   ```

3. **`AppDbContext`** — thêm:
   ```csharp
   public DbSet<QuotationSystemSettings> QuotationSystemSettings => Set<QuotationSystemSettings>();
   ```

4. **Generate migration** (chạy từ thư mục gốc dự án):
   ```bash
   dotnet ef migrations add AddAccountingConfirmedStatus \
     --project backend/src/OrderMgmt.Infrastructure \
     --startup-project backend/src/OrderMgmt.WebApi \
     --output-dir Persistence/Migrations
   ```
   Migration này sẽ tự bao gồm:
   - `ALTER TABLE quotations ADD COLUMN accounting_confirmed_at timestamptz NULL`
   - `ALTER TABLE quotations ADD COLUMN accounting_confirmed_by_user_id uuid NULL`
   - `CREATE TABLE quotation_system_settings (...)`
   - `INSERT INTO quotation_system_settings (id, revenue_reporting_date_field, updated_at) VALUES (1, 'QuotationDate', ...)`

5. **Kiểm tra migration file**: mở file vừa tạo, xác nhận có `AddColumn` cho 2 cột mới và `CreateTable` cho `quotation_system_settings`. Nếu thiếu, kiểm tra lại EF configuration.

## Verification

```bash
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal
```

Sau khi restart WebApi hoặc chạy integration tests (Testcontainers sẽ apply migration):
```bash
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo --filter "QuotationCrud"
```

## Exit Criteria

- Infrastructure build thành công
- Migration file chứa đủ 2 `AddColumn` và `CreateTable quotation_system_settings`
- `HasData` seed row `Id=1, RevenueReportingDateField="QuotationDate"` có trong migration
- Integration test `QuotationCrudTests` vẫn pass (migration không phá existing schema)
