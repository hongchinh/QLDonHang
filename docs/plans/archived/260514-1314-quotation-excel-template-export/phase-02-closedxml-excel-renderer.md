# Phase 02: ClosedXML Excel Renderer

## Objective

- Implement XLSX generation from `template_baogia.xlsx` with dynamic quotation data while preserving template formatting and formulas outside line value cells.

## Preconditions

- Phase 01 is complete.
- ClosedXML is available in Infrastructure.

## Tasks

1. Add `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExcelRenderer.cs`.
2. Resolve the template path:
   - If configured path is absolute, use it directly.
   - If relative, resolve relative to the app content base/current directory used by WebApi runtime.
   - Throw a clear exception when the template does not exist.
3. Open the workbook from a stream and select the first worksheet.
4. Fill header cells:
   - `A8`: `Số: {q.Code}` or exact agreed format based on existing template style.
   - `C9`: Vietnamese-style quotation date text matching template convention.
   - `B10`: `Đơn vị mua hàng: {q.CustomerName}`.
   - `B11`: `Địa chỉ giao hàng: {q.DeliveryAddress ?? q.CustomerAddress}`.
   - `B12`: combine `DeliveryPhone` and `DeliveryRecipient` using existing label text.
   - `B13`: `Hàng hóa cung cấp: {distinct non-shipping product names}`.
5. In `QuotationService`, before rendering Excel, identify shipping product IDs:
   - Gather `ProductId` values from quotation lines.
   - Query products with `ProductGroup`.
   - Shipping when group code equals `VC` or group name contains `vận chuyển` accent-insensitively.
6. In renderer, also exclude line names from `B13` if product name contains `vận chuyển` or `van chuyen`.
7. Determine the template line sample and total row:
   - Treat row `15` as the primary item style row.
   - Detect sample data rows before the `Cộng tiền hàng` row, currently row `17`.
   - Remove sample rows before filling actual data.
8. Insert item rows starting at row `15`:
   - Insert exactly `q.Lines.Count` rows if sample row removal leaves no item rows.
   - Copy style/height from sample row to each inserted data row.
   - Preserve row order by `SortOrder`.
9. Fill `A:G` values directly:
   - `A`: item number.
   - `B`: formatted dimensions/specification/product fallback.
   - `C`: density.
   - `D`: quantity.
   - `E`: sheet count.
   - `F`: unit price.
   - `G`: line total.
10. Clear formulas in item cells `D`, `F`, and `G` before setting backend values.
11. Move lower template sections down automatically through row operations.
12. Keep the `Cộng tiền hàng` row and update formulas:
   - `D{totalRow}` should sum `D{firstLine}:D{lastLine}` when there is at least one line.
   - `G{totalRow}` should sum `G{firstLine}:G{lastLine}` when there is at least one line.
   - If zero lines is impossible by validation, no special zero-line behavior is required.
13. Save workbook to a memory stream and return bytes.
14. Remove or stop registering `QuotationPdfRenderer` only after the PDF replacement phase, unless build requires earlier cleanup.

## Verification

- Commands:
  - `dotnet build backend/OrderMgmt.sln`
  - `dotnet test backend/OrderMgmt.sln --filter Quotations`
- Expected results:
  - Build succeeds.
  - Existing quotation tests still pass.
  - New focused tests can instantiate the renderer with a test quotation and verify:
    - Header cells are filled.
    - Sample row values are replaced.
    - Formula cells in total row point to actual item row range.
    - Item `G` cells contain values, not formulas.

## Exit Criteria

- XLSX bytes are generated from the template.
- Output workbook opens in Excel/LibreOffice with preserved visual layout.
- `B13` excludes shipping/transport lines using product group and text fallback rules.
- Line rows contain backend values only.
