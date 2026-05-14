# Phase 01: Export Contracts, Config, Packages

## Objective

- Prepare the backend architecture for Excel-template export without changing runtime behavior yet.

## Preconditions

- Requirements in `SUMMARY.md` are accepted.
- Template file exists at `backend/src/OrderMgmt.WebApi/templates/template_baogia.xlsx`.

## Tasks

1. Inspect current quotation export wiring:
   - `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs`
   - `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs`
   - `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs`
   - `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs`
2. Add `ClosedXML` package to `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj`.
3. Add application interfaces:
   - `IQuotationExcelRenderer` returning XLSX bytes from a quotation DTO and shipping product metadata.
   - `IQuotationSpreadsheetPdfConverter` converting XLSX bytes into PDF bytes.
4. Extend `IQuotationService`:
   - Add `Task<(byte[] Excel, string FileName)> RenderExcelAsync(Guid id, CancellationToken ct = default);`
   - Keep existing `RenderPdfAsync` signature so the controller route can stay stable.
5. Add export options class in Infrastructure or Application-adjacent location, with:
   - `TemplatePath`
   - `LibreOfficePath`
   - `ConversionTimeoutSeconds`
6. Register options in `DependencyInjection`.
7. Update `OrderMgmt.WebApi.csproj` to copy `templates/template_baogia.xlsx` to output and publish.
8. Add appsettings defaults:
   - `QuotationExport:TemplatePath` defaulting to `templates/template_baogia.xlsx`.
   - `QuotationExport:LibreOfficePath` blank or development-local placeholder.
   - `QuotationExport:ConversionTimeoutSeconds` defaulting to a conservative value such as `60`.

## Verification

- Commands:
  - `dotnet restore backend/OrderMgmt.sln`
  - `dotnet build backend/OrderMgmt.sln`
- Expected results:
  - Restore succeeds and downloads ClosedXML.
  - Build succeeds.
  - No controller behavior changes are required in this phase.

## Exit Criteria

- ClosedXML package is referenced only by Infrastructure.
- Application layer exposes export interfaces without depending on ClosedXML or LibreOffice types.
- Template is configured to copy to runtime/publish output.
- Existing PDF endpoint still compiles.
