# Phase 02 — Apply table primitive change + conditional Card fix + verification

**Status:** [x] complete
**Complexity:** S

**Card-fix decision from Phase 01:** `ON` (initial), then **downgraded to `OFF`** during Phase 02 task 3 after discovering that the real layout wraps each table in `<CardContent className="p-4">`. The `p-4` padding insets the table from the Card's rounded corners, so the "square blue corner artifact" the mockup demonstrated does not actually occur. Adding `overflow-hidden` would be a no-op. User confirmed `Skip Card fix` when notified.

## Objective
Sửa shadcn `<Table>` primitive để header band dùng `#005bac` + chữ trắng `font-semibold`. Conditional: nếu Phase 01 chốt Card-fix `ON` → thêm `overflow-hidden` cho `<Card>` của 3 trang list. Verify static checks + manual UI.

## Files
- `frontend/src/components/ui/table.tsx` (modify — luôn sửa)
- `frontend/src/pages/customers/customer-list-page.tsx` (modify — chỉ nếu Card-fix `ON`)
- `frontend/src/pages/products/product-list-page.tsx` (modify — chỉ nếu Card-fix `ON`)
- `frontend/src/pages/quotations/quotation-list-page.tsx` (modify — chỉ nếu Card-fix `ON`)

## Tasks

### 1. Safety grep (luôn chạy)
Grep tìm các nơi gọi `<TableHead>` / `<TableHeader>` với prop `className` có chứa `text-` hoặc `bg-`:
```
Grep pattern: 'TableHead[^>]*className' (multiline, glob *.tsx)
Grep pattern: 'TableHeader[^>]*className' (multiline, glob *.tsx)
```
- Mục đích: phát hiện override cũ trên text color/bg để không bị silent conflict.
- Nếu thấy override liên quan màu chữ → log lại trong execution report, KHÔNG sửa trong phase này (out of scope).

### 2. Edit `frontend/src/components/ui/table.tsx` (luôn)

**`TableHeader` (dòng ~14)**: đổi className.
```ts
// before
<thead ref={ref} className={cn('[&_tr]:border-b', className)} {...props} />
// after
<thead ref={ref} className={cn('bg-[#005bac] [&_tr]:border-b-0', className)} {...props} />
```

**`TableHead` (dòng ~42)**: đổi className.
```ts
// before
'h-10 px-2 text-left align-middle font-medium text-muted-foreground [&:has([role=checkbox])]:pr-0'
// after
'h-10 px-2 text-left align-middle font-semibold text-white [&:has([role=checkbox])]:pr-0'
```

KHÔNG đổi `TableBody`, `TableRow`, `TableCell`, `Table` wrapper.

### 3. Conditional Card fix (chỉ chạy nếu Card-fix `ON`)
Trên mỗi file list page:
- `frontend/src/pages/customers/customer-list-page.tsx`
- `frontend/src/pages/products/product-list-page.tsx`
- `frontend/src/pages/quotations/quotation-list-page.tsx`

Mở file, tìm `<Card ...>` đang wrap `<Table>`. Thêm `overflow-hidden` vào className:
```tsx
// before (ví dụ)
<Card>
// after
<Card className="overflow-hidden">
```
Nếu đã có className → merge (e.g. `className="overflow-hidden mt-4"`).

### 4. Static verification (luôn) — chạy ở `frontend/`
```
npm run lint
npm run test
npm run build
```
Kỳ vọng:
- `lint`: 0 error/warning mới.
- `test`: 54/54 pass (không test nào assert màu header).
- `build`: tsc + vite build success. Tailwind tự pick `bg-[#005bac]` từ source.

### 5. Manual UI verification (luôn) — `npm run dev`
- Mở `http://localhost:5173/customers`:
  - Header band: nền `#005bac`, chữ trắng `semibold`, h-10, text-align left.
  - Không có "square blue corner artifact" trên rounded Card (nếu Card-fix `ON`).
  - Hover/sort 1 cột header → icon arrow visible trắng (nếu list có sort).
- Mở `http://localhost:5173/products` → header inherit style giống.
- Mở `http://localhost:5173/quotations` → header inherit style giống.
- Console F12 → không có warning Tailwind / CSS mới.

### 6. (Optional) Lighthouse contrast audit
- DevTools → Lighthouse → Accessibility audit trên `/customers`.
- Header KHÔNG được flag "insufficient contrast". (Kỳ vọng: 5.69:1 pass AA.)

## Verification
- `npm run lint`, `npm run test`, `npm run build` đều green.
- 3 trang list render header brand color đúng visual mockup.
- Nếu Card-fix `ON`: rounded corner clean trên cả 3 trang.
- Console không warning mới.

## Exit Criteria
- File `table.tsx` đã edit đúng task 2.
- Nếu Card-fix `ON`: 3 file list page đã thêm `overflow-hidden`.
- Static checks pass.
- Visual smoke pass trên 3 trang list.
- Phase status → `[x] complete`.
