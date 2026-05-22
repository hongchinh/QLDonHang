# Phase 08 — Frontend Form Page Buttons

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm nút "KT xác nhận" và logic cancel từ `AccountingConfirmed` vào `quotation-form-page.tsx`. Sử dụng `<Can>` component để ẩn/hiện theo permission.

## Files

- `frontend/src/pages/quotations/quotation-form-page.tsx`

## Tasks

1. **Import thêm** icon và `useAuthStore` hoặc `Can` component (đã import sẵn):
   - Thêm `BadgeCheck` (hoặc icon phù hợp, ví dụ `CheckSquare`) từ `lucide-react` cho nút KT xác nhận
   - `Can` đã được import sẵn trong file

2. **`QuotationButtonAction` type** — thêm `'accounting-confirm'`:
   ```typescript
   type QuotationButtonAction = 'send' | 'confirm' | 'cancel' | 'accounting-confirm' | 'clone' | 'print' | 'excel';
   ```

3. **`actionLabel` helper** (tìm hàm này trong file hoặc inline) — thêm case:
   ```typescript
   case 'AccountingConfirm': return 'Kế toán xác nhận';
   ```

4. **Render nút "KT xác nhận"** — trong vùng action buttons của form (cạnh các nút Send/Confirm/Cancel hiện tại), thêm:
   ```tsx
   {status === 'Confirmed' && (
     <Can permission="quotations.accounting_confirm">
       <ConfirmDialog
         trigger={
           <Button variant="default" disabled={isSubmitBusy}>
             <BadgeCheck className="mr-2 h-4 w-4" />
             KT xác nhận
           </Button>
         }
         title="Xác nhận kế toán đã nhận tiền?"
         description={`Báo giá ${initial?.code} sẽ chuyển sang trạng thái "KT xác nhận". Thao tác này không thể hoàn tác trực tiếp.`}
         onConfirm={() => {
           setPendingButtonAction('accounting-confirm');
           void onTransition('AccountingConfirm').finally(() =>
             setPendingButtonAction(null)
           );
         }}
       />
     </Can>
   )}
   ```

5. **Nút "Hủy" từ `AccountingConfirmed`** — hiện tại nút Cancel đã có trong form. Kiểm tra logic render nút Cancel:
   - Nếu logic hiện tại dùng `status !== 'Cancelled'` để hiện nút Cancel, thêm điều kiện permission gate cho `AccountingConfirmed`:
   ```tsx
   {status === 'AccountingConfirmed' && (
     <Can permission="quotations.cancel_accounting_confirmed">
       {/* nút Cancel hiện tại */}
     </Can>
   )}
   ```
   - Xem kỹ logic render nút Cancel hiện tại trong file để đặt điều kiện đúng chỗ. Không duplicate — chỉ wrap thêm `<Can>` cho case `AccountingConfirmed`.

6. **Hiển thị `accountingConfirmedAt`** trong form (view mode) — tương tự `revenueDateText` hiện đang dùng `confirmedAt`. Thêm dòng hiển thị:
   ```tsx
   {initial?.accountingConfirmedAt && (
     <p className="text-xs text-muted-foreground">
       KT xác nhận: {formatRevenueDate(initial.accountingConfirmedAt)}
       {initial.accountingConfirmedByName && ` bởi ${initial.accountingConfirmedByName}`}
     </p>
   )}
   ```
   Đặt cạnh chỗ hiển thị `revenueDateText`.

## Verification

```bash
cd frontend && npm run build
```

Manual smoke (dev server):
- Login ACCOUNTANT → mở báo giá `Confirmed` → thấy nút "KT xác nhận"
- Login SALES → mở cùng báo giá → KHÔNG thấy nút "KT xác nhận"
- Login ADMIN → mở báo giá `AccountingConfirmed` → thấy nút "Hủy"

## Exit Criteria

- Frontend build thành công, không TypeScript error
- Nút "KT xác nhận" chỉ hiện khi `status === 'Confirmed'` và user có `quotations.accounting_confirm`
- Nút "Hủy" khi `status === 'AccountingConfirmed'` chỉ hiện khi user có `quotations.cancel_accounting_confirmed`
- `accountingConfirmedAt` và `accountingConfirmedByName` hiện trong form view nếu có giá trị
