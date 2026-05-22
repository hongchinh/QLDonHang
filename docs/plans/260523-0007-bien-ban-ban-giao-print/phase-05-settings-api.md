# Phase 05 — Settings API

**Status:** [ ] pending
**Complexity:** S

## Objective

Mở rộng `MeQuotationSettingsController` để hỗ trợ `?type=` query param cho upload/delete/download template của 3 loại tài liệu.

## Files

- `backend/src/OrderMgmt.WebApi/Controllers/MeQuotationSettingsController.cs`

## Tasks

### Task 5.1 — Thêm `?type=` query param vào các endpoints template

1. **Mở** `backend/src/OrderMgmt.WebApi/Controllers/MeQuotationSettingsController.cs`

2. **Thêm helper method private** để parse `type` string thành `QuotationTemplateType`:

   ```csharp
   private static QuotationTemplateType ParseTemplateType(string? type) => type?.ToLowerInvariant() switch
   {
       null or "" or "quotation" => QuotationTemplateType.Quotation,
       "handover-with-price" => QuotationTemplateType.HandoverWithPrice,
       "handover-no-price" => QuotationTemplateType.HandoverNoPrice,
       _ => throw new BadHttpRequestException($"Unknown template type: '{type}'"),
   };
   ```

   > **Lưu ý:** Dùng `BadHttpRequestException` (không phải `ArgumentException`) để đảm bảo ASP.NET Core trả 400 mà không cần phụ thuộc vào cách global error handler xử lý system exceptions.

3. **Sửa `UploadTemplate`** — thêm optional `[FromQuery] string? type = null`:

   ```csharp
   [HttpPut("template")]
   [Consumes("multipart/form-data")]
   [RequestSizeLimit(6 * 1024 * 1024)]
   [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
   public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> UploadTemplate(
       IFormFile file,
       [FromQuery] string? type,
       CancellationToken ct)
   {
       var uploaded = new UploadedFile(
           file.FileName,
           file.ContentType,
           file.Length,
           () => file.OpenReadStream());

       var templateType = ParseTemplateType(type);

       if (templateType == QuotationTemplateType.Quotation)
           return Success(await _service.UploadTemplateAsync(uploaded, ct));
       else
           return Success(await _service.UploadHandoverTemplateAsync(uploaded, templateType, ct));
   }
   ```

4. **Sửa `DeleteTemplate`** — thêm optional `[FromQuery] string? type = null`:

   ```csharp
   [HttpDelete("template")]
   [ProducesResponseType(typeof(ApiResponse<UserQuotationSettingsDto>), StatusCodes.Status200OK)]
   public async Task<ActionResult<ApiResponse<UserQuotationSettingsDto>>> DeleteTemplate(
       [FromQuery] string? type,
       CancellationToken ct)
   {
       var templateType = ParseTemplateType(type);

       if (templateType == QuotationTemplateType.Quotation)
           return Success(await _service.DeleteTemplateAsync(ct));
       else
           return Success(await _service.DeleteHandoverTemplateAsync(templateType, ct));
   }
   ```

5. **Sửa `DownloadTemplate`** — thêm optional `[FromQuery] string? type = null`:

   ```csharp
   [HttpGet("template")]
   public async Task<IActionResult> DownloadTemplate(
       [FromQuery] string? type,
       CancellationToken ct)
   {
       var templateType = ParseTemplateType(type);

       (Stream Stream, string FileName)? result;
       if (templateType == QuotationTemplateType.Quotation)
           result = await _service.GetCurrentUserTemplateStreamAsync(ct);
       else
           result = await _service.GetCurrentUserHandoverTemplateStreamAsync(templateType, ct);

       if (result is null) return NotFound();
       return File(
           result.Value.Stream,
           "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
           result.Value.FileName);
   }
   ```

6. **Thêm usings** nếu cần:
   ```csharp
   using OrderMgmt.Application.Sales.Quotations.Models;
   ```

7. **Build WebApi:**
   ```
   dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
   ```
   Expected: 0 errors.

8. **Commit:**
   ```
   git commit -m "feat: extend settings API with ?type= param for handover templates"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj` → 0 errors
- `PUT /api/me/quotation-settings/template` (không có ?type) vẫn hoạt động cho báo giá
- `PUT /api/me/quotation-settings/template?type=handover-with-price` route đúng vào `UploadHandoverTemplateAsync`

## Exit Criteria

- 3 endpoints template hiện tại giữ nguyên behavior khi không truyền `?type` (backward-compatible)
- `?type=handover-with-price` và `?type=handover-no-price` route đúng vào service methods mới
- Unknown type trả về `BadHttpRequestException` → 400 Bad Request
