# Phase 02 — Shared DateRangePicker component

**Status:** [ ] pending
**Complexity:** S

## Objective

Create a reusable `DateRangePicker` component at `frontend/src/components/ui/date-range-picker.tsx`. It renders preset chip buttons (Hôm nay / 7N / 30N / Tháng này / Tháng trước) plus a "Tuỳ chỉnh" dropdown with two date inputs. Visual style matches the existing dashboard `RangePicker`.

This component is self-contained: no dependency on dashboard-specific modules. It will be used by the quotation list page and is reusable elsewhere.

## Files

- `frontend/src/components/ui/date-range-picker.tsx` *(new)*

## Tasks

1. **Create `date-range-picker.tsx`** with the following structure:

   ```tsx
   import { useEffect, useRef, useState } from 'react';
   import { cn } from '@/lib/utils';
   import { Button } from '@/components/ui/button';
   import { Input } from '@/components/ui/input';

   export type DateRangePreset = 'today' | '7d' | '30d' | 'this-month' | 'last-month';

   interface DateRangePickerProps {
     from: string;   // 'YYYY-MM-DD' or ''
     to: string;     // 'YYYY-MM-DD' or ''
     onChange: (from: string, to: string) => void;
   }

   const PRESETS: { key: DateRangePreset; label: string }[] = [
     { key: 'today',      label: 'Hôm nay' },
     { key: '7d',         label: '7N' },
     { key: '30d',        label: '30N' },
     { key: 'this-month', label: 'Tháng này' },
     { key: 'last-month', label: 'Tháng trước' },
   ];
   ```

2. **Implement `formatDateYmd`** helper (private, not exported) in the same file:
   ```ts
   function formatDateYmd(d: Date): string {
     return d.toISOString().slice(0, 10);
   }
   ```

3. **Implement `computePreset(preset: DateRangePreset): { from: string; to: string }`** — same logic as dashboard's `computePreset`:
   - `today`: start = end = today
   - `7d`: start = today − 6 days, end = today
   - `30d`: start = today − 29 days, end = today
   - `this-month`: start = first day of current month, end = today
   - `last-month`: start = first day of previous month, end = last day of previous month

4. **Implement `matchActivePreset(from, to): DateRangePreset | null`** — iterate `PRESETS`, compare `computePreset(p.key)` against `{ from, to }`.

5. **Implement `DateRangePicker` component**:
   - Render preset chip buttons using the same `cn(...)` class pattern as dashboard `RangePicker` (active = dark fill, inactive = outline with hover)
   - Render "Tuỳ chỉnh ▾" chip button; clicking it opens a popover (local `open` state + `wrapRef` click-outside handler)
   - Popover contains: "Từ ngày" date input, "Đến ngày" date input, "Huỷ" and "Áp dụng" buttons
   - "Áp dụng" calls `onChange(draftFrom, draftTo)` only if both values are non-empty
   - Display current range as `{from} → {to}` label to the right of the chips

## Verification

```bash
cd frontend && npx tsc --noEmit
```

No TypeScript errors in the new file.

## Exit Criteria

- `date-range-picker.tsx` exists at `frontend/src/components/ui/`
- Exports: `DateRangePicker` (component), `DateRangePreset` (type)
- `npx tsc --noEmit` passes
