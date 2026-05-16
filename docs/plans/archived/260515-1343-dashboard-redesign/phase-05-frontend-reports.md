# Phase 05 — Frontend sub-pages: /reports/revenue + /reports/sales-performance

**Status:** [ ] pending
**Complexity:** M

## Objective

Build 2 trang drill-down dùng lại component Phase 03:
- `/reports/revenue` — chi tiết doanh thu (cả 2 role, BE scope theo permission).
- `/reports/sales-performance` — hiệu suất sale (admin-only).

Thay thế placeholder hiện có ở route `/reports` trong [App.tsx](../../../frontend/src/App.tsx).

## Files

- `frontend/src/pages/reports/revenue-page.tsx` (new)
- `frontend/src/pages/reports/sales-performance-page.tsx` (new)
- `frontend/src/App.tsx` (edit — thay route `/reports` placeholder bằng 2 route mới)
- Có thể thêm endpoint `/api/dashboard/revenue-detail` ở Phase 02 nếu cần bảng theo ngày + filter customer (xem optional task dưới); v1 dùng `revenue-series` + `top-customers` đã có.

## Tasks

### A. `/reports/revenue`

Layout:
```
┌─────────────────────────────────────────────────────────────┐
│ H1: Doanh thu                          [Range ▾] [Export]   │
│ [Sale ▾]  (admin) │ [Customer ▾]                            │
├─────────────────────────────────────────────────────────────┤
│ 3 KPI: Tổng doanh thu │ Số BG confirmed │ Trung bình/BG     │
├─────────────────────────────────────────────────────────────┤
│ Revenue chart (full width, granularity tabs)                │
├──────────────────────────────┬──────────────────────────────┤
│ Bảng doanh thu theo ngày     │ Top khách (top-list-card)    │
│ (DataTable: date, count,     │                              │
│  revenue, avg)               │                              │
└──────────────────────────────┴──────────────────────────────┘
```

- Header H1 + RangePicker + (conditional) SaleFilterDropdown.
- 3 KPI dẫn xuất từ `useDashboardSummary` (`rangeRevenue`, `totalCount - cancelledCount`, avg = revenue/count).
- Revenue chart reuse `<RevenueAreaChart>`.
- Bảng theo ngày: build từ `useRevenueSeries({ granularity: 'day' })`, render `@tanstack/react-table` (đã có trong package.json) hoặc table thường.
- Top khách reuse `<TopListCard>`.
- Export button (Phase v1 = stub `console.log` + toast "Sắp ra mắt"; Excel export để phase sau).

### B. `/reports/sales-performance` (admin-only)

Layout:
```
┌─────────────────────────────────────────────────────────────┐
│ H1: Hiệu suất sale                     [Range ▾]            │
├─────────────────────────────────────────────────────────────┤
│ Bảng leaderboard đầy đủ (full width):                       │
│ Rank │ Sale │ Confirmed │ Revenue │ Conversion │ Delta WoW  │
│ (sortable, paginated nếu > 20)                              │
├──────────────────────────────┬──────────────────────────────┤
│ Bar chart so sánh revenue    │ Bar chart so sánh conversion │
│ theo sale (top 10)           │ theo sale (top 10)           │
└─────────────────────────────────────────────────────────────┘
```

- Reuse `useLeaderboard({ from, to, limit: 50 })`.
- DataTable từ `@tanstack/react-table` — sort theo column, format VND.
- 2 bar chart: Recharts `<BarChart>` orientation horizontal, dùng leaderboard data top 10.
- Click row → navigate `/reports/revenue?saleUserId={userId}` để drill xuống chi tiết sale đó.

### C. Routing

Trong [App.tsx](../../../frontend/src/App.tsx) thay block:
```tsx
<Route path="reports" element={<ProtectedRoute permission="reports.revenue">{PLACEHOLDER('Báo cáo')}</ProtectedRoute>} />
```
bằng:
```tsx
<Route path="reports">
  <Route index element={<Navigate to="revenue" replace />} />
  <Route
    path="revenue"
    element={<ProtectedRoute permission="reports.revenue"><RevenuePage /></ProtectedRoute>}
  />
  <Route
    path="sales-performance"
    element={<ProtectedRoute permission="quotations.view_all"><SalesPerformancePage /></ProtectedRoute>}
  />
</Route>
```

Import:
```tsx
import { RevenuePage } from '@/pages/reports/revenue-page';
import { SalesPerformancePage } from '@/pages/reports/sales-performance-page';
```

Sidebar: thêm submenu "Báo cáo > Doanh thu" + "Báo cáo > Hiệu suất sale" (admin only). Pattern menu xem `app-layout.tsx`.

### D. Optional: BE endpoint `revenue-detail` (skip nếu không cần)

Nếu bảng theo ngày cần thêm cột "Số khách hàng duy nhất" hoặc filter `customerId`, thêm endpoint `GET /api/dashboard/revenue-detail?from=&to=&customerId=&saleUserId=` trả mảng `{ date, count, revenue, uniqueCustomers }`. Phase v1 **skip**, dùng `revenue-series` + group client-side.

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Manual:
1. SALES → `/reports/revenue` mở được, không có dropdown sale, data chỉ riêng mình.
2. ADMIN → `/reports/revenue` có dropdown sale, switch sale → chart + bảng refresh.
3. ADMIN → `/reports/sales-performance` → leaderboard đầy đủ, click row → `/reports/revenue?saleUserId=...` mở đúng sale đó.
4. SALES gõ tay `/reports/sales-performance` → redirect `/403`.

## Exit Criteria

- 2 trang report render đúng layout, data realtime từ BE.
- Sidebar có submenu "Báo cáo" với 2 mục, admin-only mục thứ 2.
- Click row leaderboard chuyển trang giữ saleUserId qua URL params.
- Lint + typecheck + build pass.
