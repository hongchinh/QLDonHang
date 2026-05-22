# Execution Report — Add AdvancePayment to Quotation

**Plan:** `docs/plans/260521-0010-advance-payment-on-quotation/SUMMARY.md`
**Executed:** 2026-05-22
**Executor:** Claude Sonnet 4.6 (claude-sonnet-4-6)

## Phases Completed

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 01 — Backend Domain/Migration | ✅ Complete | All tasks already implemented at execution start; verified build passes |
| Phase 02 — Frontend Core | ✅ Complete | Types, schema, compute-line updated; 12/12 unit tests pass |
| Phase 03 — Frontend UI | ✅ Complete | Form page wired, TotalsPanel updated with Tạm ứng + Còn lại rows |
| Phase 04 — Excel Export | ✅ Complete | Template modified programmatically, renderer updated, test added |

## Files Changed

### Backend
- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` — `AdvancePayment` field (pre-existing at execution start)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` — column type config
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/20260521172614_AddQuotationAdvancePayment.cs` — migration with `defaultValue: 0m`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` — field in `QuotationDto` and `UpsertQuotationRequest`
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs` — `GreaterThanOrEqualTo(0)` rule
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` — CreateAsync, UpdateAsync, MapToDto
- `backend/src/OrderMgmt.Infrastructure/Excel/QuotationExcelRenderer.cs` — `AdvancePaymentRowOffset=1`, `RemainingBalanceRowOffset=2` constants; `FillSummaryTotals` method
- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` — `InternalsVisibleTo` for integration tests
- `backend/src/OrderMgmt.WebApi/templates/template_baogia.xlsx` — 2 rows inserted (Tạm ứng row 18, Còn lại row 19)
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationCrudTests.cs` — 3 integration tests
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationExportTests.cs` — Excel cell-value test

### Frontend
- `frontend/src/features/quotations/types.ts` — `advancePayment: number` in `Quotation` and `UpsertQuotationRequest`
- `frontend/src/features/quotations/schema.ts` — `advancePayment: z.coerce.number().min(0)`
- `frontend/src/pages/quotations/utils/compute-line.ts` — `advancePayment` in `HeaderLike`, `remainingBalance` in `Totals`, updated `computeTotals`
- `frontend/src/pages/quotations/utils/compute-line.test.ts` — updated 3 existing calls, added 2 new tests
- `frontend/src/pages/quotations/quotation-form-page.tsx` — `watchedAdvancePayment`, header object, `onHeaderChange`, `toFormDefaults`, `toPayload`
- `frontend/src/pages/quotations/components/totals-panel.tsx` — `EditableMetric.label` optional, `MetricValue.negative` prop, Tạm ứng + Còn lại rows

## Verification Commands Run and Outcomes

| Command | Outcome |
|---------|---------|
| `dotnet build src/OrderMgmt.Application` | ✅ 0 errors |
| `dotnet build src/OrderMgmt.Infrastructure` | ✅ 0 errors |
| `dotnet build tests/OrderMgmt.IntegrationTests` | ✅ 0 errors |
| `npx vitest run compute-line.test.ts` | ✅ 12/12 pass |
| `npm run build` (TypeScript + Vite) | ✅ 0 errors |
| `dotnet test --filter QuotationCrudTests` | ⚠️ Skipped — Docker not running |
| `dotnet test --filter QuotationExportTests` | ⚠️ Skipped — Docker not running |

## Deviations from Plan

1. **Template row structure:** The plan assumed the template had pre-existing CK/VC/Thuế/Tổng cộng rows between the summaryRow and notes. The actual template had summaryRow (row 17 "Cộng tiền hàng") directly followed by notes. Tạm ứng and Còn lại were inserted at offset 1 and 2 from summaryRow (not 5 and 6 as the plan's example suggested). This aligns with the plan's intent — only the offset values differ from the example.

2. **Template modification automated:** The plan says "mở và sửa tay". The modification was done programmatically via a temporary ClosedXML script to ensure precision. The result is identical to a manual edit.

3. **Integration tests not run:** Docker is not running in the current environment. Tests compile and reference correct assertions; they should pass when Docker is available.

## Residual Risks / Follow-ups

- Run integration tests with Docker to confirm `QuotationCrudTests` (3 new tests) and `QuotationExportTests.Excel_AdvancePayment_WrittenToCorrectCells` pass.
- Manual UI verification: open form, enter tạm ứng value, confirm "Còn lại" row appears and turns red when negative.
- Template backup `template_baogia.xlsx.bak` exists at `backend/src/OrderMgmt.WebApi/templates/` — can be deleted after UI sign-off.
