# Quotation List — 3 Improvements

## Goal

Enhance the quotation list page with three additions: (1) an **Advance Payment** column in the grid and total in the footer, (2) a **preset date range picker** (Hôm nay / 7N / 30N / Tháng này / Tháng trước / Tuỳ chỉnh) replacing the two raw date inputs, and (3) **KT xác nhận** (`AccountingConfirmed`) added to the status filter—both as an option and as a default active status.

## Scope

- In scope:
  - Backend: expose `AdvancePayment` in `QuotationListItemDto` and `QuotationListAggregates`
  - Frontend: new shared `DateRangePicker` UI component
  - Frontend: wire up all three changes in `quotation-list-page.tsx` and `list-footer.tsx`
- Out of scope:
  - Changing dashboard's existing `RangePicker` (it keeps working as-is)
  - Any migration files (no schema change — `AdvancePayment` column already exists)
  - Permissions for advance payment (visible to all users per brainstorm decision)

## Assumptions

- `AdvancePayment` column already exists in the `Quotations` DB table (confirmed — entity and `QuotationDto` already have it; only the list DTO/aggregates are missing).
- `AccountingConfirmed` is already a valid `QuotationStatus` enum value on both backend and frontend.
- The shared `DateRangePicker` component lives in `frontend/src/components/ui/` alongside other shared UI components.
- `formatDateYmd` utility will be co-located inside the new component file to avoid importing from the dashboard feature directory.

## Risks

- Aggregate `SUM(AdvancePayment)` on large result sets is negligible — same pattern as existing `SUM(Total)`.
- Adding `AccountingConfirmed` to `DEFAULT_ACTIVE_STATUSES` widens the default query. Acceptable per user decision.

## Phases

- [ ] Phase 01 — Backend: AdvancePayment in list DTO & aggregates (S) — `phase-01-backend-advance-payment.md`
- [ ] Phase 02 — Shared DateRangePicker component (S) — `phase-02-date-range-picker-component.md`
- [ ] Phase 03 — Frontend wiring: list page + footer (M) — `phase-03-frontend-wiring.md`

## Final Verification

```bash
# Backend compiles
cd backend && dotnet build --no-restore -v q

# Frontend type-checks
cd frontend && npx tsc --noEmit
```

## Rollback / Recovery

All changes are additive or in-place edits to local files. No DB migration is involved. Revert with `git checkout` on the changed files.
