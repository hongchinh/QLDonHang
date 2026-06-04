# Phase 02 — Frontend: API + Page Wiring

**Status:** [ ] pending
**Complexity:** S

## Objective

Thêm hàm `downloadRevenueExcel()` vào API layer, kết nối button "Xuất Excel" trong `revenue-page.tsx` với loading state và error toast. Không có frontend test framework — phase này được verify bằng manual testing và TypeScript build.

## Files

- `frontend/src/features/reports/sales-revenue-detail/api.ts` (edit)
- `frontend/src/pages/reports/revenue-page.tsx` (edit)

## Tasks

### Task 1 — Add `downloadRevenueExcel` to API layer

Sửa `frontend/src/features/reports/sales-revenue-detail/api.ts` — thêm import `api` và hàm download:

**Trước:**
```ts
import { apiGet } from '@/lib/api-client';
import type { SalesRevenueLineItemDto, SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailApi = {
  getLines: (saleUserId: string, params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>(`/reports/sales-revenue/${saleUserId}/lines`, params),
  getRevenueLines: (params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>('/reports/revenue-lines', params),
};
```

**Sau:**
```ts
import api, { apiGet } from '@/lib/api-client';
import type { SalesRevenueLineItemDto, SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailApi = {
  getLines: (saleUserId: string, params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>(`/reports/sales-revenue/${saleUserId}/lines`, params),
  getRevenueLines: (params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>('/reports/revenue-lines', params),
  downloadRevenueExcel: async (params: SalesRevenueLineItemsParams): Promise<void> => {
    const res = await api.get('/reports/revenue-lines/excel', {
      params,
      responseType: 'blob',
    });
    const url = URL.createObjectURL(res.data as Blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `BaoCaoDoanhThu_${params.from.replace(/-/g, '')}_${params.to.replace(/-/g, '')}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },
};
```

---

### Task 2 — Wire button in `revenue-page.tsx`

Sửa `frontend/src/pages/reports/revenue-page.tsx`:

**2a. Thêm import `Loader2`** — trong phần imports đầu file, thêm vào import lucide-react (hoặc tạo mới nếu chưa có):
```ts
import { Loader2 } from 'lucide-react';
```

**2b. Thêm import API function** — trong phần imports:
```ts
import { salesRevenueDetailApi } from '@/features/reports/sales-revenue-detail/api';
```

**2c. Thêm state `isExporting`** — trong body của `RevenuePage`, ngay sau các useState hiện có:
```ts
const [isExporting, setIsExporting] = useState(false);
```

**2d. Thêm handler `handleExportExcel`** — sau khai báo `isExporting`:
```ts
async function handleExportExcel() {
  if (!from || !to) return;
  setIsExporting(true);
  try {
    await salesRevenueDetailApi.downloadRevenueExcel({ from, to, saleUserId });
  } catch {
    toast({ title: 'Lỗi', description: 'Không thể xuất Excel. Vui lòng thử lại.', variant: 'destructive' });
  } finally {
    setIsExporting(false);
  }
}
```

**2e. Thay button hiện tại** — tìm button "Xuất Excel" (dòng ~148-154):

Trước:
```tsx
<Button
  variant="outline"
  size="sm"
  onClick={() => toast({ title: 'Sắp ra mắt', description: 'Xuất Excel đang được phát triển.' })}
>
  Xuất Excel
</Button>
```

Sau:
```tsx
<Button
  variant="outline"
  size="sm"
  onClick={handleExportExcel}
  disabled={isExporting || !from || !to}
>
  {isExporting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
  Xuất Excel
</Button>
```

---

### Task 3 — Verify TypeScript build

```
cd frontend && npm run build
```
Expected: PASS — no TypeScript errors.

---

### Task 4 — Manual verification

1. Mở trang báo cáo doanh thu
2. Chọn khoảng ngày có dữ liệu → click "Xuất Excel":
   - Button hiển thị spinner khi đang tải
   - File `BaoCaoDoanhThu_YYYYMMDD_YYYYMMDD.xlsx` được download
   - Mở file: 18 cột header, dữ liệu khớp bảng UI, footer row tổng đúng
3. Chọn khoảng ngày không có dữ liệu → click "Xuất Excel":
   - File download thành công, chỉ có header row
4. Trường hợp lỗi mạng (ngắt kết nối backend) → click "Xuất Excel":
   - Toast "Không thể xuất Excel. Vui lòng thử lại." xuất hiện
5. Khi `from` hoặc `to` chưa chọn: button disabled, không thể click

---

### Task 5 — Commit

```
git add frontend/src/features/reports/sales-revenue-detail/api.ts \
        frontend/src/pages/reports/revenue-page.tsx
git commit -m "feat: wire Xuất Excel button for Chi tiết doanh thu"
```

## Verification

```
cd frontend && npm run build
```

## Exit Criteria

- [ ] TypeScript build passes
- [ ] Button "Xuất Excel" download file `.xlsx` đúng tên `BaoCaoDoanhThu_{from}_{to}.xlsx`
- [ ] Spinner hiện khi đang export
- [ ] Button disabled khi `from` hoặc `to` null
- [ ] Toast lỗi hiện khi backend trả lỗi
- [ ] File Excel mở được, 18 cột, dữ liệu khớp UI
