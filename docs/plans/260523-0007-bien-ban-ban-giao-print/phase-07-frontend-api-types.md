# Phase 07 — Frontend API + Types + Hooks

**Status:** [ ] pending
**Complexity:** S

## Objective

Mở rộng TypeScript types, API functions và React Query hooks để hỗ trợ 2 loại template biên bản và 4 download functions mới.

## Files

- `frontend/src/features/me-settings/types.ts`
- `frontend/src/features/me-settings/api.ts`
- `frontend/src/features/me-settings/hooks.ts`
- `frontend/src/features/quotations/api.ts`

## Tasks

### Task 7.1 — Mở rộng `MyQuotationSettings` type

1. **Mở** `frontend/src/features/me-settings/types.ts`

2. **Thêm 6 field mới** sau `templateUploadedAt`:

   ```typescript
   handoverWithPriceTemplateFileName: string | null;
   handoverWithPriceTemplateOriginalName: string | null;
   handoverWithPriceTemplateUploadedAt: string | null;
   handoverNoPriceTemplateFileName: string | null;
   handoverNoPriceTemplateOriginalName: string | null;
   handoverNoPriceTemplateUploadedAt: string | null;
   ```

3. **Type check:**
   ```
   cd frontend && npx tsc --noEmit
   ```
   Expected: 0 errors (có thể có unrelated errors — chỉ chú ý lỗi mới).

### Task 7.2 — Mở rộng `meSettingsApi` với type-aware functions

1. **Mở** `frontend/src/features/me-settings/api.ts`

2. **Thêm type** cho handover template type param:

   ```typescript
   export type HandoverTemplateType = 'handover-with-price' | 'handover-no-price';
   ```

   > **Lưu ý:** Không cần `'quotation'` trong union — type này chỉ dùng cho handover API functions. Các function báo giá cũ không nhận type param.

3. **Thêm 3 function mới** cho handover templates:

   ```typescript
   uploadHandoverTemplate: async (
     file: File,
     type: HandoverTemplateType,
   ): Promise<MyQuotationSettings> => {
     const form = new FormData();
     form.append('file', file);
     const res = await api.put<ApiResponse<MyQuotationSettings>>(
       `/me/quotation-settings/template?type=${type}`,
       form,
       { headers: { 'Content-Type': 'multipart/form-data' } },
     );
     if (!res.data.success || !res.data.data) {
       throw new Error(res.data.error?.message ?? 'Upload thất bại');
     }
     return res.data.data;
   },

   deleteHandoverTemplate: (
     type: HandoverTemplateType,
   ) => apiDelete<MyQuotationSettings>(`/me/quotation-settings/template?type=${type}`),

   downloadHandoverTemplate: async (
     type: HandoverTemplateType,
   ): Promise<Blob> => {
     const res = await api.get(`/me/quotation-settings/template?type=${type}`, {
       responseType: 'blob',
     });
     return res.data as Blob;
   },
   ```

### Task 7.3 — Thêm hooks cho handover template

1. **Mở** `frontend/src/features/me-settings/hooks.ts`

2. **Thêm import** `HandoverTemplateType` từ `./api`:
   ```typescript
   import { meSettingsApi, HandoverTemplateType } from './api';
   ```

3. **Thêm 2 hooks mới** sau `useDeleteTemplate`:

   ```typescript
   export function useUploadHandoverTemplate(type: HandoverTemplateType) {
     const qc = useQueryClient();
     return useMutation({
       mutationFn: (file: File) => meSettingsApi.uploadHandoverTemplate(file, type),
       onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
     });
   }

   export function useDeleteHandoverTemplate(type: HandoverTemplateType) {
     const qc = useQueryClient();
     return useMutation({
       mutationFn: () => meSettingsApi.deleteHandoverTemplate(type),
       onSuccess: () => qc.invalidateQueries({ queryKey: meSettingsKeys.all }),
     });
   }
   ```

### Task 7.4 — Thêm download functions vào `quotationsApi`

1. **Mở** `frontend/src/features/quotations/api.ts`

2. **Thêm 4 functions** sau `downloadExcel`:

   ```typescript
   downloadHandoverWithPriceExcel: async (id: string): Promise<Blob> => {
     const res = await api.get(`/quotations/${id}/handover-with-price/excel`, {
       responseType: 'blob',
     });
     return res.data as Blob;
   },

   downloadHandoverWithPricePdf: async (id: string): Promise<Blob> => {
     const res = await api.get(`/quotations/${id}/handover-with-price/pdf`, {
       responseType: 'blob',
     });
     return res.data as Blob;
   },

   downloadHandoverNoPriceExcel: async (id: string): Promise<Blob> => {
     const res = await api.get(`/quotations/${id}/handover-no-price/excel`, {
       responseType: 'blob',
     });
     return res.data as Blob;
   },

   downloadHandoverNoPricePdf: async (id: string): Promise<Blob> => {
     const res = await api.get(`/quotations/${id}/handover-no-price/pdf`, {
       responseType: 'blob',
     });
     return res.data as Blob;
   },
   ```

3. **Type check toàn bộ:**
   ```
   cd frontend && npx tsc --noEmit
   ```
   Expected: 0 errors liên quan đến các file vừa sửa.

4. **Commit:**
   ```
   git commit -m "feat: add handover template API functions, types, and hooks"
   ```

## Verification

- `npx tsc --noEmit` → không có lỗi mới trong các file vừa sửa
- `quotationsApi.downloadHandoverWithPriceExcel` tồn tại và typed đúng
- `useUploadHandoverTemplate` hook export đúng

## Exit Criteria

- `MyQuotationSettings` có 6 field mới
- `meSettingsApi` có 3 method mới cho handover templates
- `quotationsApi` có 4 method download mới
- 2 hooks mới export từ `hooks.ts`
