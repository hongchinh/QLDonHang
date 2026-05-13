# Phase 05 — Layout restructure trong quotation-form-page

**Status:** [x] complete
**Complexity:** S

## Objective
Restructure layout JSX của `<form>` trong `QuotationFormInner`: top row 2 cột `[Thông tin chung | Tổng cộng]` (grid 1fr/320px), Chi tiết hàng hóa full-width bên dưới, error + submit buttons dưới cùng.

## Files
- `frontend/src/pages/quotations/quotation-form-page.tsx`

## Tasks

1. Mở `frontend/src/pages/quotations/quotation-form-page.tsx`.

2. Tìm block JSX hiện tại (dòng ~243):
   ```jsx
   <form onSubmit={form.handleSubmit(onSubmit)} className="grid gap-4 lg:grid-cols-[1fr_320px]">
     <div className="space-y-4">
       <Card>...Thông tin chung...</Card>
       <Card>...Chi tiết hàng hóa...</Card>
       {hasSubmitError && ...}
       <div className="flex justify-end gap-2">...submit buttons...</div>
     </div>
     <div>
       <TotalsPanel ... />
     </div>
   </form>
   ```

3. Thay bằng:
   ```jsx
   <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
     <div className="grid gap-4 lg:grid-cols-[1fr_320px] items-stretch">
       <Card>
         <CardHeader><CardTitle>Thông tin chung</CardTitle></CardHeader>
         <CardContent className="space-y-3">
           {/* ...giữ nguyên toàn bộ nội dung Thông tin chung... */}
         </CardContent>
       </Card>

       <TotalsPanel lines={lineLikes} header={header} onHeaderChange={onHeaderChange} />
     </div>

     <Card>
       <CardHeader><CardTitle>Chi tiết hàng hóa</CardTitle></CardHeader>
       <CardContent>
         <LineItemsGrid form={form} />
         {form.formState.errors.lines && (
           <p className="mt-2 text-sm text-destructive">
             {String((form.formState.errors.lines as { message?: string }).message ?? 'Báo giá chưa hợp lệ.')}
           </p>
         )}
       </CardContent>
     </Card>

     {hasSubmitError && (
       <div className="rounded-md border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
         {submitError}
       </div>
     )}

     <div className="flex justify-end gap-2">
       <Button type="button" variant="outline" asChild>
         <Link to="/quotations">Hủy</Link>
       </Button>
       <Button type="submit" disabled={submitting}>
         {submitting ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
       </Button>
     </div>
   </form>
   ```
   Lưu ý:
   - `items-stretch` thêm vào grid wrapper để chắc chắn 2 card stretch theo row (mặc định grid đã stretch nhưng để rõ ý).
   - Class form ngoài cùng đổi từ `grid gap-4 lg:grid-cols-[1fr_320px]` → `space-y-4` để row Chi tiết hàng hóa + error + buttons xếp dọc.
   - **Không** đổi nội dung bên trong card "Thông tin chung" — copy nguyên trạng (4 block `form-inline-grid`).

4. Kiểm tra `CustomerQuickAddDialog` ở cuối component vẫn render — không di chuyển nó.

5. Save file.

## Verification
- Chạy `npm run lint` ở `frontend/` — không lỗi mới.
- Chạy `npm run build` — pass.
- Manual ở Phase 06: top row 2 cột, card Tổng cộng cao bằng Thông tin chung, Chi tiết hàng hóa full-width.

## Exit Criteria
- File compile sạch.
- Cấu trúc JSX khớp spec: `<form>` → `<div grid>...</div>` (2 cards top) → `<Card>` (Chi tiết) → error → submit buttons.
- `TotalsPanel` được render trong grid top-row (không trong cột riêng phía dưới).
