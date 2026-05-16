# Phase 03 — Frontend cleanup + revenue report page

**Status:** [x] complete
**Complexity:** M

## Objective

Gỡ `'ConvertedToOrder'` khỏi mọi type/component frontend. Thêm cảnh báo khi hủy báo giá Confirmed. Build trang `/reports/sales-revenue` mới, kèm route + sidebar entry.

## Files

### Modify (cleanup ConvertedToOrder)
- `frontend/src/features/quotations/types.ts`
- `frontend/src/features/me-settings/types.ts` (alias `LockAtStatus`)
- `frontend/src/pages/quotations/components/status-pill.tsx`
- `frontend/src/pages/quotations/quotation-list-page.tsx`
- `frontend/src/pages/quotations/quotation-form-page.tsx`
- `frontend/src/pages/admin/user-settings-page.tsx`
- `frontend/src/pages/settings/my-quotation-settings-page.tsx`

### Create (revenue report)
- `frontend/src/features/reports/sales-revenue/types.ts`
- `frontend/src/features/reports/sales-revenue/api.ts`
- `frontend/src/features/reports/sales-revenue/keys.ts`
- `frontend/src/features/reports/sales-revenue/hooks.ts`
- `frontend/src/pages/reports/sales-revenue-page.tsx`

### Modify (routing + nav)
- File router (tìm bằng `frontend/src/**/{router,routes,App}.{ts,tsx}` — thường là `frontend/src/router.tsx` hoặc `frontend/src/App.tsx`)
- Sidebar/menu (tìm bằng `frontend/src/components/**/sidebar*.tsx` hoặc `frontend/src/layouts/**`)

## Tasks

### A. Cleanup `ConvertedToOrder`

1. `frontend/src/features/quotations/types.ts`:
   - Đổi `export type QuotationStatus = 'Draft' | 'Sent' | 'Confirmed' | 'ConvertedToOrder' | 'Cancelled';` thành `'Draft' | 'Sent' | 'Confirmed' | 'Cancelled';`.
   - Trong interface `Quotation`, thêm:
     ```ts
     confirmedAt?: string;
     confirmedByUserId?: string;
     confirmedByName?: string;
     cancelledAt?: string;
     ```
   - Trong interface `QuotationListItem`, thêm `confirmedAt?: string;`.
2. `frontend/src/features/me-settings/types.ts`: đổi `export type LockAtStatus = 'Sent' | 'Confirmed' | 'ConvertedToOrder' | null;` thành `'Sent' | 'Confirmed' | null;`.
3. `frontend/src/pages/quotations/components/status-pill.tsx`: xóa dòng `ConvertedToOrder: { ... },` trong `MAP`.
4. `frontend/src/pages/quotations/quotation-list-page.tsx`:
   - Sửa `const canCancel = q.status !== 'Cancelled' && q.status !== 'ConvertedToOrder';` thành `const canCancel = q.status !== 'Cancelled';`.
   - Trong cancel modal/handler `onConfirmCancel`: nếu `pendingCancel.status === 'Confirmed'`, dialog hiện thông điệp warning rõ ràng (ví dụ: `"Báo giá đã xác nhận — hủy sẽ trừ doanh thu của sale. Bạn cần quyền đặc biệt."`). Cụ thể: tách state `pendingCancel: QuotationListItem | null` thành chuỗi confirmation 2-cấp nếu status = Confirmed; với non-Confirmed dùng confirm dialog ngắn như cũ.
5. `frontend/src/pages/quotations/quotation-form-page.tsx`:
   - Xóa nhánh `: initial.status === 'ConvertedToOrder' ? 'Báo giá đã chuyển đơn hàng — không thể chỉnh sửa.'`.
   - Trong button Hủy (line ~292): khi `status === 'Confirmed'`, wrap onClick bằng confirm dialog cảnh báo "Báo giá đã xác nhận — hủy sẽ ghi nhận giảm doanh thu. Cần quyền `quotations.cancel_confirmed`."
6. `frontend/src/pages/admin/user-settings-page.tsx`: xóa dòng `{ value: 'ConvertedToOrder', label: 'Từ Đã chuyển đơn hàng' },` trong `OPTIONS`.
7. `frontend/src/pages/settings/my-quotation-settings-page.tsx`: xóa dòng `ConvertedToOrder: 'Đã chuyển đơn hàng',` trong label map.

### B. Build sales revenue report

8. `frontend/src/features/reports/sales-revenue/types.ts`:
   ```ts
   export interface SalesRevenueReportItem {
     saleUserId: string;
     saleName: string;
     isSaleDeleted: boolean;
     quotationCount: number;
     totalRevenueGross: number;
     totalRevenueNet: number;
   }

   export interface SalesRevenueReport {
     from: string;
     to: string;
     items: SalesRevenueReportItem[];
     totalQuotationCount: number;
     grandTotalGross: number;
     grandTotalNet: number;
   }

   export interface SalesRevenueReportParams {
     from: string;
     to: string;
     saleUserId?: string;
   }
   ```
9. `frontend/src/features/reports/sales-revenue/keys.ts`:
   ```ts
   import type { SalesRevenueReportParams } from './types';
   export const salesRevenueKeys = {
     all: ['reports', 'sales-revenue'] as const,
     list: (p: SalesRevenueReportParams) => ['reports', 'sales-revenue', p] as const,
   };
   ```
10. `frontend/src/features/reports/sales-revenue/api.ts`:
    ```ts
    import { apiGet } from '@/lib/api-client';
    import type { SalesRevenueReport, SalesRevenueReportParams } from './types';

    export function fetchSalesRevenue(params: SalesRevenueReportParams) {
      return apiGet<SalesRevenueReport>('/api/reports/sales-revenue', { params });
    }
    ```
    (Adjust import path to match existing `api-client` exports — match style used in `frontend/src/features/quotations/api.ts`.)
11. `frontend/src/features/reports/sales-revenue/hooks.ts`:
    ```ts
    import { useQuery } from '@tanstack/react-query';
    import { fetchSalesRevenue } from './api';
    import { salesRevenueKeys } from './keys';
    import type { SalesRevenueReportParams } from './types';

    export function useSalesRevenue(params: SalesRevenueReportParams, enabled = true) {
      return useQuery({
        queryKey: salesRevenueKeys.list(params),
        queryFn: () => fetchSalesRevenue(params),
        enabled,
      });
    }
    ```
12. `frontend/src/pages/reports/sales-revenue-page.tsx`:
    - Form filter: 2 input date (`from`, `to`, default = đầu tháng → hôm nay), 1 select `saleUserId` (optional, populated từ users API hoặc để trống cho admin lọc all).
    - Bảng: cột `Sale`, `Số báo giá`, `Doanh thu (gồm thuế)`, `Doanh thu thuần`. Format số theo `Intl.NumberFormat('vi-VN')`.
    - Footer row: tổng cộng `GrandTotalGross`, `GrandTotalNet`, `TotalQuotationCount`.
    - Wrap bằng `<Can permission="reports.revenue">`.
    - Loading/empty states (theo style các page hiện có).

### C. Routing + sidebar

13. Tìm router config: `Grep -r "quotations/new" frontend/src` để biết file route. Thêm route mới:
    ```tsx
    { path: '/reports/sales-revenue', element: <SalesRevenuePage /> }
    ```
14. Tìm sidebar: `Grep -r "Báo giá" frontend/src/components frontend/src/layouts`. Thêm menu entry:
    - Section "Báo cáo" (tạo nếu chưa có)
    - Item "Doanh thu sale" → `/reports/sales-revenue`
    - Wrap permission gate `reports.revenue`

## Verification

- `cd frontend ; npm run typecheck` → 0 errors. (Đây là verification chính: TypeScript sẽ flag mọi nơi còn `'ConvertedToOrder'`.)
- `cd frontend ; npm test` → các test hiện có pass (line-items-grid.test.tsx etc.).
- `cd frontend ; npm run lint` → 0 errors mới.
- KHÔNG khởi động dev server backend nếu nó đang chạy. Frontend dev server (Vite) restart nhẹ; user có thể tự kiểm tra UI nếu muốn.

## Exit Criteria

- `grep -r ConvertedToOrder frontend/src` → không khớp.
- Type check + tests pass.
- Trang `/reports/sales-revenue` build được, không runtime crash khi mở.
