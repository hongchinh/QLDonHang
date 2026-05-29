# Phase 06 — Quotation Service + Controller

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm 4 method render biên bản vào `IQuotationService` + `QuotationService`, và 4 endpoint mới vào `QuotationsController`.

## Files

- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`

## Tasks

### Task 6.1 — Mở rộng `IQuotationService`

1. **Mở** `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`

2. **Thêm 4 method** sau `RenderPdfAsync`:

   ```csharp
   Task<(byte[] Excel, string FileName)> RenderHandoverExcelAsync(Guid id, bool withPrice, CancellationToken ct = default);
   Task<(byte[] Pdf, string FileName)> RenderHandoverPdfAsync(Guid id, bool withPrice, CancellationToken ct = default);
   ```

   **Lưu ý:** 2 method với `bool withPrice` thay vì 4 riêng biệt để tránh trùng lặp trong interface. Controller sẽ pass đúng giá trị.

3. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors (có thể fail nếu QuotationService chưa implement — tiếp tục sang Task 6.2).

### Task 6.2 — Inject `IHandoverExcelRenderer` vào `QuotationService`

1. **Mở** `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`

2. **Thêm field private** và constructor parameter:

   Tìm dòng khai báo `_excelRenderer`:
   ```csharp
   private readonly IQuotationExcelRenderer _excelRenderer;
   ```
   
   Thêm bên dưới:
   ```csharp
   private readonly IHandoverExcelRenderer _handoverRenderer;
   ```

   Trong constructor, thêm parameter `IHandoverExcelRenderer handoverRenderer` và assignment:
   ```csharp
   _handoverRenderer = handoverRenderer;
   ```

3. **Implement 2 method** sau `RenderPdfAsync`:

   ```csharp
   public async Task<(byte[] Excel, string FileName)> RenderHandoverExcelAsync(
       Guid id, bool withPrice, CancellationToken ct = default)
   {
       var dto = await GetAsync(id, ct);
       var type = withPrice
           ? QuotationTemplateType.HandoverWithPrice
           : QuotationTemplateType.HandoverNoPrice;
       var templatePath = await _templatePathResolver.ResolveHandoverTemplatePathAsync(
           dto.OwnerUserId, type, ct);
       var bytes = await _handoverRenderer.RenderAsync(dto, templatePath, withPrice, ct);
       return (bytes, $"BBBG_{dto.Code}.xlsx");
   }

   public async Task<(byte[] Pdf, string FileName)> RenderHandoverPdfAsync(
       Guid id, bool withPrice, CancellationToken ct = default)
   {
       var dto = await GetAsync(id, ct);
       var type = withPrice
           ? QuotationTemplateType.HandoverWithPrice
           : QuotationTemplateType.HandoverNoPrice;
       var templatePath = await _templatePathResolver.ResolveHandoverTemplatePathAsync(
           dto.OwnerUserId, type, ct);
       var excelBytes = await _handoverRenderer.RenderAsync(dto, templatePath, withPrice, ct);
       var pdfBytes = await _pdfConverter.ConvertAsync(excelBytes, ct);
       return (pdfBytes, $"BBBG_{dto.Code}.pdf");
   }
   ```

4. **Thêm using** nếu cần: `using OrderMgmt.Application.Sales.Quotations.Models;`

5. **Build Application:**
   ```
   dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj
   ```
   Expected: 0 errors.

### Task 6.3 — Thêm 4 endpoint vào `QuotationsController`

1. **Mở** `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`

2. **Thêm 4 action** sau action `Pdf` hiện tại:

   ```csharp
   [HttpGet("{id:guid}/handover-with-price/excel")]
   [HasPermission(Permissions.Quotations.Print)]
   public async Task<IActionResult> HandoverWithPriceExcel(Guid id, CancellationToken ct)
   {
       var (bytes, fileName) = await _quotations.RenderHandoverExcelAsync(id, withPrice: true, ct);
       return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
   }

   [HttpGet("{id:guid}/handover-with-price/pdf")]
   [HasPermission(Permissions.Quotations.Print)]
   public async Task<IActionResult> HandoverWithPricePdf(Guid id, CancellationToken ct)
   {
       var (bytes, fileName) = await _quotations.RenderHandoverPdfAsync(id, withPrice: true, ct);
       return File(bytes, "application/pdf", fileName);
   }

   [HttpGet("{id:guid}/handover-no-price/excel")]
   [HasPermission(Permissions.Quotations.Print)]
   public async Task<IActionResult> HandoverNoPriceExcel(Guid id, CancellationToken ct)
   {
       var (bytes, fileName) = await _quotations.RenderHandoverExcelAsync(id, withPrice: false, ct);
       return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
   }

   [HttpGet("{id:guid}/handover-no-price/pdf")]
   [HasPermission(Permissions.Quotations.Print)]
   public async Task<IActionResult> HandoverNoPricePdf(Guid id, CancellationToken ct)
   {
       var (bytes, fileName) = await _quotations.RenderHandoverPdfAsync(id, withPrice: false, ct);
       return File(bytes, "application/pdf", fileName);
   }
   ```

3. **Build WebApi:**
   ```
   dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj
   ```
   Expected: 0 errors.

4. **Chạy test `HandoverExportTests`** — lần này các tests nên PASS (nếu renderer đã implement đúng):
   ```
   dotnet test backend/tests/OrderMgmt.IntegrationTests/ \
     --filter "HandoverExportTests" \
     -e TEST_DB_CONNECTION="<test-db-connection-string>"
   ```
   Expected: 4 tests PASS.

5. **Commit:**
   ```
   git commit -m "feat: add handover export endpoints to QuotationsController"
   ```

## Verification

- `dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj` → 0 errors
- Integration tests `HandoverExportTests` → 4/4 PASS
- Endpoint `/api/quotations/{id}/handover-with-price/excel` trả về 200 với content-type xlsx
- Endpoint `/api/quotations/{id}/handover-no-price/pdf` trả về 200 với content-type pdf

## Exit Criteria

- 4 endpoint mới hoạt động với permission `Quotations.Print`
- `IQuotationService` có 2 method mới
- Integration tests xanh
