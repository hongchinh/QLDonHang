# Phase 02 — Frontend Types & Grid Column

**Status:** [ ] pending
**Complexity:** S

## Objective

Cập nhật TypeScript types để khớp với backend mới, thêm cột "Tạm ứng" vào grid và aggregate vào footer.

## Files

- `frontend/src/features/quotations/types.ts`
- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `frontend/src/pages/quotations/components/list-footer.tsx`

## Tasks

1. **`types.ts` — `QuotationListItem`**: thêm sau `total: number;`:
   ```ts
   advancePayment: number;
   ```

2. **`types.ts` — `QuotationListAggregates`**: thêm sau `total: number;`:
   ```ts
   advancePayment: number;
   ```

3. **`quotation-list-page.tsx` — `allTotals`**: thêm field `advancePayment` vào computed object, sau `total`:
   ```ts
   advancePayment: data?.aggregates?.advancePayment ?? 0,
   ```

4. **`quotation-list-page.tsx` — `columns`**: thêm column object vào mảng columns, ngay **sau** column `total` và **trước** block `...(canViewCost ? [...] : [])`:
   ```ts
   {
     header: () => moneyHeader('Tạm ứng'),
     accessorKey: 'advancePayment',
     cell: ({ row }) => moneyCell(row.original.advancePayment),
   },
   ```

5. **`list-footer.tsx` — summary line**: thêm đoạn sau `• VC <Money .../>` và trước block `{showCostProfit && ...}`:
   ```tsx
   {' • '}Tạm ứng <Money value={aggregates.advancePayment} loading={loading} errored={errored} />
   ```

## Verification

```bash
cd frontend && npx tsc --noEmit
```

Không có lỗi type.

## Exit Criteria

- `QuotationListItem.advancePayment` và `QuotationListAggregates.advancePayment` tồn tại trong types
- Cột "Tạm ứng" xuất hiện trong grid (sau Tổng tiền, trước Tổng nhập/Tổng LN)
- Footer hiển thị "Tạm ứng" aggregate sau "VC"
- TypeScript compile sạch
