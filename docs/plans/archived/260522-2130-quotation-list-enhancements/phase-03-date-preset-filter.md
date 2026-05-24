# Phase 03 — Date Preset Filter Component

**Status:** [ ] pending
**Complexity:** M

## Objective

Extract preset logic sang lib chung, tạo `QuotationDateFilter` với preset "Tất cả / 7N / 30N / Tháng này / Tháng trước / Tuỳ chỉnh", thay 2 `<Input type="date">` trong list page.

## Files

- `frontend/src/lib/date-range-presets.ts` *(mới)*
- `frontend/src/features/dashboard/range-picker.tsx` *(refactor import)*
- `frontend/src/pages/quotations/components/quotation-date-filter.tsx` *(mới)*
- `frontend/src/pages/quotations/quotation-list-page.tsx`

## Tasks

### 3.1 Tạo `src/lib/date-range-presets.ts`

Tạo file mới với nội dung sau (logic copy từ `range-picker.tsx`):

```ts
import { formatDateYmd } from '@/features/dashboard/format';

export type RangePreset = '7d' | '30d' | 'this-month' | 'last-month';

export function computePreset(preset: RangePreset): { from: string; to: string } {
  const today = new Date();
  let start: Date;
  let end: Date = today;
  switch (preset) {
    case '7d':
      start = new Date(today);
      start.setDate(start.getDate() - 6);
      break;
    case '30d':
      start = new Date(today);
      start.setDate(start.getDate() - 29);
      break;
    case 'this-month':
      start = new Date(today.getFullYear(), today.getMonth(), 1);
      break;
    case 'last-month':
      start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      end = new Date(today.getFullYear(), today.getMonth(), 0);
      break;
    default:
      start = new Date(today.getFullYear(), today.getMonth(), 1);
  }
  return { from: formatDateYmd(start), to: formatDateYmd(end) };
}

export function matchActivePreset(from: string, to: string): RangePreset | null {
  const presets: RangePreset[] = ['7d', '30d', 'this-month', 'last-month'];
  for (const p of presets) {
    const expected = computePreset(p);
    if (expected.from === from && expected.to === to) return p;
  }
  return null;
}
```

> **Lưu ý về `RangePreset` type:** `range-picker.tsx` hiện import `RangePreset` từ `use-dashboard-params.ts` (type có thêm `'today'`). Sau khi extract, `QuotationDateFilter` dùng type mới này (không có `'today'`). `RangePicker` dashboard sẽ dùng type từ `use-dashboard-params.ts` — giữ nguyên không đổi (xem task 3.2).

### 3.2 Update `range-picker.tsx` — không thay đổi behavior

`RangePicker` dashboard hiện tự define `computePreset` và `matchActivePreset` local. Thay bằng import từ lib chung **chỉ khi** các presets dùng trong dashboard (`7d`, `30d`, `this-month`, `last-month`) là subset của `RangePreset` mới.

Thực hiện:
- Import `computePreset` và `matchActivePreset` từ `@/lib/date-range-presets`
- Xóa 2 local function definitions cũ trong file
- Giữ nguyên `PRESETS` array (bao gồm `'today'`), `onPreset`, interface, JSX — không đổi gì khác
- Kiểm tra: `today` preset vẫn gọi `computePreset` từ `use-dashboard-params` (qua `onPreset` callback), không qua lib mới — điều này đã đúng vì `onPreset` được truyền từ dashboard page

> **Thực ra đơn giản hơn:** Xem lại `range-picker.tsx` — `computePreset` local function có thêm case `'today'`. `matchActivePreset` so sánh với tất cả PRESETS kể cả `'today'`. Để tránh breaking change, chỉ import `computePreset` và `matchActivePreset` cho 4 presets (7d, 30d, this-month, last-month) — hoặc giữ nguyên `range-picker.tsx` hoàn toàn (không refactor) vì đây không phải là yêu cầu từ user.
>
> **Quyết định cuối:** Giữ nguyên `range-picker.tsx`, không refactor. Dashboard không bị ảnh hưởng. `QuotationDateFilter` tự define logic riêng (chỉ 4 presets). Điều này tránh rủi ro break dashboard.

### 3.3 Tạo `quotation-date-filter.tsx`

```tsx
import { useEffect, useRef, useState } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { computePreset, matchActivePreset } from '@/lib/date-range-presets';
import type { RangePreset } from '@/lib/date-range-presets';

interface QuotationDateFilterProps {
  from: string;  // '' = no date filter
  to: string;    // '' = no date filter
  onChange: (from: string, to: string) => void;
}

const PRESETS: { key: RangePreset; label: string }[] = [
  { key: '7d', label: '7N' },
  { key: '30d', label: '30N' },
  { key: 'this-month', label: 'Tháng này' },
  { key: 'last-month', label: 'Tháng trước' },
];

export function QuotationDateFilter({ from, to, onChange }: QuotationDateFilterProps) {
  const [open, setOpen] = useState(false);
  const [draftFrom, setDraftFrom] = useState(from);
  const [draftTo, setDraftTo] = useState(to);
  const wrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => { setDraftFrom(from); setDraftTo(to); }, [from, to]);

  useEffect(() => {
    if (!open) return;
    function handleClick(e: MouseEvent) {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  const noFilter = from === '' && to === '';
  const activePreset = noFilter ? null : matchActivePreset(from, to);
  const isCustomActive = !noFilter && activePreset === null;

  const pillCls = (active: boolean) =>
    cn(
      'rounded-full border px-3 py-1 text-xs font-medium transition-colors',
      active
        ? 'border-foreground bg-foreground text-background'
        : 'border-border bg-card text-muted-foreground hover:bg-accent',
    );

  return (
    <div className="flex flex-wrap items-center gap-2" ref={wrapRef}>
      <button type="button" onClick={() => onChange('', '')} className={pillCls(noFilter)}>
        Tất cả
      </button>
      {PRESETS.map((p) => (
        <button
          key={p.key}
          type="button"
          onClick={() => { const r = computePreset(p.key); onChange(r.from, r.to); }}
          className={pillCls(activePreset === p.key)}
        >
          {p.label}
        </button>
      ))}
      <div className="relative">
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className={pillCls(isCustomActive)}
        >
          Tuỳ chỉnh ▾
        </button>
        {open && (
          <div className="absolute left-0 top-9 z-20 w-64 rounded-lg border border-border bg-popover p-3 shadow-md">
            <div className="space-y-2">
              <label className="block text-xs text-muted-foreground">
                Từ ngày
                <Input type="date" value={draftFrom} onChange={(e) => setDraftFrom(e.target.value)} className="mt-1" />
              </label>
              <label className="block text-xs text-muted-foreground">
                Đến ngày
                <Input type="date" value={draftTo} onChange={(e) => setDraftTo(e.target.value)} className="mt-1" />
              </label>
              <div className="flex justify-end gap-2 pt-1">
                <Button variant="ghost" size="sm" onClick={() => setOpen(false)}>Huỷ</Button>
                <Button
                  size="sm"
                  disabled={!draftFrom || !draftTo}
                  onClick={() => { onChange(draftFrom, draftTo); setOpen(false); }}
                >
                  Áp dụng
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
```

### 3.4 Update `quotation-list-page.tsx`

- Thêm import: `import { QuotationDateFilter } from './components/quotation-date-filter';`
- Xóa 2 block `<Input type="date" ... />` (aria-label "Từ ngày" và "Đến ngày")
- Thay bằng:
  ```tsx
  <QuotationDateFilter
    from={fromDate}
    to={toDate}
    onChange={(f, t) => {
      setFromDate(f);
      setToDate(t);
      if (page !== 1) setPage(1);
    }}
  />
  ```

## Verification

```bash
cd frontend && npx tsc --noEmit
```

Kiểm tra thủ công:
- Preset "Tất cả" active khi mở trang lần đầu (URL không có `from`/`to`)
- Click "7N" → URL có `from` và `to`, preset "7N" được highlight
- Click "Tất cả" sau đó → URL xóa `from`/`to`, preset "Tất cả" active
- Click "Tuỳ chỉnh" → popup mở; nhập 2 ngày → Áp dụng → URL cập nhật, "Tuỳ chỉnh" active

## Exit Criteria

- `src/lib/date-range-presets.ts` tồn tại và export `RangePreset`, `computePreset`, `matchActivePreset`
- `QuotationDateFilter` component render đúng 6 button (Tất cả + 4 preset + Tuỳ chỉnh)
- 2 `<Input type="date">` đã bị xóa khỏi `quotation-list-page.tsx`
- `RangePicker` dashboard không bị thay đổi behavior (giữ nguyên file nếu chọn không refactor)
- TypeScript compile sạch
