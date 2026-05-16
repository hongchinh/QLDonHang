# Dashboard redesign — User/Admin minimalist dashboard + sub-pages

## Problem framing

Dashboard hiện tại tại [frontend/src/pages/dashboard-page.tsx](../../../frontend/src/pages/dashboard-page.tsx) chỉ có 4 KPI card + 1 card đếm trạng thái, không có chart, không có drill-down. Sau khi pivot sang "Báo giá là chứng từ duy nhất" (xem [brainstorm 260515-1249](../260515-1249-quotation-only-pivot/SUMMARY.md)), doanh thu được ghi nhận theo `OwnerUserId` của Quotation khi `Status = Confirmed`. Cần một dashboard hiện đại, hiển thị đủ:

- Trend doanh thu theo thời gian.
- Phễu trạng thái `Draft → Sent → Confirmed`.
- Top khách hàng / top sản phẩm.
- Hoạt động gần đây (user) hoặc leaderboard sale (admin).

User chỉ thấy data của mình; admin (`quotations.view_all`) thấy toàn hệ thống và filter theo từng sale.

## Goals

- Redesign `/dashboard` với UI Linear-Vercel Minimalist + sparkline trong KPI card.
- Tạo `/admin/dashboard` riêng cho admin với leaderboard và filter sale.
- Mở 2 sub-page drill-down: `/reports/revenue`, `/reports/sales-performance`.
- Mọi widget scope theo permission `quotations.view_all` ở backend, không tin client.
- Range picker preset chips + custom popover, sync state qua URL search params.

## Non-goals

- Không xây dark mode toàn app trong phase này (token sẵn sàng nhưng không bật).
- Không in/export PDF dashboard.
- Không real-time push (chấp nhận polling/refetch qua TanStack Query).
- Không tracking thanh toán/công nợ (đã loại khỏi scope sản phẩm).

## Constraints & assumptions

- Stack hiện tại: React + Vite + TanStack Query + shadcn/ui + Tailwind. Backend .NET 9 Clean Architecture + EF Core + Npgsql.
- `Quotation` đã có `ConfirmedAt`, `OwnerUserId` (sau pivot quotation-only) → đủ cho mọi widget.
- Build skip khi WebApi đang chạy — chỉ build library project liên quan (xem memory `feedback_build_skip_when_app_running`).
- Permission hiện tại: `quotations.view_all`, `Reports.SalesRevenue`. Sẽ thêm `Dashboard.View` (mặc định mọi role) và tận dụng `quotations.view_all` cho admin dashboard + leaderboard.

## Approaches considered

### A. Linear-Vercel Minimalist (chosen, base)
- **Pros**: B2B-friendly, hòa với shadcn/ui hiện có, đọc số nhanh, scale ra sub-pages cùng ngôn ngữ.
- **Cons**: Ít "wow"; đòi typography/spacing chuẩn.
- **Complexity**: trung bình.

### B. Tremor/Stripe Data-Dense
- **Pros**: Ấn tượng analytics, Tremor blocks accelerate dev.
- **Cons**: Nhiều màu pastel, lệch hướng minimalist, mix với shadcn cần token hóa.
- **Complexity**: trung bình-cao.

### C. Glassmorphism Fintech (Dark + gradient + glow)
- **Pros**: Wow-factor cao, viral-friendly cho demo.
- **Cons**: Số tiền VNĐ khó đọc, mâu thuẫn với quotation form light-mode, tăng chi phí maintain.
- **Complexity**: cao.

## Recommended approach

**Mix A + KPI-sparkline từ B**: base Linear-Vercel Minimalist, mỗi KPI card thêm sparkline 7 ngày + delta badge pastel nhẹ (`bg-emerald-50` positive, `bg-rose-50` negative). Giữ phần còn lại của UI monochrome + 1 accent.

### Quyết định cụ thể đã chốt

| Vấn đề | Quyết định |
|---|---|
| Style chung | Linear-Vercel Minimalist (card viền mảnh, không shadow, monochrome + 1 accent) |
| KPI cards | 4 card cũ + sparkline 7 ngày + delta badge (mix B) |
| Tách user/admin | Tách thành 2 trang: `/dashboard` (user) và `/admin/dashboard` (admin) |
| Sub-pages | Có: `/reports/revenue` (cả 2), `/reports/sales-performance` (admin) |
| Range default | Tháng hiện tại; preset chips + custom popover |
| State range/filter | URL search params (`?from&to&saleUserId`) làm source of truth |
| Chart library | Recharts (đã có shadcn wrapper) |
| Permission scoping | Backend ép `saleUserId = currentUser.Id` nếu không có `quotations.view_all` |

## Routes & permissions

| Route | Permission | Vai trò |
|---|---|---|
| `/dashboard` | mọi user đã login | User dashboard — chỉ data của user, ẩn leaderboard |
| `/admin/dashboard` | `quotations.view_all` | Admin dashboard — toàn hệ thống, leaderboard, filter sale |
| `/reports/revenue` | `Reports.SalesRevenue` | Drill-down doanh thu (scope theo permission) |
| `/reports/sales-performance` | `quotations.view_all` | Hiệu suất sale (admin-only) |

Sidebar: nếu có `quotations.view_all` → menu "Dashboard" trỏ `/admin/dashboard` + link "Của tôi" trỏ `/dashboard`. Nếu không → chỉ thấy `/dashboard`.

## Layout `/dashboard` & `/admin/dashboard`

```
┌──────────────────────────────────────────────────────────────┐
│ H1 + Subtitle range                            [Range ▾]    │
│ [Tất cả sale ▾]  (chỉ admin)                                 │
├──────────────────────────────────────────────────────────────┤
│ Row 1: 4 KPI cards (sparkline + delta badge)                │
│   Hôm nay │ Khoảng │ Tổng BG │ Đã huỷ                       │
├──────────────────────────────────────────────────────────────┤
│ Row 2: Revenue area chart, full width      [Day|Week|Month] │
├──────────────────────────────┬───────────────────────────────┤
│ Row 3-left: Funnel           │ Row 3-right (user):          │
│ Draft → Sent → Confirmed     │   Hoạt động gần đây           │
│ + tỷ lệ chuyển đổi           │ Row 3-right (admin):         │
│                              │   Leaderboard sale (top 5)   │
├──────────────────────────────┴───────────────────────────────┤
│ Row 4: Top khách hàng (table-5) │ Top sản phẩm (table-5)    │
└──────────────────────────────────────────────────────────────┘
```

## API endpoints mới

```http
GET /api/dashboard/summary?from=&to=&saleUserId=
GET /api/dashboard/revenue-series?from=&to=&granularity=day|week|month&saleUserId=
GET /api/dashboard/top-customers?from=&to=&limit=5&saleUserId=
GET /api/dashboard/top-products?from=&to=&limit=5&saleUserId=
GET /api/dashboard/recent-activity?limit=10
GET /api/dashboard/sales-leaderboard?from=&to=&limit=10   [admin-only]
```

Scoping: tất cả service nhận `ICurrentUser`. Nếu `!HasPermission("quotations.view_all")` → ép `saleUserId = currentUser.Id` (override query param). Leaderboard endpoint check `view_all` ngay đầu method → throw `ForbiddenException` nếu không có.

Revenue rule: chỉ tính `Status = Confirmed AND CancelledAt IS NULL AND ConfirmedAt IN [from, to]`.

Delta: `(current - previous) / previous * 100`; previous range = cùng độ dài lùi liền kề; nếu `previous = 0` → trả `null` (FE hiển thị `—`).

## Frontend cấu trúc

```
frontend/src/features/dashboard/
  api.ts, types.ts, hooks.ts
  components/
    kpi-card.tsx          # number + delta badge + sparkline 64px
    revenue-area-chart.tsx
    status-funnel.tsx     # custom bar
    top-list-card.tsx     # reusable customer + product
    activity-timeline.tsx
    sales-leaderboard.tsx
    range-picker.tsx      # preset chips + popover
frontend/src/pages/
  dashboard-page.tsx                  # user (refactor)
  admin/admin-dashboard-page.tsx      # mới
  reports/revenue-page.tsx
  reports/sales-performance-page.tsx
```

### Design tokens
- Card: `border border-border bg-card rounded-xl p-5` (không shadow).
- Số KPI: `text-3xl font-semibold tracking-tight tabular-nums`.
- Delta badge: `bg-emerald-50 text-emerald-700` / `bg-rose-50 text-rose-700`, `text-xs px-1.5 py-0.5 rounded`.
- Sparkline: `<AreaChart>` cao 64px, stroke `currentColor` opacity 0.8, gradient fill `currentColor/30 → currentColor/0`, không trục/legend.

## Implementation outline (5 phase)

1. **Backend endpoints + permission seed** (~1.5 ngày) — 6 endpoint, service scoping, seed `Dashboard.View`. Build chỉ Application + WebApi.
2. **FE foundation** (~1 ngày) — tokens, `kpi-card`, `range-picker`, Recharts setup, URL params hook.
3. **FE pages** (~1.5 ngày) — refactor `/dashboard`, build `/admin/dashboard`, sidebar conditional.
4. **FE sub-pages** (~1 ngày) — `/reports/revenue`, `/reports/sales-performance`.
5. **QA + responsive** (~0.5 ngày) — integration tests, manual checklist, responsive smoke 1280/1024/768/375.

**Tổng**: ~5-6 ngày dev. Có thể split PR theo phase.

## Verification

### Backend integration tests (`OrderMgmt.IntegrationTests/Dashboard/`)
- `Summary_AsSale_ReturnsOnlyOwnQuotations`
- `Summary_AsAdmin_WithoutSaleFilter_ReturnsAll`
- `Summary_AsAdmin_WithSaleFilter_ReturnsScoped`
- `Summary_DeltaPct_ZeroPrevious_ReturnsNull`
- `Leaderboard_AsNonAdmin_Forbidden`
- `RevenueSeries_Granularity_Day_FillsZeroGaps`
- `Funnel_OnlyCountsWithinRange`

### Frontend
- Unit test `kpi-card`: delta positive/negative/zero, format VND, sparkline đủ N điểm.
- Manual: SALES vào `/dashboard` → chỉ thấy data riêng, sidebar không có admin link; ADMIN vào `/admin/dashboard` → leaderboard + dropdown filter sale; switch sale → mọi widget refresh.
- Responsive: 1280 / 1024 / 768 / 375, grid 4→2→1.

## Risks & mitigations

| Risk | Mitigation |
|---|---|
| Revenue-series query chậm với data lớn | Index `(OwnerUserId, ConfirmedAt) WHERE Status=3 AND IsDeleted=false`; cache 60s ở Application |
| Sparkline làm KPI request nặng | Tính trong cùng query với window function PostgreSQL, không N+1 |
| Admin filter + range + URL params phức tạp | URL search params làm source of truth → bookmark/share được |
| Sidebar conditional nhấp nháy khi auth load | Skeleton sidebar trong khi `useAuthStore` chưa hydrate |
| Hiển thị `Infinity` khi previous=0 | Trả `null` từ BE, FE render `—` |

## Open questions

- Index DB `(OwnerUserId, ConfirmedAt, Status)` đã tồn tại chưa? Cần kiểm tra trước phase BE.
- KPI doanh thu mặc định `Total` (gồm thuế + cước) hay có toggle Subtotal/Total trên UI?
- "Doanh thu khoảng" có loại trừ BG Cancelled không (đề xuất: có, đã ghi trong revenue rule).
- Top khách hàng / top sản phẩm tính theo `Total` hay `Subtotal`? (Đề xuất: `Total` đồng nhất với KPI.)

## Next steps

- Tùy chọn: trigger skill `write-plan` với artifacts này để dựng plan execution phase-by-phase.
- Kiểm tra index DB trước khi vào Phase 1 BE.
- Confirm 3 open questions ở trên với stakeholder.
