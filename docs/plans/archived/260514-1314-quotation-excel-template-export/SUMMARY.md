# Implementation Plan: Quotation Excel Template Export

> Created: 2026-05-14 13:14:09

## Objective

- Implement backend export for saved quotations using the existing Excel template at `backend/src/OrderMgmt.WebApi/templates/template_baogia.xlsx`.
- Add Excel download beside the existing print/PDF flow in `frontend/src/pages/quotations/quotation-form-page.tsx`.
- Replace the current quotation PDF endpoint implementation so `/api/quotations/{id}/pdf` renders from the filled Excel template via LibreOffice headless.

## Scope

### In scope

- Add ClosedXML-based XLSX rendering that preserves workbook styling, logo, merged cells, print settings, and formulas outside filled line cells.
- Add LibreOffice headless XLSX-to-PDF conversion with executable path configured through appsettings/environment.
- Add `GET /api/quotations/{id}/excel` (permission: `Permissions.Quotations.Print`).
- Keep existing `GET /api/quotations/{id}/pdf` route, permission (`Permissions.Quotations.Print`), and frontend semantics, but change its backend implementation to Excel-template-based PDF.
- Fill quotation header cells and dynamic line rows according to the confirmed template mapping.
- Remove the existing `IQuotationPdfRenderer` interface, `QuotationPdfRenderer` implementation, QuestPDF package, and embedded fonts — replaced entirely by the Excel-template flow.
- Add focused backend tests for endpoint behavior and renderer logic where practical.
- Update frontend API and form page to show an Excel download action beside PDF/print. Both `downloadPdf` and `downloadExcel` return a `Blob`; callers construct the filename as `BaoGia_{code}.pdf` / `BaoGia_{code}.xlsx` — consistent with the current PDF pattern.

### Out of scope

- Editing the Excel template content manually.
- Adding database tables, migrations, or new quotation fields.
- Making the lower template sections dynamic (`Lưu ý`, `Thanh toán`, bank info, signatures remain as-is).
- Adding a UI to configure LibreOffice path or template fields.
- Supporting export for unsaved quotation form state.

## Architecture & Approach

- Application layer defines export ports and service methods; Infrastructure implements file generation.
- Replace the current QuestPDF quotation path with a template-first flow:
  - `QuotationService.RenderExcelAsync(id)` loads the saved quotation DTO and asks an Excel renderer for bytes.
  - `QuotationService.RenderPdfAsync(id)` gets the same Excel bytes and sends them to a LibreOffice converter.
  - WebApi returns raw file bytes with correct MIME types, not `ApiResponse`, matching the current binary PDF behavior.
- ClosedXML is used to preserve workbook structure while inserting/deleting rows and copying row styles.
- LibreOffice is an operational dependency, configured by path. Backend should fail with a clear 500-level configuration error if missing.
- The WebApi template file must be copied to output/publish so runtime path resolution is deterministic.

Confirmed template mapping:

- `A8`: full quotation number string, formatted as `"Số: {Code}"` to preserve the cell's label prefix.
- `C9`: formatted quotation date, e.g. `"Hà nội, ngày 14 tháng 05 năm 2026"`.
- `B10`: full buyer string, formatted as `"Đơn vị mua hàng: {CustomerName}"`.
- `B11`: full delivery address string, formatted as `"Địa chỉ giao hàng: {DeliveryAddress}"`.
- `B12`: full delivery contact string, formatted as `"Điện thoại người nhận: {DeliveryPhone} - {DeliveryRecipient}"` (omit missing parts gracefully).
- `B13`: full product list string, formatted as `"Hàng hóa cung cấp: {distinct ProductName values joined by ", "}"`, excluding shipping lines (see detection rule below). Set `WrapText = true` on this cell to handle many products without visual overflow.
- `A14:G14`: existing header from template, unchanged.
- `A15:G...`: dynamic item rows.
- Item row values:
  - `A`: 1-based item number.
  - `B`: `KT: {Length}*{Width}*{Thickness}mm` when dimensions exist; otherwise fallback to specification or product name.
  - `C`: density.
  - `D`: quantity.
  - `E`: sheet count.
  - `F`: unit price.
  - `G`: line total from database/DTO.
- Delete sample data rows (rows 15 and 16 in the original template) before filling real data.
- Insert additional rows when real line count exceeds the sample row count.
- Copy style from sample data row(s) to inserted rows.
- Do not keep formulas in line columns `D`, `F`, `G`; fill backend values directly.
- Summary row (the row whose `B` cell = "Cộng tiền hàng", originally row 17): keep it in place; after all row insert/delete operations, update both `D` and `G` formula ranges to span the actual first and last item row, e.g. `SUM(D15:D{lastRow})` and `SUM(G15:G{lastRow})`. The template has NO separate rows for Tax, Discount, or Total — only this one summary row exists.
- `Freight` is not rendered as a separate line. If users create a shipping product line, it appears in the table but is excluded from `B13`.
- Shipping detection for `B13` exclusion: a line is treated as shipping when its `ProductName` (case-insensitive, accent-insensitive) contains `"vận chuyển"` or `"van chuyen"`. Group code / group name are not available in `QuotationLineDto` and must not be used; name-based matching is sufficient given the naming convention for shipping products.

## Phases

- [x] **Phase 1 [M]: Export Contracts, Config, Packages** - Add application interfaces/service methods; remove `IQuotationPdfRenderer`, `QuotationPdfRenderer`, and QuestPDF dependency; add ClosedXML package reference, runtime config, and template copy behavior.
- [x] **Phase 2 [L]: ClosedXML Excel Renderer** - Implement dynamic template fill, row/style handling, formulas, and shipping exclusion.
- [x] **Phase 3 [M]: LibreOffice PDF Conversion And API Wiring** - Implement headless conversion, replace PDF route behavior, add Excel route.
- [x] **Phase 4 [M]: Frontend Integration And Verification** - Add Excel download UI/API and run focused backend/frontend validation.

## Key Changes

**Added**
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationExcelRenderer.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationSpreadsheetPdfConverter.cs`
- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExcelRenderer.cs`
- `backend/src/OrderMgmt.Infrastructure/Excel/LibreOfficeSpreadsheetPdfConverter.cs`
- Backend integration tests under `backend/tests/OrderMgmt.IntegrationTests/Quotations/`

**Modified**
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj`
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`
- `backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj`
- `backend/src/OrderMgmt.WebApi/appsettings.json`
- `backend/src/OrderMgmt.WebApi/appsettings.Development.json`
- `frontend/src/features/quotations/api.ts`
- `frontend/src/pages/quotations/quotation-form-page.tsx`

**Deleted**
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationPdfRenderer.cs`
- `backend/src/OrderMgmt.Infrastructure/Pdf/QuotationPdfRenderer.cs` (and embedded font resources)

## Verification Strategy

- Backend build:
  - `dotnet build backend/OrderMgmt.sln`
- Backend tests:
  - `dotnet test backend/OrderMgmt.sln`
- Frontend checks:
  - Inspect `frontend/package.json` scripts first.
  - Run the existing typecheck/test command if present.
- Manual API checks after app startup:
  - `GET /api/quotations/{id}/excel` returns `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
  - `GET /api/quotations/{id}/pdf` returns `application/pdf`.
  - Downloaded Excel opens with expected template layout and dynamic rows.
  - Downloaded PDF visually matches the filled Excel template.
- Integration test setup:
  - `WebApplicationFactory` must add `QuotationExport:TemplatePath` pointing to the template file (absolute path or relative to test output directory).
  - Excel renderer tests use the real template file; PDF converter tests use a `FakeSpreadsheetPdfConverter` stub that returns fixed bytes, so LibreOffice is not required in CI/dev.

## Dependencies

- `ClosedXML` in `OrderMgmt.Infrastructure`: required to fill and preserve Excel workbook content.
- LibreOffice installed on runtime host: required for XLSX to PDF conversion.
- Config keys:
  - `QuotationExport:TemplatePath`
  - `QuotationExport:LibreOfficePath`
  - Optional `QuotationExport:ConversionTimeoutSeconds`

## Risks & Mitigations

- LibreOffice not installed or path invalid -> validate config at conversion time and throw a clear infrastructure error.
- Concurrent PDF conversions writing to same path -> use unique temp directories per request and clean them in `finally`.
- Formula ranges broken after row insert/delete -> explicitly rewrite summary formulas after row operations.
- Template file missing in publish output -> include it as `Content` with `CopyToOutputDirectory` and `CopyToPublishDirectory`.
- ClosedXML modifies workbook drawing/page settings unexpectedly -> keep edits scoped to cells/rows and verify by opening output.
- Shipping detection misses variants -> use accent-insensitive text match on `ProductName` only (group data is not stored in `QuotationLineDto`); name convention for shipping products is "Cước vận chuyển" which is reliably caught by this rule.
- Integration tests cannot rely on LibreOffice in CI/dev -> test Excel endpoint normally; isolate converter behind interface and use fake converter in WebApplicationFactory tests where needed.

## Open Questions

- None. LibreOffice is confirmed to be installed/configured by the runtime environment.
