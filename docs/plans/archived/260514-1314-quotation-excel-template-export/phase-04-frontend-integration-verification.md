# Phase 04: Frontend Integration And Verification

## Objective

- Add a user-facing Excel download action beside the current print/PDF action and verify the end-to-end export flow.

## Preconditions

- Phase 03 is complete.
- Backend endpoints are available.

## Tasks

1. Inspect frontend quotation API and form page:
   - `frontend/src/features/quotations/api.ts`
   - `frontend/src/pages/quotations/quotation-form-page.tsx`
2. Add `downloadExcel(id)` to `quotationsApi`:
   - GET `/quotations/${id}/excel`
   - `responseType: 'blob'`
3. Add an Excel action in `QuotationFormPage` for edit mode:
   - Keep existing PDF/print button behavior.
   - Add a sibling button with a suitable icon, for example `FileSpreadsheet` from `lucide-react`.
   - Use filename `BaoGia_${quotation.code}.xlsx`.
   - Use existing toast/error handling patterns.
4. Keep export disabled while existing submission/transition is pending.
5. Confirm route permission behavior:
   - Frontend route already guards edit page by update permission.
   - Backend enforces `quotations.print` for both binary export endpoints.
   - No new frontend permission model is required unless existing UI already hides print actions elsewhere.
6. Inspect `frontend/package.json` and run the existing validation scripts.
7. Manual browser verification:
   - Open saved quotation form.
   - Click Excel download and verify the file opens.
   - Click PDF/print and verify the file opens as PDF.

## Verification

- Commands:
  - `Get-Content -Path frontend/package.json`
  - Run the existing frontend typecheck/test command from `package.json`.
  - `dotnet test backend/OrderMgmt.sln --filter Quotations`
- Expected results:
  - Frontend compiles/typechecks.
  - Backend quotation tests pass.
  - Downloaded Excel and PDF use the saved quotation code in filenames.

## Exit Criteria

- Saved quotation form exposes both PDF and Excel exports.
- Excel export button downloads `.xlsx`.
- Existing PDF button still downloads `.pdf`, now generated from Excel template.
- No export action is shown for unsaved/new quotation forms.
