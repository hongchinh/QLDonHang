# Phase 03 — Frontend foundation: components + hooks + tokens

**Status:** [ ] pending
**Complexity:** M

## Objective

Xây tầng UI nguyên thủy có thể tái dùng cho cả 4 trang: `/dashboard`, `/admin/dashboard`, `/reports/revenue`, `/reports/sales-performance`. Bao gồm: design tokens, 7 component (kpi-card, range-picker, revenue-area-chart, status-funnel, top-list-card, activity-timeline, sales-leaderboard), hooks gọi 6 endpoint, hook URL search params.

## Files

### New
- `frontend/src/features/dashboard/types.ts` (edit — mở rộng)
- `frontend/src/features/dashboard/api.ts` (edit — thêm 6 endpoint)
- `frontend/src/features/dashboard/hooks.ts` (edit — thêm 6 hook)
- `frontend/src/features/dashboard/use-dashboard-params.ts` (new — URL params hook)
- `frontend/src/features/dashboard/format.ts` (new — `formatVnd`, `formatDelta`, `formatDate`)
- `frontend/src/features/dashboard/components/kpi-card.tsx` (new)
- `frontend/src/features/dashboard/components/range-picker.tsx` (new)
- `frontend/src/features/dashboard/components/revenue-area-chart.tsx` (new)
- `frontend/src/features/dashboard/components/status-funnel.tsx` (new)
- `frontend/src/features/dashboard/components/top-list-card.tsx` (new)
- `frontend/src/features/dashboard/components/activity-timeline.tsx` (new)
- `frontend/src/features/dashboard/components/sales-leaderboard.tsx` (new)

### Maybe install (kiểm tra trước)
- `@radix-ui/react-popover` — nếu range-picker dùng popover; nếu không có, fallback `dropdown-menu` đã có.

## Tasks

### A. Format helpers

`frontend/src/features/dashboard/format.ts`:
```typescript
export function formatVnd(value: number): string {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency', currency: 'VND', maximumFractionDigits: 0
  }).format(value);
}

export function formatDelta(deltaPct: number | null | undefined): {
  text: string; tone: 'positive' | 'negative' | 'neutral';
} {
  if (deltaPct == null) return { text: '—', tone: 'neutral' };
  const sign = deltaPct >= 0 ? '▲' : '▼';
  return {
    text: `${sign} ${Math.abs(deltaPct).toFixed(1)}%`,
    tone: deltaPct >= 0 ? 'positive' : 'negative'
  };
}

export function formatDateYmd(d: Date): string {
  return d.toISOString().slice(0, 10);
}
```

### B. Types

`features/dashboard/types.ts` — thêm (giữ `QuotationStats` cũ):
```typescript
export interface Kpi { value: number; deltaPct: number | null; spark: number[]; }
export interface Funnel { draft: number; sent: number; confirmed: number; cancelled: number; sentRate: number | null; confirmRate: number | null; }
export interface DashboardSummary {
  from: string; to: string; prevFrom: string; prevTo: string;
  todayRevenue: Kpi; rangeRevenue: Kpi; totalCount: Kpi; cancelledCount: Kpi;
  funnel: Funnel;
}
export interface RevenuePoint { date: string; total: number; confirmedCount: number; }
export interface RevenueSeries { points: RevenuePoint[]; }
export interface TopCustomer { customerId: string; customerName: string; revenue: number; quotationCount: number; }
export interface TopProduct { productId: string | null; productName: string; revenue: number; quantity: number; }
export interface ActivityItem { at: string; type: 'created' | 'confirmed' | 'cancelled'; quotationId: string; code: string; customerName: string; actorName: string | null; amount: number | null; }
export interface SalesLeaderboardItem { userId: string; fullName: string; revenue: number; confirmedCount: number; conversionRate: number | null; deltaPct: number | null; }

export type Granularity = 'day' | 'week' | 'month';
export interface DashboardParams { from: string; to: string; saleUserId?: string; }
```

### C. API client

`features/dashboard/api.ts`:
```typescript
import { apiGet } from '@/lib/api-client';
import type { ... } from './types';

export const dashboardApi = {
  getQuotationStats: ..., // giữ legacy
  getSummary:        (p: DashboardParams) => apiGet<DashboardSummary>('/dashboard/summary', p),
  getRevenueSeries:  (p: DashboardParams & { granularity: Granularity }) => apiGet<RevenueSeries>('/dashboard/revenue-series', p),
  getTopCustomers:   (p: DashboardParams & { limit?: number }) => apiGet<TopCustomer[]>('/dashboard/top-customers', p),
  getTopProducts:    (p: DashboardParams & { limit?: number }) => apiGet<TopProduct[]>('/dashboard/top-products', p),
  getRecentActivity: (p: { limit?: number }) => apiGet<ActivityItem[]>('/dashboard/recent-activity', p),
  getLeaderboard:    (p: { from: string; to: string; limit?: number }) => apiGet<SalesLeaderboardItem[]>('/dashboard/sales-leaderboard', p),
};
```

### D. Hooks

`features/dashboard/hooks.ts` — TanStack Query mỗi endpoint một hook, key đầy đủ params, `staleTime: 30_000`. Ví dụ:
```typescript
export function useDashboardSummary(params: DashboardParams) {
  return useQuery({
    queryKey: ['dashboard', 'summary', params],
    queryFn: () => dashboardApi.getSummary(params),
    staleTime: 30_000,
  });
}
```
Lặp lại tương tự cho 5 endpoint còn lại.

### E. URL params hook

`features/dashboard/use-dashboard-params.ts`:
```typescript
import { useSearchParams } from 'react-router-dom';

export function useDashboardParams() {
  const [sp, setSp] = useSearchParams();
  const today = new Date();
  const first = new Date(today.getFullYear(), today.getMonth(), 1);

  const from = sp.get('from') ?? first.toISOString().slice(0, 10);
  const to   = sp.get('to')   ?? today.toISOString().slice(0, 10);
  const saleUserId = sp.get('saleUserId') ?? undefined;

  const setRange = (f: string, t: string) => {
    sp.set('from', f); sp.set('to', t); setSp(sp, { replace: true });
  };
  const setSaleUserId = (id?: string) => {
    if (id) sp.set('saleUserId', id); else sp.delete('saleUserId');
    setSp(sp, { replace: true });
  };
  const setPreset = (preset: 'today' | '7d' | '30d' | 'this-month' | 'last-month') => { /* compute, setRange */ };

  return { from, to, saleUserId, setRange, setSaleUserId, setPreset };
}
```

### F. Components

#### `kpi-card.tsx`
Props: `{ label: string; value: number | string; format?: 'currency' | 'number'; kpi?: Kpi; }`. Render:
- Card: `<Card className="rounded-xl border-border bg-card p-5">`.
- Label: `<p className="text-xs uppercase tracking-wide text-muted-foreground">{label}</p>`.
- Value row: số to `text-3xl font-semibold tracking-tight tabular-nums` + delta badge inline.
- Delta badge: `tone === 'positive'` → `bg-emerald-50 text-emerald-700`, negative → `bg-rose-50 text-rose-700`, neutral → `bg-muted text-muted-foreground`.
- Sparkline: chỉ render khi `kpi?.spark.length`. Recharts `<AreaChart>` `width="100%"` `height={64}` margin all 0; `<defs>` gradient `currentColor/30 → currentColor/0`; `<Area type="monotone" dataKey="v" stroke="currentColor" strokeWidth={1.5} fill="url(#g)" />`; `<XAxis hide />` `<YAxis hide />` `<Tooltip content={() => null} cursor={false} />`.

#### `range-picker.tsx`
Layout: hàng chip preset `[Hôm nay | 7N | 30N | Tháng này | Tháng trước | Tuỳ chỉnh ▾]`.
- Chip preset = `<button>` với active state `bg-foreground text-background`.
- "Tuỳ chỉnh" — popover/dropdown chứa 2 `<input type="date">` + nút "Áp dụng".
- Nếu không có `@radix-ui/react-popover` → dùng `<details>`/`<summary>` native hoặc Radix Dialog đã có.
- Emit `(from, to)` cho parent.

#### `revenue-area-chart.tsx`
Props: `{ points: RevenuePoint[]; granularity: Granularity; onGranularityChange: (g) => void; }`.
- Header trong card: title "Doanh thu" + `<Tabs>` `[Ngày | Tuần | Tháng]` (build từ shadcn Button hoặc plain `<button>`).
- Recharts `<AreaChart>` height 280, axis có label, format VND y-axis bằng `formatVnd` short (`₫340M`).
- Stroke `var(--primary)` opacity 0.9, gradient fill, grid dashed `#F0F0F0`.
- Empty state: nếu `points.every(p => p.total === 0)` → render "Chưa có dữ liệu" thay chart.

#### `status-funnel.tsx`
Props: `Funnel`.
- 4 thanh ngang chiều dài tỉ lệ với `max(draft, sent, confirmed, cancelled)`.
- Label + count + tỉ lệ chuyển đổi giữa các bước.
- Không dùng lib funnel — chỉ `<div>` với `width: ${pct}%`.

#### `top-list-card.tsx`
Props: `{ title: string; items: Array<{ id: string; name: string; primary: number; secondary?: number | string }>; format?: 'currency' | 'number' }`.
- Card có header title + 5 row.
- Row: avatar 24px (initial), name truncate, right side `primary` format currency + small `secondary`.
- Reusable cho cả Top khách + Top sản phẩm.

#### `activity-timeline.tsx`
Props: `{ items: ActivityItem[] }`.
- Mỗi row: dot màu theo type (`created` xám, `confirmed` emerald, `cancelled` rose), timestamp relative (`date-fns formatDistanceToNow`), text "QT-001 cho KH XYZ".
- Click row → navigate `/quotations/{id}` (nếu permission).

#### `sales-leaderboard.tsx`
Props: `{ items: SalesLeaderboardItem[] }`.
- Top 5 row: rank số + avatar + tên + revenue + progress bar tỉ lệ với top 1 + delta badge.

### G. Recharts global styling

Trong [frontend/src/styles/](../../../frontend/src/) (file tailwind globals) đảm bảo có CSS variable `--primary` để Recharts dùng `var(--primary)`. Nếu chưa có, dùng thẳng `hsl(var(--primary))` trong props.

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
```

Storybook không có — viết 1 file dev sandbox tạm `frontend/src/features/dashboard/_sandbox.tsx` (gitignore) để render từng component với mock data, mount vào `/dashboard` tạm để mắt nhìn → xóa sau Phase 04.

## Exit Criteria

- 7 component file tồn tại, export default đúng, accept đúng props.
- `useDashboardSummary`, `useRevenueSeries`, `useTopCustomers`, `useTopProducts`, `useRecentActivity`, `useLeaderboard` hook hoạt động với TanStack Query DevTools (xem network tab khi mount sandbox).
- `useDashboardParams` đồng bộ URL ↔ state; refresh page giữ nguyên params.
- Lint + typecheck pass.
- Sandbox render mock data đúng visual: KPI card có sparkline + delta, range-picker chuyển preset, area chart vẽ, funnel hiển thị.
