# Design: Quotation List Enhancements

**Date:** 2026-05-22  
**Scope:** Màn hình danh sách báo giá (`/quotations`)

## Tổng quan

Ba cải tiến độc lập cho trang list báo giá:
1. Thêm cột **Tạm ứng** vào grid và footer tổng kết
2. Thay 2 input ngày bằng **preset date filter** (giống dashboard)
3. Thêm status **KT xác nhận** vào bộ lọc trạng thái

---

## Phần 1: Cột Tạm ứng

### Backend

**`QuotationListItemDto`** — thêm field:
```csharp
public decimal AdvancePayment { get; set; }
```

**`QuotationListAggregates`** — thêm field:
```csharp
public decimal AdvancePayment { get; set; }
```

**`QuotationService.GetListAsync`** — cập nhật:
- Projection: thêm `AdvancePayment = q.AdvancePayment`
- Aggregates: thêm `AdvancePayment = await query.SumAsync(q => q.AdvancePayment)`

### Frontend

**`features/quotations/types.ts`**:
- `QuotationListItem`: thêm `advancePayment: number`
- `QuotationListAggregates`: thêm `advancePayment: number`

**`pages/quotations/quotation-list-page.tsx`**:
- Thêm cột "Tạm ứng" sau cột "Vận chuyển", trước "Tổng tiền"
- Format: số căn phải, `currency.format()`
- Thêm `advancePayment: data?.aggregates?.advancePayment ?? 0` vào `allTotals`
- Truyền `advancePayment={allTotals.advancePayment}` xuống `<ListFooter>`

**`pages/quotations/components/list-footer.tsx`**:
- Thêm prop `advancePayment: number` vào `ListFooterProps`
- Hiển thị `• TU <Money value={advancePayment} />` giữa VC và Tổng tiền

**Phân quyền:** Hiển thị cho tất cả mọi người (không guard permission).

---

## Phần 2: Date Filter Preset

### Approach: Promote RangePicker thành shared component

**Move file:**
`features/dashboard/components/range-picker.tsx` → `components/ui/range-picker.tsx`

**Thay đổi trong RangePicker:**
- Thêm prop `onClear?: () => void`
- Trạng thái "không có ngày" (from/to đều rỗng):
  - Không preset nào highlight
  - Không hiện text `"from → to"`
  - Nút "Tuỳ chỉnh" không highlight
- Khi đang có date active và `onClear` được truyền: hiện nút "×" nhỏ sau các preset để xoá filter
- Presets giữ nguyên: Hôm nay, 7N, 30N, Tháng này, Tháng trước, Tuỳ chỉnh

**Cập nhật import:**
- `RangePreset` type move cùng vào `components/ui/range-picker.tsx` (không còn export từ `use-dashboard-params.ts`)
- `features/dashboard/use-dashboard-params.ts`: import `RangePreset` từ `@/components/ui/range-picker`
- `pages/dashboard-page.tsx`: đổi import `RangePicker` sang `@/components/ui/range-picker`
- Xoá file cũ `features/dashboard/components/range-picker.tsx`

**`pages/quotations/quotation-list-page.tsx`:**
- Xoá 2 `<Input type="date">` (from và to)
- Thêm:
```tsx
<RangePicker
  from={fromDate}
  to={toDate}
  onChange={(f, t) => { setFromDate(f); setToDate(t); if (page !== 1) setPage(1); }}
  onClear={() => { setFromDate(''); setToDate(''); if (page !== 1) setPage(1); }}
/>
```
- URL params `from`/`to` giữ nguyên cơ chế hiện tại
- Default: không lọc theo ngày (fromDate = '' → không truyền vào query)

---

## Phần 3: Filter Status "KT xác nhận"

**`pages/quotations/quotation-list-page.tsx`** — chỉ thay đổi 2 constant:

```ts
const STATUS_OPTIONS: ReadonlyArray<{ value: QuotationStatus; label: string }> = [
  { value: 'Draft', label: 'Nháp' },
  { value: 'Sent', label: 'Đã gửi' },
  { value: 'Confirmed', label: 'Đã xác nhận' },
  { value: 'AccountingConfirmed', label: 'KT xác nhận' }, // thêm mới
  { value: 'Cancelled', label: 'Đã hủy' },
];

const DEFAULT_ACTIVE_STATUSES: ReadonlyArray<QuotationStatus> = [
  'Draft', 'Sent', 'Confirmed', 'AccountingConfirmed', // thêm AccountingConfirmed
];
```

Backend không cần thay đổi — `AccountingConfirmed` đã có trong enum `QuotationStatus`.

---

## Files cần thay đổi

| File | Thay đổi |
|------|----------|
| `backend/.../Models/QuotationDto.cs` | Thêm `AdvancePayment` vào `QuotationListItemDto` và `QuotationListAggregates` |
| `backend/.../Services/QuotationService.cs` | Projection + aggregate sum `AdvancePayment` |
| `frontend/src/features/quotations/types.ts` | Thêm `advancePayment` vào 2 interface |
| `frontend/src/components/ui/range-picker.tsx` | File mới (moved + adapted từ dashboard) |
| `frontend/src/features/dashboard/components/range-picker.tsx` | Xoá (replaced by shared component) |
| `frontend/src/pages/dashboard-page.tsx` | Update import RangePicker |
| `frontend/src/pages/quotations/quotation-list-page.tsx` | Cột tạm ứng + RangePicker + status options |
| `frontend/src/pages/quotations/components/list-footer.tsx` | Thêm tạm ứng vào footer |

---

## Không nằm trong scope

- Cột "Còn lại" (total - advancePayment)
- Thay đổi permission model
- Thay đổi logic pagination hoặc sorting
