# Phase 03 — Frontend quotation list

**Status:** [ ] pending
**Complexity:** M

## Objective
Cập nhật trang danh sách báo giá: thêm cột + footer Tạm ứng, thay date inputs bằng RangePicker, thêm AccountingConfirmed vào status filter.

## Files
- `frontend/src/features/quotations/types.ts`
- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `frontend/src/pages/quotations/components/list-footer.tsx`

## Tasks

### 1. `types.ts` — thêm `advancePayment`

**`QuotationListItem`**: thêm sau `freight`:
```ts
advancePayment: number;
```

**`QuotationListAggregates`**: thêm sau `freight`:
```ts
advancePayment: number;
```

---

### 2. `list-footer.tsx` — thêm Tạm ứng

a. Thêm `advancePayment: number` vào `ListFooterProps` interface (sau `freight`):
```ts
advancePayment: number;
```

b. Thêm render trong JSX, sau `• VC` và trước `• Tổng`:
```tsx
{' • '}TU <Money value={advancePayment} loading={loading} errored={errored} />
```

c. Nhận và truyền prop `advancePayment` trong function signature.

---

### 3. `quotation-list-page.tsx` — cột Tạm ứng

a. **Import thêm** `RangePicker` ở đầu file:
```ts
import { RangePicker } from '@/components/ui/range-picker';
```

b. **`allTotals`**: thêm `advancePayment` vào useMemo:
```ts
advancePayment: data?.aggregates?.advancePayment ?? 0,
```

c. **Cột grid**: thêm sau cột `freight`, trước cột `total`:
```ts
{
  header: () => moneyHeader('Tạm ứng'),
  accessorKey: 'advancePayment',
  cell: ({ row }) => moneyCell(row.original.advancePayment),
},
```

d. **`<ListFooter>`**: thêm prop `advancePayment={allTotals.advancePayment}`.

e. **Status options**: cập nhật `STATUS_OPTIONS`:
```ts
const STATUS_OPTIONS: ReadonlyArray<{ value: QuotationStatus; label: string }> = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Sent', label: 'Đã gửi' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'AccountingConfirmed', label: 'KT xác nhận' },
  { value: 'Cancelled', label: 'Đã hủy' },
];
```

f. **Default statuses**: cập nhật `DEFAULT_ACTIVE_STATUSES`:
```ts
const DEFAULT_ACTIVE_STATUSES: ReadonlyArray<QuotationStatus> = [
  'Draft', 'Sent', 'Confirmed', 'AccountingConfirmed',
];
```

g. **Thay date inputs**: xoá 2 block `<Input type="date">` (from và to), thay bằng:
```tsx
<RangePicker
  from={fromDate}
  to={toDate}
  onChange={(f, t) => {
    setFromDate(f);
    setToDate(t);
    if (page !== 1) setPage(1);
  }}
  onClear={() => {
    setFromDate('');
    setToDate('');
    if (page !== 1) setPage(1);
  }}
/>
```

---

## Verification
```bash
cd frontend
npx tsc --noEmit
```
TypeScript build pass. Kiểm tra thêm trực quan:
- Cột "Tạm ứng" hiển thị đúng vị trí (sau VC, trước Tổng tiền)
- Footer hiện "TU ..." giữa VC và Tổng
- Preset buttons (Hôm nay, 7N, 30N, Tháng này, Tháng trước, Tuỳ chỉnh, ×) thay thế 2 input ngày
- Dropdown status có "KT xác nhận", và nó được tick mặc định

## Exit Criteria
- `QuotationListItem.advancePayment` và `QuotationListAggregates.advancePayment` có kiểu `number`
- Grid có cột "Tạm ứng" giữa "Vận chuyển" và "Tổng tiền"
- Footer hiển thị tổng tạm ứng
- Date inputs đã được thay bằng `RangePicker`
- `STATUS_OPTIONS` có `AccountingConfirmed`; `DEFAULT_ACTIVE_STATUSES` có `AccountingConfirmed`
- `tsc --noEmit` pass
