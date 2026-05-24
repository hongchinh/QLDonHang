# Phase 01 — DB + Domain

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm 6 column mới vào entity `UserQuotationSettings` và tạo EF migration để lưu per-user template cho 2 loại biên bản.

## Files

- `backend/src/OrderMgmt.Domain/Entities/Identity/UserQuotationSettings.cs`
- `backend/src/OrderMgmt.Infrastructure/Migrations/<timestamp>_AddHandoverTemplateFields.cs` (generated)
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (generated)

## Tasks

### Task 1.1 — Mở rộng entity `UserQuotationSettings`

1. **Mở file** `backend/src/OrderMgmt.Domain/Entities/Identity/UserQuotationSettings.cs`

2. **Thêm 6 property** sau property `TemplateUploadedAt` hiện có:

   ```csharp
   public string? HandoverWithPriceTemplateFileName { get; set; }
   public string? HandoverWithPriceTemplateOriginalName { get; set; }
   public DateTimeOffset? HandoverWithPriceTemplateUploadedAt { get; set; }

   public string? HandoverNoPriceTemplateFileName { get; set; }
   public string? HandoverNoPriceTemplateOriginalName { get; set; }
   public DateTimeOffset? HandoverNoPriceTemplateUploadedAt { get; set; }
   ```

3. **Build project** để kiểm tra compile:
   ```
   dotnet build backend/src/OrderMgmt.Domain/OrderMgmt.Domain.csproj
   ```
   Expected: Build succeeded, 0 errors.

### Task 1.2 — Tạo EF migration

1. **Tạo migration** từ thư mục gốc của solution:
   ```
   dotnet ef migrations add AddHandoverTemplateFields \
     --project backend/src/OrderMgmt.Infrastructure \
     --startup-project backend/src/OrderMgmt.WebApi
   ```

2. **Kiểm tra file migration** được tạo ra có đúng 6 `AddColumn` calls với snake_case names:
   - `handover_with_price_template_file_name`
   - `handover_with_price_template_original_name`
   - `handover_with_price_template_uploaded_at`
   - `handover_no_price_template_file_name`
   - `handover_no_price_template_original_name`
   - `handover_no_price_template_uploaded_at`

   Và `Down()` có 6 `DropColumn` tương ứng.

3. **Build Infrastructure** để xác nhận migration hợp lệ:
   ```
   dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
   ```
   Expected: Build succeeded.

4. **Commit:**
   ```
   git add backend/src/OrderMgmt.Domain/Entities/Identity/UserQuotationSettings.cs \
           backend/src/OrderMgmt.Infrastructure/Migrations/ \
           backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs
   git commit -m "feat: add handover template fields to UserQuotationSettings"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` → 0 errors
- Migration file tồn tại và có đủ 6 AddColumn calls

## Exit Criteria

- Entity có 9 template-related properties (3 cũ + 6 mới)
- Migration file được tạo và build thành công
