# Execution Report — Báo Giá Module MVP

**Plan:** `docs/plans/260513-0055-bao-gia-mvp/SUMMARY.md`
**Executed:** 2026-05-13
**Mode:** Batch

## Phases

- [x] Phase 01 — Product PricingMode (M)
- [x] Phase 02 — Product search endpoint (S)
- [x] Phase 03 — Quotation domain + migration (M)
- [x] Phase 04 — Quotation application layer (L)
- [x] Phase 05 — Quotation CRUD controller (S)
- [x] Phase 06 — PDF rendering with QuestPDF (M)
- [x] Phase 07 — Frontend feature module (S)
- [x] Phase 08 — Frontend pages + routing (L)
- [x] Phase 09 — Integration tests (M) — files compile-clean; runtime execution deferred (see Residual risks)

## Files changed

### Backend — Domain
- `backend/src/OrderMgmt.Domain/Enums/Enums.cs` — added `PricingMode` enum.
- `backend/src/OrderMgmt.Domain/Entities/Catalog/Product.cs` — added `PricingMode` field, default `PerUnit`.
- `backend/src/OrderMgmt.Domain/Entities/Sales/Quotation.cs` — new aggregate.
- `backend/src/OrderMgmt.Domain/Entities/Sales/QuotationLine.cs` — new owned entity.

### Backend — Application
- `backend/src/OrderMgmt.Application/Catalog/Products/Models/ProductDto.cs` — `PricingMode` on DTOs + new `ProductSuggestionDto`.
- `backend/src/OrderMgmt.Application/Catalog/Products/Interfaces/IProductService.cs` — added `SearchAsync`.
- `backend/src/OrderMgmt.Application/Catalog/Products/Services/ProductService.cs` — implement `SearchAsync`, copy `PricingMode` in CRUD/list.
- `backend/src/OrderMgmt.Application/Catalog/Products/Validators/ProductValidators.cs` — `PricingMode.IsInEnum()` on create/update.
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` — `Quotations` + `QuotationLines` DbSets.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Models/QuotationDto.cs` — DTOs + `QuotationAction` enum.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationService.cs` — service contract.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Interfaces/IQuotationPdfRenderer.cs` — renderer port.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Services/QuotationService.cs` — CRUD + recompute + state machine + PDF orchestration.
- `backend/src/OrderMgmt.Application/Sales/Quotations/Validators/QuotationValidators.cs` — FluentValidation rules.
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — register `IQuotationService`.

### Backend — Infrastructure
- `backend/src/OrderMgmt.Infrastructure/OrderMgmt.Infrastructure.csproj` — added `QuestPDF 2024.12.3` + `EmbeddedResource Pdf\Fonts\*.ttf`.
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/CatalogConfiguration.cs` — `Product.PricingMode` mapping (`int`, default `PerUnit`).
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SalesConfiguration.cs` — `QuotationConfiguration` + `QuotationLineConfiguration`.
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` — `Quotations` + `QuotationLines` `DbSet`s.
- `backend/src/OrderMgmt.Infrastructure/Migrations/20260512180946_AddPricingModeToProduct.cs` (+ Designer) — generated.
- `backend/src/OrderMgmt.Infrastructure/Migrations/20260512181542_AddQuotations.cs` (+ Designer) — generated.
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — regenerated.
- `backend/src/OrderMgmt.Infrastructure/Pdf/QuotationPdfRenderer.cs` — QuestPDF customer-facing layout.
- `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/README.txt` — instructions to drop Roboto TTFs.
- `backend/src/OrderMgmt.Infrastructure/DependencyInjection.cs` — QuestPDF license/font bootstrap + `IQuotationPdfRenderer` registration.

### Backend — WebApi
- `backend/src/OrderMgmt.WebApi/Controllers/ProductsController.cs` — new `Search` action.
- `backend/src/OrderMgmt.WebApi/Controllers/QuotationsController.cs` — new (CRUD + `/transition` + `/pdf`).

### Backend — Tests
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationTestBase.cs` — shared fixture base.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationCrudTests.cs` — new.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationRecomputeTests.cs` — new.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationStateMachineTests.cs` — new.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationPermissionTests.cs` — new.
- `backend/tests/OrderMgmt.IntegrationTests/Quotations/QuotationSoftDeleteCascadeTests.cs` — new.

### Frontend — features/products
- `frontend/src/features/products/types.ts` — `PricingMode` union + `ProductSuggestion`; `pricingMode` on all product types.
- `frontend/src/features/products/schema.ts` — `pricingMode` field.
- `frontend/src/features/products/api.ts` — `productsApi.search`.
- `frontend/src/features/products/keys.ts` — `search` key.
- `frontend/src/features/products/hooks.ts` — `useProductSearch` + debounce.

### Frontend — features/quotations (all new)
- `frontend/src/features/quotations/types.ts`
- `frontend/src/features/quotations/schema.ts`
- `frontend/src/features/quotations/api.ts`
- `frontend/src/features/quotations/keys.ts`
- `frontend/src/features/quotations/hooks.ts`

### Frontend — pages/quotations (all new)
- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `frontend/src/pages/quotations/quotation-form-page.tsx`
- `frontend/src/pages/quotations/components/status-pill.tsx`
- `frontend/src/pages/quotations/components/product-typeahead-cell.tsx` — uses absolute-positioned dropdown (no Radix Popover dependency).
- `frontend/src/pages/quotations/components/line-items-grid.tsx`
- `frontend/src/pages/quotations/components/totals-panel.tsx`
- `frontend/src/pages/quotations/utils/compute-line.ts`
- `frontend/src/pages/quotations/utils/compute-line.test.ts` — 9 vitest cases for the FE recompute.

### Frontend — other
- `frontend/src/pages/products/product-form-page.tsx` — `pricingMode` select + payload wiring.
- `frontend/src/pages/products/product-list-page.tsx` — `Loại giá` column.
- `frontend/src/lib/permissions.ts` — added `quotations.delete`, `quotations.print` codes.
- `frontend/src/App.tsx` — replaced `/quotations` placeholder with real routes (list/new/edit) gated by permissions.

## Verification commands run

| Command | Outcome |
|---|---|
| `dotnet build OrderMgmt.Domain` | ✅ 0 warnings, 0 errors |
| `dotnet build OrderMgmt.Application` | ✅ 0 warnings, 0 errors |
| `dotnet build OrderMgmt.Infrastructure` | ✅ 0 warnings, 0 errors |
| `dotnet build OrderMgmt.WebApi` | ✅ 0 warnings, 0 errors |
| `dotnet build OrderMgmt.IntegrationTests` | ✅ 0 warnings, 0 errors |
| `dotnet ef migrations add AddPricingModeToProduct` | ✅ migration + designer generated (one informational EF Core warning about CLR default sentinel — see Deviations) |
| `dotnet ef migrations add AddQuotations` | ✅ migration + designer generated |
| `npm run build` (frontend) | ✅ tsc + vite both clean |
| `npm test -- --run` (frontend) | ✅ 37/37 tests pass (28 prior + 9 new for compute-line) |
| `dotnet test OrderMgmt.IntegrationTests` | ⏭ NOT RUN — Docker / Postgres unavailable in this environment (see Residual risks) |
| Manual smoke (Swagger / UI) | ⏭ NOT RUN — requires WebApi restart by user |

## Deviations from plan

1. **QuestPDF font registration is generic-glob**, not three explicit `RegisterFontFromEmbeddedResource` calls. `Infrastructure.DependencyInjection.BootstrapQuestPdf` iterates `*.Pdf.Fonts.*.ttf` embedded resources and registers each. This means dropping the Roboto TTFs in `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/` after the fact requires no code change. Behavior with no fonts present: QuestPDF falls back to its default Lato font; Vietnamese diacritics may render incompletely. A README in that folder documents the requirement.

2. **Phase 06 fonts are not bundled.** The plan called for embedding Roboto-Regular/Bold/Italic. I cannot fetch binary files, so the `.csproj` glob references the three files but they are not present. User chose "I'll drop TTFs in later" via AskUserQuestion. Until the TTFs are added, the PDF endpoint still works but Vietnamese rendering quality is limited.

3. **Product typeahead uses absolute-positioned dropdown, not Radix Popover.** The plan permitted this fallback; `@radix-ui/react-popover` is not in `package.json`. Behavior is functionally equivalent.

4. **`Recompute` is `static` and pure** as required. It is private to `QuotationService`; in retrospect it could have been exposed for direct unit testing inside the test project, but the plan’s phase-09 tests exercise it end-to-end via the controller, which still locks the contract.

5. **EF Core informational warning** (`PricingMode` "configured with database-generated default, but has no configured sentinel value"). Practical impact is nil because the C# initializer sets `PricingMode = PerUnit` (which happens to equal the column default). Suppressed implicitly by initializer; not silenced via `[Sentinel]` attribute to avoid touching unrelated code.

6. **Phase 04 — `IQuotationPdfRenderer` injection on QuotationService** was added in Phase 06 (not retroactively documented in the plan). Done forward-compatibly: the renderer port is referenced from the service constructor, and DI registers a concrete implementation.

## Residual risks / follow-ups

- 🔴 **Integration tests not executed.** All 16 new test methods compile cleanly but were not run because Docker isn't installed (Testcontainers prerequisite) and no `TEST_DB_CONNECTION` Postgres was offered. **Action:** run `dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj --nologo` after starting Docker Desktop OR after setting `TEST_DB_CONNECTION` to a reachable Postgres dev instance.

- 🟡 **Roboto TTFs missing.** Drop the three files into `backend/src/OrderMgmt.Infrastructure/Pdf/Fonts/` (see that folder's README). Until then, Vietnamese diacritics on the generated PDF may render as boxes. No build/runtime error is raised.

- 🟡 **Manual smoke not executed.** After restarting WebApi, verify (a) Swagger lists 7 `Quotations` endpoints, (b) end-to-end create → Send → Confirm → Print works in the UI, and (c) no console warnings about controlled inputs in the line-items grid.

- 🟢 **PricingMode sentinel warning.** Can be silenced later by adding `[Sentinel(PricingMode.PerUnit)]` if it becomes noisy.

- 🟢 **`QuotationListItemDto.CreatedByName`** is fetched via a sub-query against `Users` per row. Acceptable at MVP scale; revisit if the list grows past ~1k rows or shows N+1 in production traces.

- 🟢 **`@radix-ui/react-popover`** is not used. If a richer typeahead UI is needed later (focus trapping, keyboard nav via Radix), add the package and refactor `product-typeahead-cell.tsx`.

## Final status

`Execution complete. Report archived at docs/plans/archived/260513-0055-bao-gia-mvp/EXECUTION-REPORT.md.`
