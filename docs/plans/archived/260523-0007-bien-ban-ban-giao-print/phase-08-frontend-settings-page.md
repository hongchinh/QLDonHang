# Phase 08 — Frontend Settings Page

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm 2 Card upload template mới vào `my-quotation-settings-page.tsx` cho biên bản bàn giao có tiền và không tiền, reuse pattern của Card báo giá hiện có.

## Files

- `frontend/src/pages/settings/my-quotation-settings-page.tsx`

## Tasks

### Task 8.1 — Thêm component `HandoverTemplateCard`

1. **Mở** `frontend/src/pages/settings/my-quotation-settings-page.tsx`

2. **Thêm import** cho hooks và type mới:

   ```typescript
   import {
     useDeleteTemplate,
     useMySettings,
     useUploadTemplate,
     useUploadHandoverTemplate,
     useDeleteHandoverTemplate,
   } from '@/features/me-settings/hooks';
   import { meSettingsApi, HandoverTemplateType } from '@/features/me-settings/api';
   ```

3. **Thêm component `HandoverTemplateCard`** trước `QuotationSettingsTabContent`. Component này nhận props `type` và `title`:

   ```typescript
   interface HandoverTemplateCardProps {
     type: HandoverTemplateType;
     title: string;
     description: string;
     settings: ReturnType<typeof useMySettings>['data'];
   }

   function HandoverTemplateCard({ type, title, description, settings }: HandoverTemplateCardProps) {
     const upload = useUploadHandoverTemplate(type);
     const remove = useDeleteHandoverTemplate(type);
     const inputRef = useRef<HTMLInputElement>(null);
     const [downloading, setDownloading] = useState(false);

     const isWithPrice: boolean = type === 'handover-with-price';
     const templateFileName = isWithPrice
       ? settings?.handoverWithPriceTemplateFileName
       : settings?.handoverNoPriceTemplateFileName;
     const templateOriginalName = isWithPrice
       ? settings?.handoverWithPriceTemplateOriginalName
       : settings?.handoverNoPriceTemplateOriginalName;
     const templateUploadedAt = isWithPrice
       ? settings?.handoverWithPriceTemplateUploadedAt
       : settings?.handoverNoPriceTemplateUploadedAt;

     const hasTemplate = !!templateFileName;

     const handlePick = () => inputRef.current?.click();

     const handleUpload = async (file: File) => {
       if (file.size > MAX_BYTES) {
         toast({ title: 'File quá lớn', description: 'Tối đa 5MB.', variant: 'destructive' });
         return;
       }
       try {
         await upload.mutateAsync(file);
         toast({ title: 'Đã tải template lên', description: file.name });
       } catch (err) {
         toast({ title: 'Tải lên thất bại', description: getErrorMessage(err), variant: 'destructive' });
       } finally {
         if (inputRef.current) inputRef.current.value = '';
       }
     };

     const handleDelete = async () => {
       try {
         await remove.mutateAsync();
         toast({ title: 'Đã xoá template', description: 'Sẽ dùng template mặc định.' });
       } catch (err) {
         toast({ title: 'Xoá thất bại', description: getErrorMessage(err), variant: 'destructive' });
       }
     };

     const handleDownload = async () => {
       if (!templateFileName) return;
       setDownloading(true);
       try {
         const blob = await meSettingsApi.downloadHandoverTemplate(type);
         const url = URL.createObjectURL(blob);
         const a = document.createElement('a');
         a.href = url;
         a.download = templateOriginalName ?? templateFileName;
         document.body.appendChild(a);
         a.click();
         document.body.removeChild(a);
         URL.revokeObjectURL(url);
       } catch (err) {
         toast({ title: 'Tải về thất bại', description: getErrorMessage(err), variant: 'destructive' });
       } finally {
         setDownloading(false);
       }
     };

     return (
       <Card>
         <CardHeader>
           <CardTitle>{title}</CardTitle>
           <CardDescription>{description}</CardDescription>
         </CardHeader>
         <CardContent className="space-y-3">
           {hasTemplate ? (
             <>
               <p className="text-sm">
                 <strong>{templateOriginalName ?? templateFileName}</strong>
                 {templateUploadedAt && (
                   <span className="ml-2 text-muted-foreground">
                     (cập nhật {new Date(templateUploadedAt).toLocaleString('vi-VN')})
                   </span>
                 )}
               </p>
               <div className="flex gap-2">
                 <Button variant="outline" onClick={handleDownload} disabled={downloading}>
                   Tải về
                 </Button>
                 <Button variant="outline" onClick={handlePick}>Tải lên thay thế</Button>
                 <Button variant="destructive" onClick={handleDelete} disabled={remove.isPending}>
                   Xoá
                 </Button>
               </div>
             </>
           ) : (
             <>
               <p className="text-sm text-muted-foreground">Đang dùng template mặc định của hệ thống.</p>
               <Button onClick={handlePick}>Tải lên template riêng</Button>
             </>
           )}
           <input
             ref={inputRef}
             type="file"
             accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
             className="hidden"
             onChange={(e) => {
               const file = e.target.files?.[0];
               if (file) void handleUpload(file);
             }}
           />
         </CardContent>
       </Card>
     );
   }
   ```

4. **Sửa `QuotationSettingsTabContent`** — thêm 2 `HandoverTemplateCard` sau Card "Template Excel" hiện có (trước Card "Khoá theo trạng thái"):

   ```typescript
   <HandoverTemplateCard
     type="handover-with-price"
     title="Template Biên bản bàn giao (có tiền)"
     description="File .xlsx dùng khi xuất biên bản bàn giao kèm giá. Nếu chưa upload, hệ thống dùng template mặc định."
     settings={settings}
   />

   <HandoverTemplateCard
     type="handover-no-price"
     title="Template Biên bản bàn giao (không tiền)"
     description="File .xlsx dùng khi xuất biên bản bàn giao không kèm giá. Nếu chưa upload, hệ thống dùng template mặc định."
     settings={settings}
   />
   ```

5. **Type check:**
   ```
   cd frontend && npx tsc --noEmit
   ```
   Expected: 0 errors mới.

6. **Commit:**
   ```
   git commit -m "feat: add handover template sections to settings page"
   ```

## Verification

- `npx tsc --noEmit` → 0 errors mới
- Trang settings hiển thị 3 Card template: Báo giá / Biên bản có tiền / Biên bản không tiền
- Upload/delete/download hoạt động với đúng `?type=` query param

## Exit Criteria

- `HandoverTemplateCard` component tái sử dụng đúng pattern của card báo giá
- 2 card mới xuất hiện trong `QuotationSettingsTabContent` theo đúng thứ tự: báo giá → biên bản có tiền → biên bản không tiền → khoá trạng thái
