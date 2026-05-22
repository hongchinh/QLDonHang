# Phase 02 — RangePicker shared component

**Status:** [ ] pending
**Complexity:** M

## Objective
Move `RangePicker` từ `features/dashboard/components/` sang `components/ui/`, đồng thời:
- Move `RangePreset` type vào file mới
- Thêm support trạng thái empty (from/to rỗng = không lọc)
- Thêm prop `onClear` để xoá date filter
- Cập nhật imports ở dashboard

## Files
- `frontend/src/components/ui/range-picker.tsx` ← file mới (tạo từ nội dung cũ)
- `frontend/src/features/dashboard/components/range-picker.tsx` ← xoá sau khi cập nhật xong
- `frontend/src/features/dashboard/use-dashboard-params.ts`
- `frontend/src/pages/dashboard-page.tsx`

## Tasks

1. **Tạo `frontend/src/components/ui/range-picker.tsx`** với nội dung dựa trên file cũ, áp dụng các thay đổi sau:

   a. **Move `RangePreset` type** vào đây (không còn import từ `use-dashboard-params`):
   ```ts
   export type RangePreset = 'today' | '7d' | '30d' | 'this-month' | 'last-month';
   ```

   b. **Update interface** — thêm prop `onClear`:
   ```ts
   interface RangePickerProps {
     from: string;
     to: string;
     onChange: (from: string, to: string) => void;
     onPreset?: (preset: RangePreset) => void;
     onClear?: () => void;
   }
   ```

   c. **Empty state** — khi `from` và `to` đều rỗng:
   - Không preset nào được highlight (matchActivePreset trả về null, nhưng nút "Tuỳ chỉnh" cũng không highlight)
   - Không hiển thị text `{from} → {to}` ở cuối
   - Logic: thay đổi điều kiện render của span text thành:
     ```tsx
     {from && to && (
       <span className="ml-1 text-xs text-muted-foreground">
         {from} → {to}
       </span>
     )}
     ```

   d. **Nút "Tuỳ chỉnh" highlight**: chỉ highlight khi `matchPreset == null` VÀ `from && to` đều có giá trị:
   ```tsx
   matchPreset == null && Boolean(from && to)
     ? 'border-foreground bg-foreground text-background'
     : 'border-border bg-card text-muted-foreground hover:bg-accent'
   ```

   e. **Nút xoá "×"** — hiện khi `(from || to)` và `onClear` được truyền:
   ```tsx
   {(from || to) && onClear && (
     <button
       type="button"
       onClick={onClear}
       className="rounded-full border border-border bg-card px-2 py-1 text-xs text-muted-foreground hover:bg-accent"
       aria-label="Xoá bộ lọc ngày"
     >
       ×
     </button>
   )}
   ```
   Đặt sau các preset buttons, trước nút "Tuỳ chỉnh".

   f. Giữ nguyên toàn bộ logic còn lại: `computePreset`, `matchActivePreset`, `handlePresetClick`, `handleApply`, popover "Tuỳ chỉnh".

2. **`use-dashboard-params.ts`**: thay dòng import `RangePreset`:
   ```ts
   // Trước:
   export type RangePreset = 'today' | '7d' | '30d' | 'this-month' | 'last-month';
   // Sau:
   export type { RangePreset } from '@/components/ui/range-picker';
   ```
   (Re-export để giữ backward compat nếu có nơi nào đang import từ đây)

3. **`dashboard-page.tsx`**: đổi import `RangePicker`:
   ```ts
   // Trước:
   import { RangePicker } from '@/features/dashboard/components/range-picker';
   // Sau:
   import { RangePicker } from '@/components/ui/range-picker';
   ```

4. **Xoá** `frontend/src/features/dashboard/components/range-picker.tsx`.

## Verification
```bash
cd frontend
npx tsc --noEmit
```
TypeScript build phải pass không có lỗi liên quan đến `RangePicker` hay `RangePreset`.

## Exit Criteria
- `components/ui/range-picker.tsx` tồn tại với `RangePreset` export và `onClear` prop
- Empty state (from/to rỗng): không hiện text, không highlight Tuỳ chỉnh
- Dashboard vẫn import được `RangePicker` và `RangePreset`
- File cũ đã xoá
- `tsc --noEmit` pass
