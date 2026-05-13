# Phase 06 — PDF rendering with QuestPDF

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Render Báo giá as PDF using QuestPDF on the server. One customer-facing template that does NOT show cost/profit. Embed Roboto so Vietnamese diacritics render consistently across environments. Expose `GET /api/quotations/{id}/pdf` returning `application/pdf`.

## Files
- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` (add `<PackageReference Include="QuestPDF" Version="2024.12.x" />` — pin to the latest 2024.12 minor available in NuGet cache)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationPdfRenderer.cs` (new — port; the renderer is infrastructure, the port lives in Application)
- `backend/src/OrderMgmt.Infrastructure/Pdf/QuotationPdfRenderer.cs` (new — implementation)
- `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/Roboto-Regular.ttf` (embedded resource)
- `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/Roboto-Bold.ttf` (embedded resource)
- `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/Roboto-Italic.ttf` (embedded resource)
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` (register `IQuotationPdfRenderer`, configure QuestPDF license + fonts)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` (add `Task<(byte[] Pdf, string FileName)> RenderPdfAsync(Guid id, CancellationToken ct = default);`)
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` (implement `RenderPdfAsync` — load `QuotationDto`, call renderer)
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` (add `Pdf` action)

## Tasks
1. **License acknowledgement**: in `Infrastructure/DependencyInjection.cs` set
   ```csharp
   QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
   ```
   Confirm with the user (out of band) that the org qualifies (< $1M revenue). The assumption is recorded in `SUMMARY.md`.
2. **Font registration** — add the three Roboto TTF files as embedded resources:
   ```xml
   <ItemGroup>
     <EmbeddedResource Include="Pdf\Fonts\Roboto-*.ttf" />
   </ItemGroup>
   ```
   At startup register: `FontManager.RegisterFontFromEmbeddedResource("OrderMgmt.Infrastructure.Pdf.Fonts.Roboto-Regular.ttf");` (and the two variants).
3. **Port** — `IQuotationPdfRenderer`:
   ```csharp
   public interface IQuotationPdfRenderer
   {
       byte[] Render(QuotationDto quotation);
   }
   ```
4. **Renderer** — `QuotationPdfRenderer` implements the QuestPDF layout. Sections (top to bottom):
   - **Header**: company name, address, MST, phone, email (read from configuration `CompanyInfo` section — fall back to hard-coded values for MVP; full config screen is out of scope).
   - **Title**: `BẢNG BÁO GIÁ HÀNG HÓA`, centered, bold, size 18.
   - **Meta**: số báo giá + ngày báo giá (right-aligned).
   - **Customer block**: tên đơn vị, MST, địa chỉ, người liên hệ, SĐT.
   - **Delivery block**: địa chỉ giao, người nhận, SĐT, ngày giao (omit lines that are empty).
   - **Line items table** with columns: `STT | Mã | Tên hàng / quy cách | ĐVT | SL | Đơn giá | Thành tiền`. Specification rendered as second line under name. No cost/profit columns.
   - **Totals box** (right-aligned): Cộng tiền hàng, Chiết khấu (if > 0), Cước vận chuyển (if > 0), Thuế GTGT (rate + amount), **Tổng cộng** (bold, larger).
   - **Footer**: ghi chú `InternalNote` is NOT printed (it's internal); two signature columns "Bên mua" / "Bên bán" with name + date placeholders.
   - **Currency formatting**: `value.ToString("#,##0", new CultureInfo("vi-VN"))`.
   - **Date formatting**: `$"Hà Nội, ngày {d.Day:D2} tháng {d.Month:D2} năm {d.Year}"` for the meta date string.
5. **DI registration** — `services.AddScoped<IQuotationPdfRenderer, QuotationPdfRenderer>();` in `Infrastructure.DependencyInjection.AddInfrastructure`.
6. **Service method** — `QuotationService.RenderPdfAsync`:
   ```csharp
   public async Task<(byte[] Pdf, string FileName)> RenderPdfAsync(Guid id, CancellationToken ct = default)
   {
       var dto = await GetAsync(id, ct); // throws NotFound if missing
       var bytes = _pdfRenderer.Render(dto);
       var fileName = $"BaoGia_{dto.Code}.pdf";
       return (bytes, fileName);
   }
   ```
   Inject `IQuotationPdfRenderer` into the service constructor.
7. **Controller endpoint**:
   ```csharp
   [HttpGet("{id:guid}/pdf")]
   [HasPermission(Permissions.Quotations.Print)]
   public async Task<IActionResult> Pdf(Guid id, CancellationToken ct)
   {
       var (bytes, fileName) = await _quotations.RenderPdfAsync(id, ct);
       return File(bytes, "application/pdf", fileName);
   }
   ```
   Note: this endpoint returns raw bytes, not `ApiResponse<T>`. That's intentional — browsers cannot consume the wrapped JSON. Document this exception in code with a one-line comment.

## Verification
```
# Restore + build to confirm package resolved
dotnet restore backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj
dotnet build backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj -nologo --verbosity minimal
dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -nologo --verbosity minimal
```

Manual (after WebApi restart):
```
curl -H "Authorization: Bearer <token>" -o test.pdf "http://localhost:5050/api/quotations/<id>/pdf"
# Open test.pdf — verify Vietnamese diacritics render correctly, currency `#,##0`, no cost column.
```

## Exit Criteria
- Infrastructure project builds clean with the new package.
- Embedded TTFs resolve at runtime (no `FontFamilyNotInstalledException`).
- PDF for a sample quotation downloads, opens, and shows correct Vietnamese, correct currency formatting, no internal cost columns.
- `IQuotationPdfRenderer` is the only place QuestPDF is referenced — Application has no transitive QuestPDF dependency.
