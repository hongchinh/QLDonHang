# Scrollable Sales Revenue Detail Table with Totals Summary

## Goal
Improve the sales revenue detail page by making the product table fixed-height with internal scrolling, adding a sticky table header, and displaying aggregate totals for quantity, amounts, freight, costs, and profit columns in a separate summary section below.

## Scope

### In Scope
- Convert table container to fixed responsive height (calc-based, viewport-aware)
- Make table header sticky during vertical scroll
- Create totals calculation logic for: quantity, lineTotal, freight, unitCost, lineCost, lineProfit
- Add separate totals section below table displaying calculated aggregates
- Maintain consistent formatting with existing table (tabular-nums, money format)
- Add tests for totals calculation logic

### Out of Scope
- Horizontal scroll improvements (already implemented)
- Pagination or virtual scrolling
- Column visibility toggles
- Export or print functionality

## Assumptions
- Table will be placed in the existing Card component structure
- Fixed height should use `calc(100vh - <pixels>)` for responsive behavior
- Totals section should be visually distinct from the scrolling table
- Users primarily work on desktop/tablet (not mobile-specific optimization)
- Existing `moneyFmt` formatter continues to be used for all numeric display

## Risks
- **Table height calculation:** If navigation bar or header heights change, fixed height calculation may need adjustment. Mitigation: Use a CSS variable or `dvh` unit if available.
- **Performance:** Calculating totals for large datasets (1000+ rows) should be fast. Mitigation: Use `useMemo` to prevent recalculation on every render.
- **Sticky header:** Some browsers may not support sticky positioning in overflow containers. Mitigation: Use standard `position: sticky` which has broad support.

## Phases
- [x] Phase 01 — Totals Calculation Logic (S) — `phase-01-totals-logic.md`
- [x] Phase 02 — Layout & Structure Changes (M) — `phase-02-layout-changes.md`
- [x] Phase 03 — Styling & Visual Integration (S) — `phase-03-styling.md`

## Final Verification
After all phases complete:
```bash
cd frontend
npm run test -- sales-revenue-detail
npm run build
npm run dev  # Visual inspection in browser
```

Then visit `/reports/sales-revenue-detail/...` (with appropriate params) to verify:
- Table height is fixed and constrained
- Table header stays visible when scrolling
- Totals section displays correct sums
- Money formatting is consistent
- No layout breaks on different screen sizes

## Rollback / Recovery
If issues arise:
1. Revert to previous commit: `git revert <commit-hash>`
2. Or cherry-pick individual files from `git show <previous-commit>:<file-path>`

All changes are isolated to the sales-revenue-detail page component and its test file; no shared dependencies are modified.
