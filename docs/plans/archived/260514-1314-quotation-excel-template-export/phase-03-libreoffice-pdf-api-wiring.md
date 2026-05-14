# Phase 03: LibreOffice PDF Conversion And API Wiring

## Objective

- Expose Excel download and replace the existing PDF implementation with LibreOffice conversion from the filled Excel template.

## Preconditions

- Phase 02 is complete.
- Runtime environment can provide `QuotationExport:LibreOfficePath`.

## Tasks

1. Add `backend/src/OrderMgmt.Infrastructure/Excel/LibreOfficeSpreadsheetPdfConverter.cs`.
2. Implement conversion flow:
   - Create a unique temp directory per conversion.
   - Write input bytes to `quotation.xlsx`.
   - Start LibreOffice with headless arguments similar to:
     - `--headless`
     - `--convert-to pdf`
     - `--outdir <tempDir>`
     - `<inputFile>`
   - Capture stdout/stderr.
   - Enforce `ConversionTimeoutSeconds`.
   - Read the produced PDF bytes.
   - Clean temp files/directories in `finally`.
3. Register:
   - `IQuotationExcelRenderer -> QuotationExcelRenderer`
   - `IQuotationSpreadsheetPdfConverter -> LibreOfficeSpreadsheetPdfConverter`
4. Update `QuotationService`:
   - Add `RenderExcelAsync`.
   - Change `RenderPdfAsync` to call `RenderExcelAsync`, then converter.
   - Return file names:
     - `BaoGia_{code}.xlsx`
     - `BaoGia_{code}.pdf`
5. Remove `IQuotationPdfRenderer`/QuestPDF usage from `QuotationService` constructor.
6. Decide cleanup:
   - If QuestPDF is no longer used anywhere, remove its DI registration and package reference.
   - If removal creates unrelated churn or package is used elsewhere, leave package cleanup to a separate small follow-up.
7. Update `QuotationsController`:
   - Add `GET {id:guid}/excel` with `Permissions.Quotations.Print`.
   - Return MIME type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.
   - Keep `GET {id:guid}/pdf` route and permission, but it now returns converted PDF.
8. Add or update tests:
   - Excel endpoint returns success, content type, and `.xlsx` filename.
   - PDF endpoint can be tested with a fake converter in test DI to avoid requiring LibreOffice.
   - Permissions for Excel follow the existing print permission behavior.

## Verification

- Commands:
  - `dotnet build backend/OrderMgmt.sln`
  - `dotnet test backend/OrderMgmt.sln --filter Quotations`
- Manual command/check if LibreOffice is installed locally:
  - Start the API.
  - Download `/api/quotations/{id}/excel`.
  - Download `/api/quotations/{id}/pdf`.
- Expected results:
  - Excel endpoint returns a valid `.xlsx`.
  - PDF endpoint returns `application/pdf` from the filled Excel template.
  - Existing PDF route path remains unchanged for frontend compatibility.

## Exit Criteria

- `/api/quotations/{id}/excel` works for authorized users.
- `/api/quotations/{id}/pdf` no longer uses QuestPDF for quotation rendering.
- LibreOffice errors produce actionable logs/exceptions.
- Temp files are cleaned up after success, failure, and timeout.
