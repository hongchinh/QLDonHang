# Phase 04 — Frontend

**Status:** [ ] pending
**Complexity:** M

## Objective

Thêm `zaloGroupId` vào customer form + API. Tạo `SendZaloDialog` component. Thêm nút "Gửi Zalo" trên quotation form page với dialog chọn file type.

## Files

- `frontend/src/features/customers/types.ts`
- `frontend/src/features/customers/schema.ts`
- `frontend/src/features/customers/api.ts`
- `frontend/src/pages/customers/customer-form-fields.tsx`
- `frontend/src/features/quotations/api.ts`
- `frontend/src/features/quotations/hooks.ts`
- `frontend/src/components/send-zalo-dialog.tsx` ← new
- `frontend/src/pages/quotations/quotation-form-page.tsx`

---

## Tasks

### Task 1 — Customer types + schema + API

No tests for pure type changes (compile-time verified). Apply all in one commit.

1. **Edit** `frontend/src/features/customers/types.ts`:

   In `Customer` interface, add after `status`:
   ```typescript
   zaloGroupId?: string;
   ```

   In `UpsertCustomerRequest`, add after `status?`:
   ```typescript
   zaloGroupId?: string;
   ```

2. **Edit** `frontend/src/features/customers/schema.ts`:

   In `customerSchema`, add after `status`:
   ```typescript
   zaloGroupId: optionalString(50),
   ```

3. **Edit** `frontend/src/pages/customers/customer-form-fields.tsx`:

   In `FieldProps.name` union type (line ~190), add `| 'zaloGroupId'`.

   In `toFormDefaults`, add:
   ```typescript
   zaloGroupId: customer?.zaloGroupId ?? '',
   ```

   In the form JSX, add after the `Ghi chú` field (before the `showStatusField` block):
   ```tsx
   <Field
     label="Zalo Group ID"
     name="zaloGroupId"
     form={form}
     hint="ID nhóm Zalo để nhận báo giá tự động"
   />
   ```

4. **Verify TypeScript compiles**:
   ```
   cd frontend && npm run build 2>&1 | tail -20
   ```
   Expected: 0 errors.

5. **Commit**:
   ```
   git commit -m "feat: add zaloGroupId to customer form and API types"
   ```

---

### Task 2 — Quotation API sendToZalo

1. **Edit** `frontend/src/features/quotations/api.ts` — add after `downloadExcel`:
   ```typescript
   sendToZalo: (id: string, fileType: 'excel' | 'pdf') =>
     apiPost<void>(`/quotations/${id}/send-zalo`, { fileType }),
   ```

2. **Edit** `frontend/src/features/quotations/hooks.ts` — add after existing hooks (or at end):
   ```typescript
   export function useSendQuotationToZalo() {
     const queryClient = useQueryClient();
     return useMutation({
       mutationFn: ({ id, fileType }: { id: string; fileType: 'excel' | 'pdf' }) =>
         quotationsApi.sendToZalo(id, fileType),
     });
   }
   ```
   Ensure `useMutation` and `useQueryClient` are imported from `@tanstack/react-query`.

3. **Commit**:
   ```
   git commit -m "feat: add sendToZalo API call and useSendQuotationToZalo hook"
   ```

---

### Task 3 — SendZaloDialog component

1. **Create** `frontend/src/components/send-zalo-dialog.tsx`:
   ```tsx
   import { useState } from 'react';
   import {
     Dialog,
     DialogContent,
     DialogHeader,
     DialogTitle,
     DialogFooter,
   } from '@/components/ui/dialog';
   import { Button } from '@/components/ui/button';
   import { Label } from '@/components/ui/label';
   import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
   import { ButtonLoader } from '@/components/ui/button-loader';

   interface SendZaloDialogProps {
     open: boolean;
     onClose: () => void;
     onConfirm: (fileType: 'excel' | 'pdf') => Promise<void>;
     zaloGroupId?: string;
     quotationCode: string;
     isPending: boolean;
   }

   export function SendZaloDialog({
     open,
     onClose,
     onConfirm,
     zaloGroupId,
     quotationCode,
     isPending,
   }: SendZaloDialogProps) {
     const [fileType, setFileType] = useState<'excel' | 'pdf'>('excel');

     async function handleConfirm() {
       await onConfirm(fileType);
     }

     return (
       <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
         <DialogContent className="sm:max-w-sm">
           <DialogHeader>
             <DialogTitle>Gửi báo giá vào nhóm Zalo</DialogTitle>
           </DialogHeader>

           {!zaloGroupId ? (
             <p className="text-sm text-destructive">
               Khách hàng chưa có Zalo Group ID. Vui lòng cập nhật hồ sơ khách hàng trước.
             </p>
           ) : (
             <div className="space-y-4">
               <div className="space-y-2">
                 <Label>Chọn định dạng file</Label>
                 <RadioGroup
                   value={fileType}
                   onValueChange={(v) => setFileType(v as 'excel' | 'pdf')}
                   className="flex gap-4"
                 >
                   <div className="flex items-center gap-2">
                     <RadioGroupItem value="excel" id="ft-excel" />
                     <Label htmlFor="ft-excel">Excel (.xlsx)</Label>
                   </div>
                   <div className="flex items-center gap-2">
                     <RadioGroupItem value="pdf" id="ft-pdf" />
                     <Label htmlFor="ft-pdf">PDF (.pdf)</Label>
                   </div>
                 </RadioGroup>
               </div>
               <p className="text-sm text-muted-foreground">
                 Gửi đến nhóm: <span className="font-mono">{zaloGroupId}</span>
               </p>
             </div>
           )}

           <DialogFooter>
             <Button variant="outline" onClick={onClose} disabled={isPending}>
               Hủy
             </Button>
             {zaloGroupId && (
               <ButtonLoader loading={isPending} onClick={handleConfirm}>
                 Gửi ngay
               </ButtonLoader>
             )}
           </DialogFooter>
         </DialogContent>
       </Dialog>
     );
   }
   ```

   **Note on `RadioGroup`:** The project uses Radix UI. If `RadioGroup` is not yet in `@/components/ui/`, create `frontend/src/components/ui/radio-group.tsx` following the same pattern as other Radix UI components in the project:
   ```tsx
   import * as RadioGroupPrimitive from '@radix-ui/react-radio-group';
   import { cn } from '@/lib/utils';

   export const RadioGroup = RadioGroupPrimitive.Root;

   export function RadioGroupItem({ className, ...props }: React.ComponentProps<typeof RadioGroupPrimitive.Item>) {
     return (
       <RadioGroupPrimitive.Item
         className={cn(
           'aspect-square h-4 w-4 rounded-full border border-primary text-primary ring-offset-background focus:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50',
           className,
         )}
         {...props}
       >
         <RadioGroupPrimitive.Indicator className="flex items-center justify-center">
           <div className="h-2.5 w-2.5 rounded-full bg-current" />
         </RadioGroupPrimitive.Indicator>
       </RadioGroupPrimitive.Item>
     );
   }
   ```
   Install if missing: `cd frontend && npm install @radix-ui/react-radio-group`

2. **Build check**:
   ```
   cd frontend && npm run build 2>&1 | tail -20
   ```

3. **Commit**:
   ```
   git commit -m "feat: add SendZaloDialog component"
   ```

---

### Task 4 — Integrate "Gửi Zalo" button into quotation form page

This task modifies `quotation-form-page.tsx`. The key areas to change:

1. **Add to imports** at top of file:
   ```tsx
   import { Send as ZaloSend } from 'lucide-react';
   import { useSendQuotationToZalo } from '@/features/quotations/hooks';
   import { SendZaloDialog } from '@/components/send-zalo-dialog';
   ```

2. **Extend `QuotationButtonAction` type** (line ~64):
   ```typescript
   type QuotationButtonAction = 'send' | 'confirm' | 'cancel' | 'accounting-confirm' | 'clone' | 'print' | 'excel' | 'send-zalo';
   ```

3. **In `QuotationFormPage` outer component** — the `onSendZalo` handler needs the `initialSelectedCustomer` data. Pass `zaloGroupId` and `isPending` down via `InnerProps`.

   In `QuotationFormPage`:
   ```tsx
   // Add to InnerProps interface:
   onSendZalo: (fileType: 'excel' | 'pdf') => Promise<void>;
   sendZaloIsPending: boolean;
   customerZaloGroupId?: string;

   // In the outer component, add:
   const sendZalo = useSendQuotationToZalo();

   // Pass to QuotationFormInner:
   onSendZalo={async (fileType) => {
     if (!id || !isEdit) return;
     try {
       await sendZalo.mutateAsync({ id, fileType });
       toast({ variant: 'success', title: 'Đã gửi file vào nhóm Zalo' });
     } catch (err) {
       toast({ variant: 'destructive', title: 'Gửi Zalo thất bại', description: getErrorMessage(err) });
     }
   }}
   sendZaloIsPending={sendZalo.isPending}
   customerZaloGroupId={initialSelectedCustomer?.zaloGroupId}
   ```

4. **In `QuotationFormInner`**:

   Destructure new props:
   ```tsx
   function QuotationFormInner({
     ...existing,
     onSendZalo,
     sendZaloIsPending,
     customerZaloGroupId,
   }: InnerProps) {
   ```

   Add state for dialog (no separate mutation instance needed — `isPending` comes from outer):
   ```tsx
   const [sendZaloOpen, setSendZaloOpen] = useState(false);
   ```

   Add "Gửi Zalo" button in the action button area (next to the Excel/PDF buttons — search for `onDownloadExcel` call in the JSX, it's in the button toolbar). Insert after the Excel button:
   ```tsx
   {isEdit && (
     <Button
       type="button"
       variant="outline"
       size="sm"
       disabled={isSubmitBusy}
       onClick={() => setSendZaloOpen(true)}
     >
       <ZaloSend className="h-4 w-4 mr-1" />
       Gửi Zalo
     </Button>
   )}
   ```

   Add `SendZaloDialog` before the closing tag of the main return:
   ```tsx
   <SendZaloDialog
     open={sendZaloOpen}
     onClose={() => setSendZaloOpen(false)}
     onConfirm={async (fileType) => {
       await onSendZalo(fileType);
       setSendZaloOpen(false);
     }}
     zaloGroupId={customerZaloGroupId}
     quotationCode={initial?.code ?? ''}
     isPending={sendZaloIsPending}
   />
   ```

5. **Build check**:
   ```
   cd frontend && npm run build 2>&1 | tail -20
   ```
   Expected: 0 errors.

6. **Verify visually** — Start dev server and open a quotation edit page. Confirm:
   - "Gửi Zalo" button appears in the action bar
   - Clicking it opens the dialog
   - If customer has no `ZaloGroupId`: warning message shown, no send button
   - If customer has `ZaloGroupId`: radio options shown, group ID displayed, "Gửi ngay" button active

   ```
   cd frontend && npm run dev
   # Open http://localhost:5173 and navigate to a quotation edit page
   ```

7. **Commit**:
   ```
   git commit -m "feat: add Gửi Zalo button and SendZaloDialog to quotation form"
   ```

---

## Verification

```bash
# TypeScript build
cd frontend && npm run build

# Dev server (manual UI test)
cd frontend && npm run dev
```

Manual checks:
- [ ] Customer form shows "Zalo Group ID" field (optional)
- [ ] Saving customer with `zaloGroupId` persists the value
- [ ] Quotation form shows "Gửi Zalo" button only for edit mode
- [ ] Dialog opens on click
- [ ] Dialog shows warning when customer has no ZaloGroupId
- [ ] Dialog shows radio + group ID when ZaloGroupId present
- [ ] Success toast shown after send (with fake backend response if testing offline)
- [ ] Error toast shown on failure

## Exit Criteria

- `npm run build` passes with 0 TypeScript errors
- "Gửi Zalo" button visible on quotation edit pages
- `SendZaloDialog` renders correctly for both states (no groupId / has groupId)
- Customer form field "Zalo Group ID" present and persists through save
