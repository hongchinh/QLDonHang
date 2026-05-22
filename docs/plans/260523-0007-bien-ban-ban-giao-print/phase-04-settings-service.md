# Phase 04 — Settings Service + DTO

**Status:** [ ] pending
**Complexity:** M

## Objective

Mở rộng `UserQuotationSettingsDto` với 6 field mới, thêm type-aware methods vào `IUserQuotationSettingsService` và implement trong `UserQuotationSettingsService`.

## Files

- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/UserQuotationSettingsDto.cs`
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Interfaces/IUserQuotationSettingsService.cs`
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/UserQuotationSettingsService.cs`

## Tasks

### Task 4.1 — Mở rộng `UserQuotationSettingsDto`

1. **Mở** `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/UserQuotationSettingsDto.cs`

2. **Thêm 6 property** sau `TemplateUploadedAt`:

   ```csharp
   public string? HandoverWithPriceTemplateFileName { get; set; }
   public string? HandoverWithPriceTemplateOriginalName { get; set; }
   public DateTimeOffset? HandoverWithPriceTemplateUploadedAt { get; set; }

   public string? HandoverNoPriceTemplateFileName { get; set; }
   public string? HandoverNoPriceTemplateOriginalName { get; set; }
   public DateTimeOffset? HandoverNoPriceTemplateUploadedAt { get; set; }
   ```

3. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors.

### Task 4.2 — Mở rộng interface `IUserQuotationSettingsService`

1. **Mở** `backend/src/OrderMgmt.Application/Identity/UserSettings/Interfaces/IUserQuotationSettingsService.cs`

2. **Thêm using** `using OrderMgmt.Application.Sales.Quotations.Models;`

3. **Thêm 3 method mới** sau các methods hiện tại:

   ```csharp
   Task<UserQuotationSettingsDto> UploadHandoverTemplateAsync(
       UploadedFile file,
       QuotationTemplateType type,
       CancellationToken ct = default);

   Task<UserQuotationSettingsDto> DeleteHandoverTemplateAsync(
       QuotationTemplateType type,
       CancellationToken ct = default);

   Task<(Stream Stream, string FileName)?> GetCurrentUserHandoverTemplateStreamAsync(
       QuotationTemplateType type,
       CancellationToken ct = default);
   ```

4. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors (có thể có error do chưa implement — bỏ qua trong bước này nếu service là concrete class, không interface).

### Task 4.3 — Implement 3 method mới trong `UserQuotationSettingsService`

1. **Mở** `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/UserQuotationSettingsService.cs`

2. **Thêm method `UploadHandoverTemplateAsync`**:

   ```csharp
   public async Task<UserQuotationSettingsDto> UploadHandoverTemplateAsync(
       UploadedFile file,
       QuotationTemplateType type,
       CancellationToken ct = default)
   {
       var userId = _currentUser.UserId
           ?? throw new UnauthorizedAccessException("User not authenticated.");

       TemplateUploadValidator.Validate(file, _uploadOptions.CurrentValue);

       var dir = _pathResolver.GetUserTemplatesDirectory();
       var (fileName, fieldUpdater) = type switch
       {
           QuotationTemplateType.HandoverWithPrice =>
               ($"{userId}_handover_with_price.xlsx",
               (Action<UserQuotationSettings, string, DateTimeOffset>)((s, fn, at) =>
               {
                   s.HandoverWithPriceTemplateFileName = fn;
                   s.HandoverWithPriceTemplateOriginalName = Path.GetFileName(file.FileName);
                   s.HandoverWithPriceTemplateUploadedAt = at;
               })),
           QuotationTemplateType.HandoverNoPrice =>
               ($"{userId}_handover_no_price.xlsx",
               (Action<UserQuotationSettings, string, DateTimeOffset>)((s, fn, at) =>
               {
                   s.HandoverNoPriceTemplateFileName = fn;
                   s.HandoverNoPriceTemplateOriginalName = Path.GetFileName(file.FileName);
                   s.HandoverNoPriceTemplateUploadedAt = at;
               })),
           _ => throw new ArgumentOutOfRangeException(nameof(type)),
       };

       var finalPath = Path.Combine(dir, fileName);
       var tempPath = finalPath + ".tmp";

       using (var source = file.OpenReadStream())
       using (var dest = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
       {
           await source.CopyToAsync(dest, ct);
       }
       if (File.Exists(finalPath)) File.Delete(finalPath);
       File.Move(tempPath, finalPath);

       var settings = await EnsureSettingsAsync(userId, ct);
       fieldUpdater(settings, fileName, _clock.UtcNow);
       await _db.SaveChangesAsync(ct);

       return await ToDtoAsync(settings, ct);
   }
   ```

3. **Thêm method `DeleteHandoverTemplateAsync`**:

   ```csharp
   public async Task<UserQuotationSettingsDto> DeleteHandoverTemplateAsync(
       QuotationTemplateType type,
       CancellationToken ct = default)
   {
       var userId = _currentUser.UserId
           ?? throw new UnauthorizedAccessException("User not authenticated.");

       var settings = await EnsureSettingsAsync(userId, ct);
       var dir = _pathResolver.GetUserTemplatesDirectory();

       var (fileName, fieldClearer) = type switch
       {
           QuotationTemplateType.HandoverWithPrice =>
               (settings.HandoverWithPriceTemplateFileName,
               (Action<UserQuotationSettings>)(s =>
               {
                   s.HandoverWithPriceTemplateFileName = null;
                   s.HandoverWithPriceTemplateOriginalName = null;
                   s.HandoverWithPriceTemplateUploadedAt = null;
               })),
           QuotationTemplateType.HandoverNoPrice =>
               (settings.HandoverNoPriceTemplateFileName,
               s =>
               {
                   s.HandoverNoPriceTemplateFileName = null;
                   s.HandoverNoPriceTemplateOriginalName = null;
                   s.HandoverNoPriceTemplateUploadedAt = null;
               }),
           _ => throw new ArgumentOutOfRangeException(nameof(type)),
       };

       if (!string.IsNullOrWhiteSpace(fileName))
       {
           var filePath = Path.Combine(dir, fileName);
           if (File.Exists(filePath)) File.Delete(filePath);
       }
       fieldClearer(settings);
       await _db.SaveChangesAsync(ct);

       return await ToDtoAsync(settings, ct);
   }
   ```

4. **Thêm method `GetCurrentUserHandoverTemplateStreamAsync`**:

   ```csharp
   public async Task<(Stream Stream, string FileName)?> GetCurrentUserHandoverTemplateStreamAsync(
       QuotationTemplateType type,
       CancellationToken ct = default)
   {
       var userId = _currentUser.UserId
           ?? throw new UnauthorizedAccessException("User not authenticated.");

       var settings = await _db.UserQuotationSettings
           .AsNoTracking()
           .Where(s => s.UserId == userId)
           .Select(s => new
           {
               s.HandoverWithPriceTemplateFileName,
               s.HandoverWithPriceTemplateOriginalName,
               s.HandoverNoPriceTemplateFileName,
               s.HandoverNoPriceTemplateOriginalName,
           })
           .FirstOrDefaultAsync(ct);

       var (fileName, originalName) = type switch
       {
           QuotationTemplateType.HandoverWithPrice =>
               (settings?.HandoverWithPriceTemplateFileName,
                settings?.HandoverWithPriceTemplateOriginalName),
           QuotationTemplateType.HandoverNoPrice =>
               (settings?.HandoverNoPriceTemplateFileName,
                settings?.HandoverNoPriceTemplateOriginalName),
           _ => throw new ArgumentOutOfRangeException(nameof(type)),
       };

       if (string.IsNullOrWhiteSpace(fileName)) return null;

       var dir = _pathResolver.GetUserTemplatesDirectory();
       var path = Path.Combine(dir, fileName);
       if (!File.Exists(path)) return null;

       var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
       return (stream, originalName ?? fileName);
   }
   ```

5. **Mở rộng `ToDtoAsync`** để map 6 field mới:

   Trong method `ToDtoAsync`, thêm sau `TemplateUploadedAt = settings.TemplateUploadedAt`:
   ```csharp
   HandoverWithPriceTemplateFileName = settings.HandoverWithPriceTemplateFileName,
   HandoverWithPriceTemplateOriginalName = settings.HandoverWithPriceTemplateOriginalName,
   HandoverWithPriceTemplateUploadedAt = settings.HandoverWithPriceTemplateUploadedAt,
   HandoverNoPriceTemplateFileName = settings.HandoverNoPriceTemplateFileName,
   HandoverNoPriceTemplateOriginalName = settings.HandoverNoPriceTemplateOriginalName,
   HandoverNoPriceTemplateUploadedAt = settings.HandoverNoPriceTemplateUploadedAt,
   ```

6. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors.

7. **Commit:**
   ```
   git commit -m "feat: extend settings service with handover template upload/delete/download"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj` → 0 errors
- `UserQuotationSettingsDto` có 6 field mới
- Interface có 3 method mới
- `UserQuotationSettingsService` implement đầy đủ, không có `NotImplementedException`

## Exit Criteria

- `ToDtoAsync` map cả 9 template fields (3 cũ + 6 mới)
- 3 method handover hoạt động đúng logic: save file với naming `{userId}_handover_with_price.xlsx` hoặc `_no_price.xlsx`
