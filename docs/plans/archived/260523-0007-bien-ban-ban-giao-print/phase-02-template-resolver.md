# Phase 02 — Config + Template Resolver

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm config paths cho 2 system templates biên bản và mở rộng `IQuotationExportPathResolver` / `QuotationExportPathResolver` để resolve template theo loại tài liệu.

## Files

- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportOptions.cs`
- `backend/src/OrderMgmt.WebApi/appsettings.json`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationExportPathResolver.cs`
- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportPathResolver.cs`

## Tasks

### Task 2.1 — Thêm `QuotationTemplateType` enum

1. **Tạo file mới** `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationTemplateType.cs`:

   ```csharp
   namespace OrderMgmt.Application.Sales.Quotations.Models;

   public enum QuotationTemplateType
   {
       Quotation,
       HandoverWithPrice,
       HandoverNoPrice,
   }
   ```

2. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors.

### Task 2.2 — Thêm template paths vào `QuotationExportOptions`

1. **Mở** `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportOptions.cs`

2. **Thêm 2 property** sau `TemplatePath`:

   ```csharp
   public string HandoverWithPriceTemplatePath { get; set; } = string.Empty;
   public string HandoverNoPriceTemplatePath { get; set; } = string.Empty;
   ```

3. **Mở** `backend/src/OrderMgmt.WebApi/appsettings.json`, section `"QuotationExport"`, thêm:

   ```json
   "HandoverWithPriceTemplatePath": "templates/templete_bbbg.xlsx",
   "HandoverNoPriceTemplatePath": "templates/templete_bbbg_sl.xlsx"
   ```

   > **Lưu ý:** Tên file đã đổi so với brainstorm (`BIENBANBANGIAOKIEMPHIEUXUAT.xlsx` → `templete_bbbg.xlsx`, `BIENBANBANGIAO.xlsx` → `templete_bbbg_sl.xlsx`). Xác nhận lại trước khi commit bằng cách mở 2 file để kiểm tra layout khớp với Pre-task Phase 03.

### Task 2.3 — Mở rộng interface `IQuotationExportPathResolver`

1. **Mở** `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationExportPathResolver.cs`

2. **Thêm method mới** sau `ResolveTemplatePathAsync`:

   ```csharp
   /// <summary>
   /// Returns the absolute path to the handover template for the given user and type.
   /// Falls back to the system template when the user has no per-user template or the file is missing.
   /// </summary>
   Task<string> ResolveHandoverTemplatePathAsync(
       Guid ownerUserId,
       QuotationTemplateType type,
       CancellationToken ct = default);
   ```

   Thêm `using OrderMgmt.Application.Sales.Quotations.Models;` nếu chưa có.

### Task 2.4 — Implement `ResolveHandoverTemplatePathAsync`

1. **Mở** `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportPathResolver.cs`

2. **Thêm method**:

   ```csharp
   public async Task<string> ResolveHandoverTemplatePathAsync(
       Guid ownerUserId,
       QuotationTemplateType type,
       CancellationToken ct = default)
   {
       var opts = _options.CurrentValue;

       // Xác định tên file user template theo type
       var (userFileName, systemPath) = type switch
       {
           QuotationTemplateType.HandoverWithPrice =>
               ($"{ownerUserId}_handover_with_price.xlsx", opts.HandoverWithPriceTemplatePath),
           QuotationTemplateType.HandoverNoPrice =>
               ($"{ownerUserId}_handover_no_price.xlsx", opts.HandoverNoPriceTemplatePath),
           _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
       };

       var userDir = ResolveAbsolute(opts.UserTemplatesPath);
       var userPath = Path.Combine(userDir, userFileName);
       if (File.Exists(userPath)) return userPath;

       return ResolveAbsolute(systemPath);
   }
   ```

   Thêm `using OrderMgmt.Application.Sales.Quotations.Models;` ở đầu file.

3. **Build Infrastructure:**
   ```
   dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
   ```
   Expected: 0 errors.

4. **Commit:**
   ```
   git commit -m "feat: extend template resolver for handover document types"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` → 0 errors
- `QuotationTemplateType` enum tồn tại trong Application layer
- `ResolveHandoverTemplatePathAsync` có trong interface và implementation

## Exit Criteria

- `QuotationExportOptions` có `HandoverWithPriceTemplatePath` và `HandoverNoPriceTemplatePath`
- `appsettings.json` trỏ đúng 2 file template đã có sẵn
- Interface và implementation đều compile thành công
