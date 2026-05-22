# Phase 09 — Frontend Form Dropdowns

**Status:** [ ] pending
**Complexity:** S

## Objective

Chuyển button "Excel" và "In" trong `quotation-form-page.tsx` thành `DropdownMenu` với 3 option: Báo giá / Biên bản (có tiền) / Biên bản (không tiền).

## Files

- `frontend/src/pages/quotations/quotation-form-page.tsx`

## Tasks

### Task 9.0 — Mở rộng `QuotationButtonAction` type

1. **Mở** `frontend/src/pages/quotations/quotation-form-page.tsx`

2. **Tìm dòng 64** (khai báo `QuotationButtonAction`):

   ```typescript
   type QuotationButtonAction = 'send' | 'confirm' | 'cancel' | 'accounting-confirm' | 'clone' | 'print' | 'excel';
   ```

3. **Thêm 4 member mới**:

   ```typescript
   type QuotationButtonAction =
     | 'send' | 'confirm' | 'cancel' | 'accounting-confirm' | 'clone'
     | 'print' | 'excel'
     | 'excel-handover-price' | 'excel-handover-no-price'
     | 'print-handover-price' | 'print-handover-no-price';
   ```

4. **Type check:**
   ```
   cd frontend && npx tsc --noEmit
   ```
   Expected: 0 errors mới (type mở rộng là backward-compatible).

### Task 9.1 — Mở rộng `InnerProps` interface

1. **Mở** `frontend/src/pages/quotations/quotation-form-page.tsx`

2. **Tìm `interface InnerProps`** (khoảng dòng 163) và **thêm 4 callback mới**:

   ```typescript
   interface InnerProps {
     // ... existing props ...
     onPrint: () => Promise<void>;
     onDownloadExcel: () => Promise<void>;
     // Thêm mới:
     onPrintHandoverWithPrice: () => Promise<void>;
     onPrintHandoverNoPrice: () => Promise<void>;
     onDownloadHandoverWithPriceExcel: () => Promise<void>;
     onDownloadHandoverNoPriceExcel: () => Promise<void>;
   }
   ```

### Task 9.2 — Thêm handlers trong outer component

1. **Tìm section** `onDownloadExcel={async () => {` (khoảng dòng 136) trong outer component

2. **Thêm 4 handler mới** sau handler `onDownloadExcel`:

   ```typescript
   onPrintHandoverWithPrice={async () => {
     if (!id || !isEdit || !quotation) return;
     try {
       const blob = await quotationsApi.downloadHandoverWithPricePdf(id);
       const url = URL.createObjectURL(blob);
       window.open(url, '_blank', 'noopener,noreferrer');
       window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
     } catch (err) {
       toast({ variant: 'destructive', title: 'Không mở được PDF', description: getErrorMessage(err) });
     }
   }}
   onPrintHandoverNoPrice={async () => {
     if (!id || !isEdit || !quotation) return;
     try {
       const blob = await quotationsApi.downloadHandoverNoPricePdf(id);
       const url = URL.createObjectURL(blob);
       window.open(url, '_blank', 'noopener,noreferrer');
       window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
     } catch (err) {
       toast({ variant: 'destructive', title: 'Không mở được PDF', description: getErrorMessage(err) });
     }
   }}
   onDownloadHandoverWithPriceExcel={async () => {
     if (!id || !isEdit || !quotation) return;
     try {
       const blob = await quotationsApi.downloadHandoverWithPriceExcel(id);
       const url = URL.createObjectURL(blob);
       const a = document.createElement('a');
       a.href = url;
       a.download = `BieuBanBanGiao_${quotation.code}.xlsx`;
       document.body.appendChild(a);
       a.click();
       a.remove();
       URL.revokeObjectURL(url);
     } catch (err) {
       toast({ variant: 'destructive', title: 'Không tải được Excel', description: getErrorMessage(err) });
     }
   }}
   onDownloadHandoverNoPriceExcel={async () => {
     if (!id || !isEdit || !quotation) return;
     try {
       const blob = await quotationsApi.downloadHandoverNoPriceExcel(id);
       const url = URL.createObjectURL(blob);
       const a = document.createElement('a');
       a.href = url;
       a.download = `BieuBanBanGiao_${quotation.code}.xlsx`;
       document.body.appendChild(a);
       a.click();
       a.remove();
       URL.revokeObjectURL(url);
     } catch (err) {
       toast({ variant: 'destructive', title: 'Không tải được Excel', description: getErrorMessage(err) });
     }
   }}
   ```

### Task 9.3 — Thêm DropdownMenu imports

1. **Thêm imports** cho Radix/shadcn DropdownMenu ở đầu file (kiểm tra đã có chưa):

   ```typescript
   import {
     DropdownMenu,
     DropdownMenuContent,
     DropdownMenuItem,
     DropdownMenuTrigger,
   } from '@/components/ui/dropdown-menu';
   import { ChevronDown } from 'lucide-react';
   ```

   **Lưu ý:** Kiểm tra `frontend/src/components/ui/dropdown-menu.tsx` đã tồn tại chưa. Nếu chưa, cài với shadcn CLI: `npx shadcn-ui@latest add dropdown-menu`

### Task 9.4 — Chuyển button "Excel" thành DropdownMenu

1. **Tìm đoạn JSX** button "Excel" (khoảng dòng 513-522):

   ```jsx
   <Button
     variant="outline"
     size="sm"
     onClick={() => runButtonAction('excel', onDownloadExcel)}
     disabled={isSubmitBusy}
     aria-busy={pendingButtonAction === 'excel'}
   >
     {pendingButtonAction === 'excel' ? <ButtonLoader className="mr-2" /> : <FileSpreadsheet className="mr-2 h-4 w-4 text-emerald-700" />}
     {pendingButtonAction === 'excel' ? 'Đang xuất...' : 'Excel'}
   </Button>
   ```

2. **Thay thế** bằng DropdownMenu:

   ```jsx
   <DropdownMenu>
     <DropdownMenuTrigger asChild>
       <Button
         variant="outline"
         size="sm"
         disabled={isSubmitBusy}
       >
         <FileSpreadsheet className="mr-2 h-4 w-4 text-emerald-700" />
         Excel
         <ChevronDown className="ml-1 h-3 w-3" />
       </Button>
     </DropdownMenuTrigger>
     <DropdownMenuContent align="end">
       <DropdownMenuItem onClick={() => runButtonAction('excel', onDownloadExcel)}>
         Báo giá
       </DropdownMenuItem>
       <DropdownMenuItem onClick={() => runButtonAction('excel-handover-price', onDownloadHandoverWithPriceExcel)}>
         Biên bản bàn giao (có tiền)
       </DropdownMenuItem>
       <DropdownMenuItem onClick={() => runButtonAction('excel-handover-no-price', onDownloadHandoverNoPriceExcel)}>
         Biên bản bàn giao (không tiền)
       </DropdownMenuItem>
     </DropdownMenuContent>
   </DropdownMenu>
   ```

### Task 9.5 — Chuyển button "In" thành DropdownMenu

1. **Tìm đoạn JSX** button "In" (khoảng dòng 523-532)

2. **Thay thế** bằng DropdownMenu:

   ```jsx
   <DropdownMenu>
     <DropdownMenuTrigger asChild>
       <Button
         variant="outline"
         size="sm"
         disabled={isSubmitBusy}
       >
         <Printer className="mr-2 h-4 w-4 text-indigo-600" />
         In
         <ChevronDown className="ml-1 h-3 w-3" />
       </Button>
     </DropdownMenuTrigger>
     <DropdownMenuContent align="end">
       <DropdownMenuItem onClick={() => runButtonAction('print', onPrint)}>
         Báo giá
       </DropdownMenuItem>
       <DropdownMenuItem onClick={() => runButtonAction('print-handover-price', onPrintHandoverWithPrice)}>
         Biên bản bàn giao (có tiền)
       </DropdownMenuItem>
       <DropdownMenuItem onClick={() => runButtonAction('print-handover-no-price', onPrintHandoverNoPrice)}>
         Biên bản bàn giao (không tiền)
       </DropdownMenuItem>
     </DropdownMenuContent>
   </DropdownMenu>
   ```

   **Lưu ý:** `runButtonAction` nhận key để track `pendingButtonAction`. Nếu cần loading state trong dropdown, kiểm tra function `runButtonAction` có hỗ trợ các key mới không (thường là string type-safe).

### Task 9.6 — Destructure props mới trong `QuotationFormInner`

1. **Tìm `function QuotationFormInner({`** (khoảng dòng 176)

2. **Thêm 4 prop mới** vào destructuring:

   ```typescript
   function QuotationFormInner({
     // ... existing ...
     onPrint,
     onDownloadExcel,
     onPrintHandoverWithPrice,
     onPrintHandoverNoPrice,
     onDownloadHandoverWithPriceExcel,
     onDownloadHandoverNoPriceExcel,
   }: InnerProps) {
   ```

3. **Type check:**
   ```
   cd frontend && npx tsc --noEmit
   ```
   Expected: 0 errors.

4. **Commit:**
   ```
   git commit -m "feat: convert Excel/Print buttons to dropdowns for báo giá and biên bản"
   ```

## Verification

- `npx tsc --noEmit` → 0 errors
- Trong trình duyệt: button "Excel" và "In" hiển thị dropdown với 3 option
- Click "Báo giá" → download file báo giá (hành vi cũ)
- Click "Biên bản bàn giao (có tiền)" → download biên bản Excel / mở PDF biên bản có tiền
- Click "Biên bản bàn giao (không tiền)" → download biên bản Excel / mở PDF biên bản không tiền

## Exit Criteria

- 2 button đều là DropdownMenu với 3 option
- 4 handler mới đúng tên file (prefix `BieuBanBanGiao_`)
- Hành vi cũ ("Báo giá") không bị thay đổi
- TypeScript type check xanh
