# Phase 02 — Frontend totals footer + cuộn dọc trong Card

**Status:** [ ] pending | [-] in-progress | [x] complete
**Complexity:** M

## Objective
Thêm `TotalsFooter` luôn hiển thị dưới Card với 4 cột tổng × 2 mức (*Trang hiện tại*, *Toàn bộ filter*). Restructure layout Card thành **flex chain** (`h-full` + `flex-1 min-h-0`, KHÔNG hardcode pixel) để khu vực `<Table>` cuộn dọc bên trong khi bảng nhiều dòng. Pagination và footer luôn nhìn thấy.

## Files
- `frontend/src/features/quotations/types.ts` — thêm 2 interface.
- `frontend/src/features/quotations/api.ts` — đổi generic `quotationsApi.list`.
- `frontend/src/pages/quotations/components/totals-footer.tsx` (mới) — component trình bày footer.
- `frontend/src/pages/quotations/quotation-list-page.tsx` — tính `pageTotals`, restructure layout, render `<TotalsFooter />`.

> **Note về line numbers:** số dòng trong các Task bên dưới là **gợi ý vị trí** tại thời điểm viết plan. Khi execute, search bằng tên import / component / variable thay vì paste theo line.

## Tasks

### 1. Thêm types trong `types.ts`
Sau interface `QuotationListItem` (dòng 69-87), thêm:
```ts
export interface QuotationListAggregates {
  subtotal: number;
  discount: number;
  freight: number;
  total: number;
}

export interface QuotationListResult extends PagedResult<QuotationListItem> {
  aggregates: QuotationListAggregates;
}
```
Export ở chỗ hiện tại (file đã re-export `PagedResult` ở cuối).

### 2. Đổi generic của `quotationsApi.list` trong `api.ts`
Dòng 19, đổi:
```ts
return apiGet<QuotationListResult>('/quotations', serialized);
```
Import thêm `QuotationListResult` từ `./types`.

`useQuotations` trong `hooks.ts` không cần sửa — kiểu tự suy ra từ `quotationsApi.list`.

### 3. Tạo component `components/totals-footer.tsx`

**A11y:** dùng `role="group"` + `aria-label` để screen reader đọc theo cụm; mỗi cell có `aria-label` mô tả (vd "Tổng tiền trang hiện tại: 2.500.000"). Loading dùng skeleton bar (không phải text `—`), empty dùng `0`.

```tsx
import type { QuotationListAggregates } from '@/features/quotations/types';

const currency = new Intl.NumberFormat('vi-VN');

interface TotalsFooterProps {
  page: QuotationListAggregates;
  all: QuotationListAggregates;
  loading?: boolean;
  errored?: boolean;
}

function Cell({
  value,
  loading,
  errored,
  strong,
  label,
}: {
  value: number;
  loading?: boolean;
  errored?: boolean;
  strong?: boolean;
  label: string;
}) {
  if (loading) {
    return (
      <div className="flex justify-end" aria-label={`${label}: đang tải`}>
        <div className="h-4 w-20 animate-pulse rounded bg-muted" />
      </div>
    );
  }
  const text = errored ? '—' : currency.format(value);
  return (
    <div
      className={`text-right tabular-nums ${strong ? 'font-semibold' : ''}`}
      aria-label={`${label}: ${text}`}
    >
      {text}
    </div>
  );
}

export function TotalsFooter({ page, all, loading, errored }: TotalsFooterProps) {
  return (
    <div
      role="group"
      aria-label="Tổng tiền báo giá"
      className="grid grid-cols-[140px_repeat(4,minmax(0,1fr))] gap-x-4 gap-y-1 text-sm"
    >
      <div className="text-muted-foreground">Trang hiện tại</div>
      <Cell value={page.subtotal} loading={loading} errored={errored} label="Tiền hàng trang" />
      <Cell value={page.discount} loading={loading} errored={errored} label="Chiết khấu trang" />
      <Cell value={page.freight}  loading={loading} errored={errored} label="Vận chuyển trang" />
      <Cell value={page.total}    loading={loading} errored={errored} label="Tổng tiền trang" strong />

      <div className="text-muted-foreground">Toàn bộ filter</div>
      <Cell value={all.subtotal} loading={loading} errored={errored} label="Tiền hàng toàn bộ" />
      <Cell value={all.discount} loading={loading} errored={errored} label="Chiết khấu toàn bộ" />
      <Cell value={all.freight}  loading={loading} errored={errored} label="Vận chuyển toàn bộ" />
      <Cell value={all.total}    loading={loading} errored={errored} label="Tổng tiền toàn bộ" strong />
    </div>
  );
}
```

### 4. Sửa `quotation-list-page.tsx`

**4.1. Import:** thêm dòng import `TotalsFooter` sau import `StatusPill`:
```ts
import { TotalsFooter } from './components/totals-footer';
```

**4.2. Tính tổng trong component body** (sau khi destructure `useQuotations`):
```ts
const pageTotals = useMemo(() => {
  const items = data?.items ?? [];
  return items.reduce(
    (acc, q) => ({
      subtotal: acc.subtotal + q.subtotal,
      discount: acc.discount + q.discount,
      freight:  acc.freight  + q.freight,
      total:    acc.total    + q.total,
    }),
    { subtotal: 0, discount: 0, freight: 0, total: 0 },
  );
}, [data?.items]);

const allTotals = data?.aggregates ?? { subtotal: 0, discount: 0, freight: 0, total: 0 };
```

**4.3. Restructure JSX — flex chain (KHÔNG dùng `maxHeight: calc(100vh - 200px)`):**

Đổi page wrapper (hiện tại `<div className="space-y-4">`) và Card để chain `h-full + flex-1 min-h-0` từ shell xuống vùng table. Shell content có `overflow-y-auto` sẽ KHÔNG kích hoạt khi page = `h-full` đúng cách.

```tsx
// Page wrapper — đổi từ "space-y-4" thành flex column với h-full
<div className="flex h-full min-h-0 flex-col gap-4">
  {/* Title bar (h1 + nút Thêm) — giữ nguyên markup, không flex-grow */}
  <div className="flex items-center justify-between">{/* ... */}</div>

  {/* Card — flex-1 min-h-0 để giành phần height còn lại */}
  <Card className="flex flex-1 min-h-0 flex-col">
    <CardContent className="flex flex-1 min-h-0 flex-col gap-3 p-4">
      {/* Filter bar — giữ nguyên markup, không flex-grow */}
      <div className="flex flex-wrap items-center gap-2">{/* ... */}</div>

      {isError && (
        <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {getErrorMessage(error)}
        </div>
      )}

      {/* Khu vực bảng — flex-1 min-h-0 + overflow-y-auto: cuộn dọc trong Card */}
      <div className="flex-1 min-h-0 overflow-y-auto rounded-md border">
        <Table>
          {/* giữ nguyên TableHeader + TableBody */}
        </Table>
      </div>

      {/* Footer tổng — luôn hiển thị, nằm ngoài vùng scroll */}
      <div className="border-t pt-3">
        <TotalsFooter
          page={pageTotals}
          all={allTotals}
          loading={isLoading}
          errored={isError}
        />
      </div>

      {/* Pagination — giữ nguyên điều kiện hiển thị */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between text-sm">{/* ... */}</div>
      )}
    </CardContent>
  </Card>
</div>
```

**Lưu ý chain:** `min-h-0` BẮT BUỘC ở mọi flex-column trung gian (page wrapper, Card, CardContent, table area) — thiếu 1 chỗ → child sẽ tràn ra ngoài và shell scroll kích hoạt → user thấy double scroll. Verify trong smoke test.

Giữ nguyên: filter bar markup, error banner, table header/body markup, pagination markup, `<ConfirmDialog>` ở cuối.

### 5. Tự kiểm tra TypeScript
- `data` (kiểu `QuotationListResult | undefined`) có `aggregates` → không lỗi.
- `pageTotals` có cùng shape với `QuotationListAggregates` → pass vào `TotalsFooter`.

## Verification

```powershell
# Type-check + lint
npm --prefix frontend run build
npm --prefix frontend run lint

# Khởi động dev server và mở /quotations (nếu chưa chạy)
npm --prefix frontend run dev
```

**Manual smoke test trên `/quotations`:**

1. Footer hiện 2 dòng (`Trang hiện tại`, `Toàn bộ filter`), mỗi dòng 4 ô tiền; cột "Tổng tiền" in đậm.
2. Đổi trang qua nút "Trước"/"Sau" → "Trang hiện tại" thay đổi; "Toàn bộ filter" giữ nguyên.
3. Đổi filter (status / from / to / search) → cả 2 dòng cập nhật. "Trang hiện tại" reset về tổng của trang 1.
4. Seed (hoặc tạo bằng tay) >25 báo giá → bảng cao hơn Card → khu vực `<Table>` cuộn dọc **bên trong** Card; **không** thấy scroll thứ 2 ở shell (nếu có → chain `min-h-0` đâu đó bị thiếu). Footer + pagination vẫn nhìn thấy ở đáy Card. Filter bar không cuộn theo.
5. Trong khi loading → 8 ô tiền hiển thị **skeleton bar** (`animate-pulse`), không phải text `—`.
6. Bảng rỗng (filter không match) → 8 ô = `0` (phân biệt rõ với loading).
7. Tắt mạng + reload → 8 ô là `—` (errored state); banner lỗi vẫn hoạt động.
8. Login user thường không có `quotations.view_all` → "Toàn bộ filter" chỉ tính báo giá của user đó (cross-check bằng cách login admin để so sánh).
9. **Cancelled rule:** không filter status → "Toàn bộ filter" KHÔNG bao gồm Cancelled (so với "Trang hiện tại" có thể chứa Cancelled rows → 2 dòng có thể chênh ở mức số nhỏ; OK theo thiết kế). Filter status=Cancelled → cả 2 dòng đều phản ánh Cancelled.
10. **A11y:** dùng tab/screen reader (VoiceOver hoặc NVDA) duyệt qua footer → đọc đúng cụm "Tổng tiền báo giá" + từng cell với label rõ ràng.
11. **Responsive (desktop only — mobile out of scope):** desktop ≥1024px, footer hiển thị bình thường. Khi resize xuống mobile, Table có overflow-x → footer grid không khớp cột, accepted (đã ghi trong risks).

## Exit Criteria
- [ ] `npm run build` (tsc -b + vite build) thành công, không lỗi type.
- [ ] `npm run lint` không error.
- [ ] Footer luôn hiển thị (loading / empty / error / có data); loading dùng skeleton bar, empty hiển thị `0`, error hiển thị `—`.
- [ ] Khi bảng nhiều dòng, vùng bảng cuộn dọc bên trong Card; footer + pagination luôn nhìn thấy. **Không có** double scroll (shell scroll không kích hoạt).
- [ ] "Trang hiện tại" khớp với manual sum của 4 cột tiền trong rows đang hiển thị; "Toàn bộ filter" khớp với SUM backend (cross-check qua DevTools network response `aggregates`).
- [ ] Cancelled rule: không filter status → "Toàn bộ filter" loại trừ Cancelled; filter status=Cancelled → bao gồm Cancelled.
- [ ] Footer có `role="group"` + `aria-label`; mỗi cell có `aria-label` mô tả.
- [ ] Không regression UX của filter / pagination / dropdown actions hiện có.
