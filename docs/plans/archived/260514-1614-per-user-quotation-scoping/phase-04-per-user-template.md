# Phase 04 — Per-user template + render fallback

**Status:** [x] complete
**Complexity:** L

## Objective

User upload/replace/delete file Excel làm template báo giá của mình. Validation security đầy đủ (MIME, magic bytes, ClosedXML parse, size cap, deny .xlsm/.xls, zip-bomb cap). Render Excel sử dụng template của **owner** báo giá (không phải user đang in); fallback về template hệ thống khi owner chưa upload.

## Files

- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportOptions.cs` (modify — thêm `UserTemplatesPath`, `UploadMaxBytes`, `AllowedMimeTypes`, `UnzippedMaxBytes`)
- `backend/src/OrderMgmt.WebApi/appsettings.json` + `.Development.json` (modify — thêm config)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/UserQuotationSettingsService.cs` (modify — `UploadTemplateAsync`, `DeleteTemplateAsync`, `GetTemplateStreamAsync`)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Interfaces/IUserQuotationSettingsService.cs` (modify)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/UploadedFile.cs` (new — edge-safe upload abstraction)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Models/TemplateUploadOptions.cs` (new — Application-level validation options)
- `backend/src/OrderMgmt.Application/Identity/UserSettings/Services/TemplateUploadValidator.cs` (new — security checks, no ASP.NET reference)
- `backend/src/OrderMgmt.Application/Common/Exceptions/ValidationException.cs` (verify existing or reuse `DomainException("VALIDATION", ...)`)
- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExcelRenderer.cs` (modify — chấp nhận `templatePath` override)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationExportPathResolver.cs` (new)
- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExportPathResolver.cs` (new)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationExcelRenderer.cs` (modify — thêm overload `RenderAsync(QuotationDto, string templatePath, ...)`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (modify — resolve template path theo owner)
- `backend/src/OrderMgmt.WebApi/Controllers/MeQuotationSettingsController.cs` (modify — 3 endpoint template)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationTemplateTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/TestData/valid-template.xlsx` (test fixture — copy từ `backend/src/OrderMgmt.WebApi/templates/template_baogia.xlsx`)
- `backend/tests/OrderMgmt.IntegrationTests/TestData/zip-bomb.xlsx` (test fixture — script tạo)

## Tasks

### Config

1. Mở rộng `QuotationExportOptions`:
   ```csharp
   public string UserTemplatesPath { get; set; } = "templates/users";
   public long UploadMaxBytes { get; set; } = 5 * 1024 * 1024;       // 5 MB compressed
   public long UnzippedMaxBytes { get; set; } = 50 * 1024 * 1024;    // 50 MB total unzipped (zip-bomb cap)
   public string[] AllowedMimeTypes { get; set; } =
       { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
   ```
2. Cập nhật cả 2 `appsettings.json` và `.Development.json` để thêm 4 trường mới.

### Template upload validator (security)

3. Tạo `UploadedFile` + `TemplateUploadOptions` trong Application để không reference `Microsoft.AspNetCore.Http` hoặc Infrastructure options:
   ```csharp
   public sealed class UploadedFile
   {
       private readonly Func<Stream> _openReadStream;
       public UploadedFile(string fileName, string contentType, long length, Func<Stream> openReadStream)
       {
           FileName = fileName;
           ContentType = contentType;
           Length = length;
           _openReadStream = openReadStream;
       }
       public string FileName { get; }
       public string ContentType { get; }
       public long Length { get; }
       public Stream OpenReadStream() => _openReadStream();
   }

   public sealed class TemplateUploadOptions
   {
       public long UploadMaxBytes { get; set; } = 5 * 1024 * 1024;
       public long UnzippedMaxBytes { get; set; } = 50 * 1024 * 1024;
       public string[] AllowedMimeTypes { get; set; } =
           { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
   }
   ```
4. Tạo `TemplateUploadValidator` static class:
   ```csharp
   public static class TemplateUploadValidator
   {
       private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };

       public static void Validate(UploadedFile file, TemplateUploadOptions options)
       {
           if (file.Length == 0)
               throw new DomainException("VALIDATION", "File rỗng.");
           if (file.Length > options.UploadMaxBytes)
               throw new DomainException("VALIDATION", $"File vượt quá {options.UploadMaxBytes / (1024 * 1024)} MB.");

           var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
           if (ext != ".xlsx")
               throw new DomainException("VALIDATION", "Chỉ chấp nhận file .xlsx.");

           if (!options.AllowedMimeTypes.Contains(file.ContentType))
               throw new DomainException("VALIDATION", $"MIME không hợp lệ: {file.ContentType}.");

           using var s = file.OpenReadStream();
           Span<byte> head = stackalloc byte[4];
           if (s.Read(head) != 4 || !head.SequenceEqual(ZipMagic))
               throw new DomainException("VALIDATION", "File không phải định dạng .xlsx hợp lệ (magic bytes).");

           // Zip-bomb cap: stream qua ZipArchive, đếm uncompressed size.
           s.Position = 0;
           using var zip = new ZipArchive(s, ZipArchiveMode.Read, leaveOpen: true);
           long totalUnzipped = zip.Entries.Sum(e => e.Length);
           if (totalUnzipped > options.UnzippedMaxBytes)
               throw new DomainException("VALIDATION", $"File giải nén vượt {options.UnzippedMaxBytes / (1024 * 1024)} MB.");

           // Parse thử bằng ClosedXML để chắc chắn không corrupt / không phải file giả.
           s.Position = 0;
           try
           {
               using var wb = new ClosedXML.Excel.XLWorkbook(s);
               _ = wb.Worksheets.Count();
           }
           catch (Exception ex)
           {
               throw new DomainException("VALIDATION", $"Không mở được file Excel: {ex.Message}");
           }
       }
   }
   ```

### Service methods

5. Mở rộng `IUserQuotationSettingsService`:
   ```csharp
   Task<UserQuotationSettingsDto> UploadTemplateAsync(UploadedFile file, CancellationToken ct);
   Task<UserQuotationSettingsDto> DeleteTemplateAsync(CancellationToken ct);
   Task<(Stream Stream, string FileName)?> GetCurrentUserTemplateStreamAsync(CancellationToken ct);
   ```
   - **Decision bắt buộc**: dùng `UploadedFile`; Application không reference `IFormFile`.
6. Implement `UploadTemplateAsync`:
   - Validate qua `TemplateUploadValidator`.
   - Tính đường dẫn `{userTemplatesPath}/{currentUserId}.xlsx`. `Directory.CreateDirectory(userTemplatesPath)`.
   - Stream file ra disk (atomic: ghi `.tmp` → rename).
   - Update record: `TemplateFileName = "{userId}.xlsx"`, `TemplateOriginalName = file.FileName`, `TemplateUploadedAt = clock.UtcNow`. SaveChanges. Return DTO.
7. Implement `DeleteTemplateAsync`:
   - Nếu file tồn tại → xoá.
   - Set 3 trường = null. SaveChanges.
8. Implement `GetCurrentUserTemplateStreamAsync`:
   - Đọc settings; nếu `TemplateFileName == null` → return null (controller trả 404).
   - Mở `FileStream` (FileShare.Read).

### Template resolution trong render

9. Cập nhật `IQuotationExcelRenderer` thêm overload:
   ```csharp
   Task<byte[]> RenderAsync(QuotationDto dto, string templatePath, CancellationToken ct = default);
   ```
   (Giữ overload cũ delegate sang overload mới với `options.TemplatePath`.)
10. Sửa `QuotationExcelRenderer` để chấp nhận `templatePath` argument (read file từ path đó thay vì hard-code `options.TemplatePath`).
11. Trong `QuotationService.RenderExcelAsync`:
    ```csharp
    var templatePath = await _templatePathResolver.ResolveTemplatePathAsync(quotation.OwnerUserId, ct);
    var bytes = await _excelRenderer.RenderAsync(dto, templatePath, ct);
    ```
    Không inject `IOptions<QuotationExportOptions>` trực tiếp vào `QuotationService`. Tạo port `IQuotationExportPathResolver` trong Application với method `ResolveTemplatePathAsync(Guid ownerUserId, CancellationToken ct)`; Infrastructure implementation đọc `QuotationExportOptions`, `UserQuotationSettings`, check file tồn tại và fallback.
12. Áp dụng cùng logic cho `RenderPdfAsync` (gọi qua `RenderExcelAsync` trước rồi convert).

### Controller endpoints

13. Trong `MeQuotationSettingsController` thêm:
    ```csharp
    [HttpPut("template")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]  // 1 MB headroom over UploadMaxBytes
    public async Task<...> UploadTemplate(IFormFile file, IUserQuotationSettingsService svc, CancellationToken ct)
    {
        var uploaded = new UploadedFile(file.FileName, file.ContentType, file.Length, () => file.OpenReadStream());
        return Success(await svc.UploadTemplateAsync(uploaded, ct));
    }

    [HttpDelete("template")]
    public async Task<...> DeleteTemplate(IUserQuotationSettingsService svc, CancellationToken ct)
        => Success(await svc.DeleteTemplateAsync(ct));

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(IUserQuotationSettingsService svc, CancellationToken ct)
    {
        var result = await svc.GetCurrentUserTemplateStreamAsync(ct);
        if (result is null) return NotFound();
        return File(result.Value.Stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.Value.FileName);
    }
    ```

### Tests

14. Tạo `QuotationTemplateTests`:
    - **Upload thành công**: PUT multipart valid xlsx → 200; file tồn tại trên disk; `GET /me/quotation-settings` trả `TemplateFileName` không null.
    - **Render dùng template user**: SALES1 upload template A, tạo Q, render Excel → file content khác render với template hệ thống (so sánh worksheet name hoặc 1 cell đặc trưng).
    - **Render fallback khi chưa upload**: SALES2 (chưa upload) tạo Q → render dùng template hệ thống.
    - **Render dùng template của owner hiện tại**: SALES1 upload, transfer Q cho SALES2 (chưa upload). ADMIN render Q → dùng template SALES2 nếu có, nếu SALES2 chưa upload thì fallback template hệ thống.
    - **Delete template**: DELETE → file biến mất, response settings.TemplateFileName=null. Render fallback hệ thống.
    - **Format sai**: upload .txt rename .xlsx → 400 VALIDATION.
    - **Magic bytes sai**: tạo file 4 byte không phải PK\x03\x04 đổi tên .xlsx → 400.
    - **MIME sai**: upload với Content-Type `text/plain` → 400.
    - **Quá size**: file > 5MB → 400 hoặc 413.
    - **`.xlsm` bị từ chối**: 400.

## Verification

```powershell
dotnet build backend/OrderMgmt.sln -c Debug
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~QuotationTemplate"
```

Verify disk:
```powershell
ls backend/src/OrderMgmt.WebApi/templates/users/
```

## Exit Criteria

- 3 endpoint `/api/me/quotation-settings/template` hoạt động (PUT/DELETE/GET).
- 10 test security/integration template pass.
- Render Excel cho báo giá có owner đã upload → dùng đúng template owner.
- Render fallback đúng khi owner chưa upload hoặc file thiếu.
- Validation reject toàn bộ vector tấn công liệt kê (magic bytes, MIME, size, .xlsm, parse fail, zip-bomb).
