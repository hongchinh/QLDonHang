# Phase 09 — Frontend Settings Admin Page

**Status:** [ ] pending
**Complexity:** M

## Objective

Xây dựng trang settings admin `/settings/quotation` cho phép ADMIN chọn `RevenueReportingDateField`. Thêm API client, hooks, và route.

## Files

- `frontend/src/features/quotation-settings/api.ts` ← file mới (hoặc đặt trong `features/quotations/`)
- `frontend/src/features/quotation-settings/hooks.ts` ← file mới
- `frontend/src/features/quotation-settings/types.ts` ← file mới
- `frontend/src/pages/settings/quotation-settings-page.tsx` ← file mới
- `frontend/src/App.tsx` (hoặc router file) — thêm route
- `frontend/src/pages/settings/settings-hub-page.tsx` (nếu có trang hub) — thêm link

## Tasks

### API & Types

1. **Tạo `types.ts`**:
   ```typescript
   export type RevenueReportingDateField = 'QuotationDate' | 'ConfirmedAt' | 'AccountingConfirmedAt';

   export interface QuotationSystemSettings {
     revenueReportingDateField: RevenueReportingDateField;
     updatedAt: string;
     updatedBy?: string;
   }

   export interface UpdateQuotationSystemSettingsRequest {
     revenueReportingDateField: RevenueReportingDateField;
   }
   ```

2. **Tạo `api.ts`**:
   ```typescript
   import { apiGet, apiPut } from '@/lib/api-client';
   import type { QuotationSystemSettings, UpdateQuotationSystemSettingsRequest } from './types';

   export const quotationSettingsApi = {
     get: () => apiGet<QuotationSystemSettings>('/settings/quotation'),
     update: (data: UpdateQuotationSystemSettingsRequest) =>
       apiPut<QuotationSystemSettings>('/settings/quotation', data),
   };
   ```

3. **Tạo `hooks.ts`** — dùng React Query (xem pattern của `features/quotations/hooks.ts`):
   ```typescript
   import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
   import { quotationSettingsApi } from './api';
   import type { UpdateQuotationSystemSettingsRequest } from './types';

   const KEYS = { settings: ['quotation-settings'] as const };

   export function useQuotationSystemSettings() {
     return useQuery({ queryKey: KEYS.settings, queryFn: quotationSettingsApi.get });
   }

   export function useUpdateQuotationSystemSettings() {
     const qc = useQueryClient();
     return useMutation({
       mutationFn: (data: UpdateQuotationSystemSettingsRequest) =>
         quotationSettingsApi.update(data),
       onSuccess: () => qc.invalidateQueries({ queryKey: KEYS.settings }),
     });
   }
   ```

### Page Component

4. **Tạo `quotation-system-settings-page.tsx`**:
   - Sử dụng `useQuotationSystemSettings()` để load giá trị hiện tại
   - Select/RadioGroup với 3 lựa chọn:
     - `QuotationDate` → "Theo ngày báo giá (mặc định)"
     - `ConfirmedAt` → "Theo ngày khách xác nhận"
     - `AccountingConfirmedAt` → "Theo ngày kế toán xác nhận nhận tiền"
   - Nút Save gọi `useUpdateQuotationSystemSettings()`
   - Hiển thị `updatedAt` và `updatedBy` (nếu có)
   - Toast success/error

   Giữ UI đơn giản: `Card` với label + select + save button. Xem `pages/settings/my-quotation-settings-page.tsx` để tham khảo pattern.

### Routing

5. **Thêm route** trong `App.tsx` (hoặc router config):
   ```tsx
   <Route path="/settings/quotation" element={
     <ProtectedRoute requireRole="ADMIN">
       <QuotationSystemSettingsPage />
     </ProtectedRoute>
   } />
   ```
   Xem cách `ProtectedRoute` nhận `requireRole` từ `frontend/src/routes/protected-route.tsx`. Nếu không có prop đó, dùng `<Can requireRole="ADMIN">` wrap toàn trang.

6. **Thêm link vào settings hub** (`settings-hub-page.tsx` nếu tồn tại):
   - Thêm card/link "Cấu hình hệ thống báo giá" → `/settings/quotation`
   - Wrap bằng `<Can requireRole="ADMIN">` để chỉ ADMIN thấy

## Verification

```bash
cd frontend && npm run build
```

Manual smoke:
- Login ADMIN → `/settings/quotation` → thấy dropdown, đổi giá trị → Save → reload lại vẫn đúng
- Login SALES → redirect về 403/home (ProtectedRoute guard)

## Exit Criteria

- Frontend build thành công
- Trang `/settings/quotation` accessible với ADMIN, guarded với các role khác
- Dropdown hiển thị 3 options, Save persist vào DB qua API
- `updatedAt` hiển thị sau khi save
