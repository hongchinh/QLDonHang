# Phase 04 — TotalsPanel stretch

**Status:** [x] complete
**Complexity:** S

## Objective
Sửa `TotalsPanel` để card có thể stretch full chiều cao theo CSS Grid row, bỏ `sticky top-4` (không còn cần vì layout mới ép TotalsPanel cùng row với "Thông tin chung"). Nhóm "Tổng giá vốn / Lợi nhuận gộp" neo đáy bằng `mt-auto` để filler khoảng trống khi card cao hơn nội dung tối thiểu.

## Files
- `frontend/src/pages/quotations/components/totals-panel.tsx`

## Tasks

1. Mở `frontend/src/pages/quotations/components/totals-panel.tsx`.

2. Sửa `<Card>`:
   - Từ `<Card className="sticky top-4">` → `<Card className="h-full flex flex-col">`.

3. Sửa `<CardContent>`:
   - Từ `<CardContent className="space-y-3">` → `<CardContent className="space-y-3 flex-1 flex flex-col">`.

4. Bọc 2 dòng cuối ("Tổng giá vốn" + "Lợi nhuận gộp") vào 1 div neo đáy:
   ```tsx
   {/* Thay đoạn này: */}
   <div className="my-2 border-t" />
   <Row label="Tổng giá vốn" value={fmt.format(totals.totalCost)} muted />
   <Row label="Lợi nhuận gộp" value={fmt.format(totals.grossProfit)} muted />

   {/* Thành: */}
   <div className="mt-auto space-y-3">
     <div className="border-t" />
     <Row label="Tổng giá vốn" value={fmt.format(totals.totalCost)} muted />
     <Row label="Lợi nhuận gộp" value={fmt.format(totals.grossProfit)} muted />
   </div>
   ```
   Lưu ý: bỏ `my-2` của `border-t` vì wrapper đã có `space-y-3` xử lý spacing.

5. Save file.

## Verification
- Chạy `npm run lint` ở `frontend/` — không lỗi mới.
- Chạy `npm run build` — pass.
- Manual ở Phase 06: card có chiều cao bằng card "Thông tin chung" trên màn ≥1024px; phần "Tổng giá vốn / Lợi nhuận gộp" neo đáy.

## Exit Criteria
- File compile sạch.
- `<Card>` không còn `sticky top-4`, có `h-full flex flex-col`.
- `<CardContent>` có `flex-1 flex flex-col`.
- Nhóm chi phí cuối bọc trong `<div className="mt-auto space-y-3">`.
