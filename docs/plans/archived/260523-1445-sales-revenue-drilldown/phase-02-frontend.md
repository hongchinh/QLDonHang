# Phase 02 — Frontend

**Status:** [ ] pending
**Complexity:** M

## Objective

Tạo các feature files cho API mới, trang chi tiết `SalesRevenueDetailPage`, đăng ký route, và thêm click handler vào bảng tổng hợp. Sau khi hoàn thành, user có thể click dòng sale → trang chi tiết → click dòng hàng → trang báo giá.

> **Frontend TDD note:** Các file `types.ts`, `keys.ts`, `api.ts`, `hooks.ts` là wrappers mỏng không có logic thuần (pure logic) để test riêng. Dùng `npx tsc --noEmit` làm gate kiểm tra kiểu (type check) thay cho unit test. Sau khi tất cả file được tạo, chạy type check trước khi commit.

## Files

**Mới:**
- `frontend/src/features/reports/sales-revenue-detail/types.ts`
- `frontend/src/features/reports/sales-revenue-detail/keys.ts`
- `frontend/src/features/reports/sales-revenue-detail/api.ts`
- `frontend/src/features/reports/sales-revenue-detail/hooks.ts`
- `frontend/src/pages/reports/sales-revenue-detail-page.tsx`

**Sửa:**
- `frontend/src/App.tsx`
- `frontend/src/pages/reports/sales-revenue-page.tsx`

## Tasks

### Task 1 — Tạo types.ts

Tạo file `frontend/src/features/reports/sales-revenue-detail/types.ts`:

```typescript
export interface SalesRevenueLineItemDto {
  quotationId: string;
  quotationCode: string;
  quotationDate: string;       // DateOnly → "YYYY-MM-DD"
  confirmedAt: string | null;  // DateTime? → ISO string
  customerName: string;
  customerAddress: string | null;
  contactPhone: string | null;
  freight: number;
  isFirstLineOfQuotation: boolean;
  productName: string;
  specification: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  unitCost: number | null;
  lineCost: number | null;
  lineProfit: number | null;
}

export interface SalesRevenueLineItemsParams {
  from: string;
  to: string;
}
```

### Task 2 — Tạo keys.ts

Tạo file `frontend/src/features/reports/sales-revenue-detail/keys.ts`:

```typescript
import type { SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailKeys = {
  all: ['reports', 'sales-revenue-detail'] as const,
  lines: (saleUserId: string, p: SalesRevenueLineItemsParams) =>
    ['reports', 'sales-revenue-detail', saleUserId, p] as const,
};
```

### Task 3 — Tạo api.ts

Tạo file `frontend/src/features/reports/sales-revenue-detail/api.ts`:

```typescript
import { apiGet } from '@/lib/api-client';
import type { SalesRevenueLineItemDto, SalesRevenueLineItemsParams } from './types';

export const salesRevenueDetailApi = {
  getLines: (saleUserId: string, params: SalesRevenueLineItemsParams) =>
    apiGet<SalesRevenueLineItemDto[]>(`/reports/sales-revenue/${saleUserId}/lines`, params),
};
```

### Task 4 — Tạo hooks.ts

Tạo file `frontend/src/features/reports/sales-revenue-detail/hooks.ts`:

```typescript
import { useQuery } from '@tanstack/react-query';
import { salesRevenueDetailApi } from './api';
import { salesRevenueDetailKeys } from './keys';
import type { SalesRevenueLineItemsParams } from './types';

export function useSalesRevenueDetail(
  saleUserId: string | undefined,
  params: SalesRevenueLineItemsParams,
  enabled = true,
) {
  return useQuery({
    queryKey: salesRevenueDetailKeys.lines(saleUserId ?? '', params),
    queryFn: () => salesRevenueDetailApi.getLines(saleUserId!, params),
    enabled: enabled && !!saleUserId,
  });
}
```

### Task 5 — Tạo SalesRevenueDetailPage

Tạo file `frontend/src/pages/reports/sales-revenue-detail-page.tsx`:

```tsx
import { useNavigate, useParams, useSearchParams, Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Button } from '@/components/ui/button';
import { Can } from '@/components/auth/can';
import { useSalesRevenueDetail } from '@/features/reports/sales-revenue-detail/hooks';
import type { SalesRevenueLineItemDto } from '@/features/reports/sales-revenue-detail/types';
import { ArrowLeft } from 'lucide-react';

const moneyFmt = new Intl.NumberFormat('vi-VN');

function formatDate(value: string | null | undefined): string {
  if (!value) return '';
  return value.slice(0, 10).split('-').reverse().join('/');
}

export function SalesRevenueDetailPage() {
  const { saleUserId } = useParams<{ saleUserId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const from = searchParams.get('from') ?? '';
  const to = searchParams.get('to') ?? '';

  const query = useSalesRevenueDetail(saleUserId, { from, to }, Boolean(from && to && saleUserId));

  const items: SalesRevenueLineItemDto[] = query.data ?? [];
  const hasCost = items.some((i) => i.unitCost !== null);

  const saleName = items[0]
    ? items[0].customerName   // fallback — we'll show it in header
    : '';

  return (
    <Can
      permission="reports.revenue"
      fallback={<div className="p-4">Bạn không có quyền xem báo cáo này.</div>}
    >
      <div className="space-y-4">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-1 h-4 w-4" />
            Quay lại
          </Button>
          <div>
            <h1 className="text-2xl font-bold">Chi tiết doanh thu</h1>
            <p className="text-sm text-muted-foreground">
              {from && to ? `${formatDate(from)} – ${formatDate(to)}` : ''}
            </p>
          </div>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Dòng hàng hóa</CardTitle>
          </CardHeader>
          <CardContent>
            {query.isLoading ? (
              <div className="text-sm text-muted-foreground">Đang tải…</div>
            ) : query.isError ? (
              <div className="text-sm text-destructive">Không tải được dữ liệu.</div>
            ) : items.length === 0 ? (
              <div className="text-sm text-muted-foreground">
                Không có dòng hàng nào trong khoảng thời gian này.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Số BG</TableHead>
                      <TableHead>Ngày BG</TableHead>
                      <TableHead>Ngày XN</TableHead>
                      <TableHead>Khách hàng</TableHead>
                      <TableHead>Địa chỉ</TableHead>
                      <TableHead>Điện thoại</TableHead>
                      <TableHead>Hàng hóa</TableHead>
                      <TableHead>Kích thước</TableHead>
                      <TableHead className="text-right">SL</TableHead>
                      <TableHead className="text-right">Đơn giá</TableHead>
                      <TableHead className="text-right">Số tiền</TableHead>
                      <TableHead className="text-right">Vận chuyển</TableHead>
                      {hasCost && (
                        <>
                          <TableHead className="text-right">ĐG nhập</TableHead>
                          <TableHead className="text-right">Thành tiền nhập</TableHead>
                          <TableHead className="text-right">Lợi nhuận</TableHead>
                        </>
                      )}
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {items.map((item, idx) => (
                      <TableRow
                        key={`${item.quotationId}-${idx}`}
                        className="cursor-pointer hover:bg-muted/50"
                        onClick={() => navigate(`/quotations/${item.quotationId}`)}
                      >
                        <TableCell>
                          {item.isFirstLineOfQuotation ? item.quotationCode : ''}
                        </TableCell>
                        <TableCell>
                          {item.isFirstLineOfQuotation ? formatDate(item.quotationDate) : ''}
                        </TableCell>
                        <TableCell>
                          {item.isFirstLineOfQuotation ? formatDate(item.confirmedAt) : ''}
                        </TableCell>
                        <TableCell>
                          {item.isFirstLineOfQuotation ? item.customerName : ''}
                        </TableCell>
                        <TableCell>
                          {item.isFirstLineOfQuotation ? (item.customerAddress ?? '') : ''}
                        </TableCell>
                        <TableCell>
                          {item.isFirstLineOfQuotation ? (item.contactPhone ?? '') : ''}
                        </TableCell>
                        <TableCell>{item.productName}</TableCell>
                        <TableCell>{item.specification ?? ''}</TableCell>
                        <TableCell className="text-right">{moneyFmt.format(item.quantity)}</TableCell>
                        <TableCell className="text-right">{moneyFmt.format(item.unitPrice)}</TableCell>
                        <TableCell className="text-right">{moneyFmt.format(item.lineTotal)}</TableCell>
                        <TableCell className="text-right">
                          {item.isFirstLineOfQuotation ? moneyFmt.format(item.freight) : ''}
                        </TableCell>
                        {hasCost && (
                          <>
                            <TableCell className="text-right">
                              {item.unitCost !== null ? moneyFmt.format(item.unitCost) : ''}
                            </TableCell>
                            <TableCell className="text-right">
                              {item.lineCost !== null ? moneyFmt.format(item.lineCost) : ''}
                            </TableCell>
                            <TableCell className="text-right">
                              {item.lineProfit !== null ? moneyFmt.format(item.lineProfit) : ''}
                            </TableCell>
                          </>
                        )}
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </Can>
  );
}
```

> **Note về `saleName`:** Tên sale không có trong `SalesRevenueLineItemDto` (chỉ có CustomerName). Trang header chỉ hiển thị khoảng ngày. Nếu muốn hiển thị tên sale, có thể đọc từ state navigation (không nằm trong scope hiện tại).

### Task 6 — Thêm route vào App.tsx

Mở `frontend/src/App.tsx`.

**Thêm import:**
```tsx
import { SalesRevenueDetailPage } from '@/pages/reports/sales-revenue-detail-page';
```

**Thêm route** bên trong `<Route path="reports">`, ngay sau route `sales-revenue`:

```tsx
<Route
  path="sales-revenue/:saleUserId"
  element={
    <ProtectedRoute permission="reports.revenue">
      <SalesRevenueDetailPage />
    </ProtectedRoute>
  }
/>
```

Route đầy đủ sau khi sửa (phần reports):

```tsx
<Route path="reports">
  <Route index element={<Navigate to="revenue" replace />} />
  <Route
    path="revenue"
    element={
      <ProtectedRoute permission="reports.revenue">
        <RevenuePage />
      </ProtectedRoute>
    }
  />
  <Route
    path="sales-performance"
    element={
      <ProtectedRoute permission="quotations.view_all">
        <SalesPerformancePage />
      </ProtectedRoute>
    }
  />
  <Route
    path="sales-revenue"
    element={
      <ProtectedRoute permission="reports.revenue">
        <SalesRevenuePage />
      </ProtectedRoute>
    }
  />
  <Route
    path="sales-revenue/:saleUserId"
    element={
      <ProtectedRoute permission="reports.revenue">
        <SalesRevenueDetailPage />
      </ProtectedRoute>
    }
  />
  <Route
    path="vehicle-revenue"
    element={
      <ProtectedRoute permission="reports.revenue">
        <VehicleRevenuePage />
      </ProtectedRoute>
    }
  />
</Route>
```

### Task 7 — Thêm click handler vào SalesRevenuePage

Mở `frontend/src/pages/reports/sales-revenue-page.tsx`.

**Thêm import:**
```tsx
import { useNavigate } from 'react-router-dom';
```

**Thêm** trong body của `SalesRevenuePage` component (sau các `useState`):
```tsx
const navigate = useNavigate();
```

**Sửa** `TableRow` trong tbody (tìm dòng `<TableRow key={it.saleUserId}>`):

Từ:
```tsx
<TableRow key={it.saleUserId}>
```

Thành:
```tsx
<TableRow
  key={it.saleUserId}
  className="cursor-pointer hover:bg-muted/50"
  onClick={() =>
    navigate(`/reports/sales-revenue/${it.saleUserId}?from=${from}&to=${to}`)
  }
>
```

> Footer row `<TableRow>` trong `<TableFooter>` **không thêm** onClick — nó không clickable.

### Task 8 — Chạy TypeScript type check

```bash
cd frontend && npx tsc --noEmit
```

Expected: 0 errors. Sửa bất kỳ lỗi type nào trước khi tiếp tục.

### Task 9 — Commit

```bash
git add frontend/src/features/reports/sales-revenue-detail/
git add frontend/src/pages/reports/sales-revenue-detail-page.tsx
git add frontend/src/App.tsx
git add frontend/src/pages/reports/sales-revenue-page.tsx
git commit -m "feat: add sales-revenue drill-down detail page and navigation"
```

## Verification

```bash
# Type check
cd frontend && npx tsc --noEmit

# Manual smoke test
# 1. Mở http://localhost:5173/reports/sales-revenue
# 2. Đặt bộ lọc ngày có dữ liệu, xác nhận các dòng sale có cursor-pointer
# 3. Click một dòng sale → xác nhận URL chuyển sang /reports/sales-revenue/:id?from=...&to=...
# 4. Xác nhận bảng hiển thị đúng cột; dòng đầu mỗi nhóm có số BG / khách hàng / vận chuyển
# 5. Các dòng tiếp theo của cùng báo giá để trống ở các cột cấp báo giá
# 6. Click nút Quay lại → trở về trang tổng hợp
# 7. Click một dòng hàng trong bảng chi tiết → URL chuyển sang /quotations/:id
```

## Exit Criteria

- [ ] 4 files trong `features/reports/sales-revenue-detail/` tồn tại và type-check sạch
- [ ] `SalesRevenueDetailPage` render bảng với đúng cột; cost columns ẩn khi không có dữ liệu cost
- [ ] Route `/reports/sales-revenue/:saleUserId` hoạt động với `ProtectedRoute permission="reports.revenue"`
- [ ] Rows trong `SalesRevenuePage` có `cursor-pointer` và navigate đúng URL với query params
- [ ] Footer row trong `SalesRevenuePage` không clickable
- [ ] `npx tsc --noEmit` trả về 0 lỗi
