# Phase 03 — Frontend wiring: list page + footer

**Status:** [ ] pending
**Complexity:** M

## Objective

Wire up all three user-facing changes in the quotation list:
1. `advancePayment` column + footer total
2. Preset date range picker replacing the two raw date inputs
3. `AccountingConfirmed` (KT xác nhận) added to status filter options and defaults

## Files

- `frontend/src/features/quotations/types.ts`
- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `frontend/src/pages/quotations/components/list-footer.tsx`

## Tasks

### A. `types.ts` — extend interfaces

1. **`QuotationListItem`** — add after `total`:
   ```ts
   advancePayment: number;
   ```

2. **`QuotationListAggregates`** — add after `total`:
   ```ts
   advancePayment: number;
   ```

### B. `quotation-list-page.tsx` — status filter

3. **`STATUS_OPTIONS`** — add after `Confirmed` entry:
   ```ts
   { value: 'AccountingConfirmed', label: 'KT xác nhận' },
   ```

4. **`DEFAULT_ACTIVE_STATUSES`** — add `'AccountingConfirmed'`:
   ```ts
   const DEFAULT_ACTIVE_STATUSES: ReadonlyArray<QuotationStatus> = ['Draft', 'Sent', 'Confirmed', 'AccountingConfirmed'];
   ```

### C. `quotation-list-page.tsx` — advance payment in allTotals

5. **`allTotals` useMemo** — add after `total`:
   ```ts
   advancePayment: data?.aggregates?.advancePayment ?? 0,
   ```

### D. `quotation-list-page.tsx` — grid column

6. **`columns` useMemo** — add after the `total` column definition (before the `canViewCost` conditional block):
   ```tsx
   {
     header: () => moneyHeader('Tạm ứng'),
     accessorKey: 'advancePayment',
     cell: ({ row }) => moneyCell(row.original.advancePayment),
   },
   ```

### E. `quotation-list-page.tsx` — date range picker

7. **Add import** at top of file:
   ```ts
   import { DateRangePicker } from '@/components/ui/date-range-picker';
   ```

8. **Replace the two `<Input type="date">` elements** in the filter bar with:
   ```tsx
   <DateRangePicker
     from={fromDate}
     to={toDate}
     onChange={(f, t) => {
       setFromDate(f);
       setToDate(t);
       if (page !== 1) setPage(1);
     }}
   />
   ```
   Remove the now-unused `from` and `to` `<Input>` elements entirely.

### F. `quotation-list-page.tsx` — pass advancePayment to footer

9. **`ListFooter` usage** — add prop:
   ```tsx
   advancePayment={allTotals.advancePayment}
   ```

### G. `list-footer.tsx` — display advance payment total

10. **`ListFooterProps`** — add after `aggregates`:
    ```ts
    advancePayment?: number;
    ```
    
    Wait — `advancePayment` is already inside `aggregates: QuotationListAggregates`. Since `QuotationListAggregates` now has `advancePayment`, no extra prop is needed. The footer receives it via `aggregates.advancePayment`.

    Instead, update the **rendered summary line** in `ListFooter` to show advance payment after the freight line:
    ```tsx
    {' • '}TU <Money value={aggregates.advancePayment} loading={loading} errored={errored} />
    ```
    Insert after the `VC` (freight) display and before the cost/profit block.

## Verification

```bash
cd frontend && npx tsc --noEmit
```

Manual check (if dev server running):
- Grid shows "Tạm ứng" column after "Tổng tiền", right-aligned, formatted
- Footer summary shows `• TU X,XXX`
- Status filter dropdown includes "KT xác nhận" and it is checked by default on fresh page load
- Date filter shows 5 preset chips + "Tuỳ chỉnh" dropdown; selecting a preset updates `from`/`to` URL params and re-fetches

## Exit Criteria

- `npx tsc --noEmit` passes with no new errors
- `QuotationListItem.advancePayment` and `QuotationListAggregates.advancePayment` present in types
- Column "Tạm ứng" visible in grid between "Tổng tiền" and cost columns (or actions if no cost permission)
- Footer line includes `TU` amount
- `STATUS_OPTIONS` has `AccountingConfirmed` entry
- `DEFAULT_ACTIVE_STATUSES` includes `'AccountingConfirmed'`
- `DateRangePicker` replaces the two raw date inputs
