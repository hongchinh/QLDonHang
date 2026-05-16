# Phase 04 — Frontend dashboards (user + admin) + sidebar

**Status:** [ ] pending
**Complexity:** M

## Objective

Refactor `/dashboard` thành user view; build `/admin/dashboard` (mới) với leaderboard + dropdown filter sale. Update sidebar trong `AppLayout`: admin chỉ thấy link "Dashboard" trỏ `/admin/dashboard`, user thấy `/dashboard`. Sử dụng các component đã build ở Phase 03.

## Files

- `frontend/src/pages/dashboard-page.tsx` (refactor toàn bộ)
- `frontend/src/pages/admin/admin-dashboard-page.tsx` (new)
- `frontend/src/App.tsx` (edit — thêm route `/admin/dashboard`)
- `frontend/src/components/layout/app-layout.tsx` (edit — sidebar conditional)
- Có thể: `frontend/src/features/admin-users/api.ts` (re-use) — lấy list sale cho dropdown

## Tasks

### A. User dashboard `/dashboard`

Refactor [dashboard-page.tsx](../../../frontend/src/pages/dashboard-page.tsx):

```tsx
export function DashboardPage() {
  const params = useDashboardParams();
  const summary = useDashboardSummary(params);
  const revenue = useRevenueSeries({ ...params, granularity });
  const topCustomers = useTopCustomers({ ...params, limit: 5 });
  const topProducts = useTopProducts({ ...params, limit: 5 });
  const activity = useRecentActivity({ limit: 8 });

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Tổng quan của tôi</h1>
          <p className="text-sm text-muted-foreground">{formatRangeLabel(params)}</p>
        </div>
        <RangePicker from={params.from} to={params.to} onChange={params.setRange} />
      </header>

      {/* Row 1: 4 KPI cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <KpiCard label="Doanh thu hôm nay" format="currency" kpi={summary.data?.todayRevenue} />
        <KpiCard label="Doanh thu khoảng" format="currency" kpi={summary.data?.rangeRevenue} />
        <KpiCard label="Tổng báo giá" format="number" kpi={summary.data?.totalCount} />
        <KpiCard label="Đã huỷ" format="number" kpi={summary.data?.cancelledCount} />
      </div>

      {/* Row 2: Revenue chart full width */}
      <Card>
        <CardHeader><CardTitle>Doanh thu</CardTitle></CardHeader>
        <CardContent>
          <RevenueAreaChart points={revenue.data?.points ?? []} granularity={granularity} onGranularityChange={setGranularity} />
        </CardContent>
      </Card>

      {/* Row 3: Funnel + Activity */}
      <div className="grid gap-4 lg:grid-cols-2">
        <Card><CardHeader><CardTitle>Phễu trạng thái</CardTitle></CardHeader><CardContent><StatusFunnel data={summary.data?.funnel} /></CardContent></Card>
        <Card><CardHeader><CardTitle>Hoạt động gần đây</CardTitle></CardHeader><CardContent><ActivityTimeline items={activity.data ?? []} /></CardContent></Card>
      </div>

      {/* Row 4: Top customers + Top products */}
      <div className="grid gap-4 lg:grid-cols-2">
        <TopListCard title="Top khách hàng" items={mapCustomers(topCustomers.data)} format="currency" />
        <TopListCard title="Top sản phẩm" items={mapProducts(topProducts.data)} format="currency" />
      </div>
    </div>
  );
}
```

- Bỏ legacy `useQuotationStats` (move ra để Phase 06 cleanup hoặc giữ làm fallback nếu summary 404).
- `granularity` state cục bộ (`useState<Granularity>('day')`).

### B. Admin dashboard `/admin/dashboard`

`frontend/src/pages/admin/admin-dashboard-page.tsx` (mới):

- Cùng layout với DashboardPage, khác:
  - Title: "Tổng quan hệ thống".
  - Header thêm `<SaleFilterDropdown value={params.saleUserId} onChange={params.setSaleUserId} />` ngay cạnh range picker.
  - Row 3 right: thay `ActivityTimeline` bằng `SalesLeaderboard` (sản phẩm Top vẫn giữ).
  - Hook thêm: `useLeaderboard({ from, to, limit: 5 })`.

- `SaleFilterDropdown`: load users qua `adminUsersApi.list()` đã có (xem [frontend/src/features/admin-users/](../../../frontend/src/features/admin-users/)), select native hoặc `@radix-ui/react-select`. Empty option "Tất cả sale".

### C. Routing

[App.tsx](../../../frontend/src/App.tsx) thêm route trước/sau route customers:
```tsx
<Route
  path="admin/dashboard"
  element={
    <ProtectedRoute permission="quotations.view_all">
      <AdminDashboardPage />
    </ProtectedRoute>
  }
/>
```
Import: `import { AdminDashboardPage } from '@/pages/admin/admin-dashboard-page';`.

### D. Sidebar

Trong `frontend/src/components/layout/app-layout.tsx` (đọc file trước để xem cấu trúc menu):

- Nếu user có permission `quotations.view_all`:
  - Link "Dashboard" trỏ `/admin/dashboard`.
- Nếu không:
  - Link "Dashboard" trỏ `/` (mặc định `/dashboard`).

Logic:
```tsx
const hasViewAll = useAuthStore((s) => s.hasPermission('quotations.view_all'));
const dashboardHref = hasViewAll ? '/admin/dashboard' : '/';
```

Note: `/` đang là index route render `DashboardPage`. User thường truy cập `/` vẫn nhận user dashboard. Admin gõ tay `/` thì cũng OK (admin có quyền `quotations.view` nên xem user dashboard không vấn đề). Sidebar chỉ định hướng menu chính của họ.

### E. Loading & error states

- Hook nào đang loading → skeleton `<div className="h-9 w-24 animate-pulse rounded bg-muted" />` thay số.
- Error: toast (đã có `Toaster`), card fallback "Không tải được dữ liệu".

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Manual (WebApi + frontend đang chạy):
1. Login SALES → vào `/` → thấy 4 KPI (chỉ data mình) + funnel + activity + top KH/SP. URL search params sync khi đổi range.
2. Login ADMIN → sidebar redirect `/admin/dashboard` → leaderboard 5 sale, dropdown filter chọn 1 sale → mọi widget refresh. URL có `?saleUserId=...`.
3. SALES gõ tay `/admin/dashboard` → redirect `/403`.

## Exit Criteria

- 2 trang dashboard render đầy đủ 6 widget với data thật từ BE.
- Sidebar conditional chính xác theo permission.
- URL params (from/to/saleUserId) là source of truth — F5 giữ state.
- Lint + typecheck + build pass.
- Visual đúng tinh thần Linear-Vercel: card viền mảnh, monochrome + 1 accent, sparkline gọn 64px, badge pastel nhẹ.
