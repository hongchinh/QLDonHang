# Execution Report — Totals Panel money input + width tuning

**Plan:** [SUMMARY.md](SUMMARY.md)
**Executed:** 2026-05-17
**Mode:** Batch

## Phases

- [x] Phase 01 — Create `money-input` util + tests
- [x] Phase 02 — Refactor `line-items-grid` to import from the new util
- [x] Phase 03 — Update `TotalsPanel`: behavior + layout

## Files Changed

- `frontend/src/pages/quotations/utils/money-input.ts` (new) — shared vi-VN format/parse helpers.
- `frontend/src/pages/quotations/utils/money-input.test.ts` (new) — 15 unit tests (8 parse + 7 format).
- `frontend/src/pages/quotations/components/line-items-grid.tsx` — import shared util; remove local `parseMoney` and `moneyInput`; inline `editing` ternary at the two call sites. `fmt` kept (still used by `cell-total-main` / `cell-total-meta`).
- `frontend/src/pages/quotations/components/totals-panel.tsx` — `EditableMetric` now uses `type="text"` + `inputMode="decimal"` + focus/blur draft state + auto-select; `SummaryRow` grid 86→64px / gap-3→gap-2 with `whitespace-nowrap` labels; Điều chỉnh inner grid gap-2→gap-1; Thuế inner grid 92→56px; Input padding `px-2`→`px-1.5`.

## Verification

| Command | Outcome |
|---|---|
| `npx vitest run src/pages/quotations/utils/money-input.test.ts` | 15/15 passed |
| `npx vitest run src/pages/quotations/components/line-items-grid.test.tsx` | 5/5 passed |
| `npm run typecheck` | clean |
| `npm run test -- --run` (full vitest suite) | 91/91 passed across 13 files |
| `npm run lint` | 0 errors. 3 pre-existing `jsx-a11y/label-has-associated-control` warnings in unrelated files (`admin/components/reset-password-dialog.tsx`, `reports/sales-revenue-page.tsx`) — not in scope. |

## Deviations from Plan

- **Test count:** SUMMARY mentioned "14 test cases (8 parse, 6 format)"; the phase-01 body lists 7 format cases (`3000000`, `0`, `undefined`, `null`, `""`, `"1500"`, `NaN`). Implemented all 7 → 15 total. Matches the phase body, not the SUMMARY tally.
- No other deviations. Tasks executed as specified.

## Pending Manual Verification

The plan defines a browser smoke test (`npm run dev` → `/quotations/new`, steps 1–9). This cannot be exercised from the CLI session. The user should run the smoke checklist in [SUMMARY.md](SUMMARY.md) section "Final Verification" before considering the change shipped. Key checks:

1. `99999999` in CK → blur shows full `"99.999.999"` (not clipped) at 100% and 110% zoom.
2. Click into CK at `"0"` and type `"5"` → field becomes `"5"`, not `"05"` (auto-select-on-focus).
3. Empty CK + blur → shows `"0"`.
4. Same for VC.
5. Thuế % accepts `10` and `8,5`; Tiền thuế column gains width.
6. `LineItemsGrid` Đơn giá / Giá vốn still format/parse as before.

## Residual Risks / Follow-ups

- Tổng cộng can flash to subtotal when user types a partially-typed thousand-separated number (e.g., `"3.000.0"`). Documented in plan Risks; behavior is consistent with `LineItemsGrid`.
- Width budget for CK/VC is tight (~10px buffer for `"99.999.999"`). Acceptable at 100–110% zoom; OS scrollbar overlays or font-size changes could push it past the cell.
- No new file references in `MEMORY.md` were warranted (no surprising decisions or user feedback this session).
