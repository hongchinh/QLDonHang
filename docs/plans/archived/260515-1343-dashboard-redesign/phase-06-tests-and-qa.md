# Phase 06 — Integration tests + responsive QA

**Status:** [ ] pending
**Complexity:** S

## Objective

Verify hành vi backend dashboard (scoping, delta, funnel, leaderboard) bằng integration test; verify component KPI card bằng unit test; manual smoke responsive 4 breakpoint.

## Files

- `backend/tests/OrderMgmt.IntegrationTests/Dashboard/DashboardEndpointsTests.cs` (new)
- `backend/tests/OrderMgmt.IntegrationTests/Dashboard/DashboardScopingTests.cs` (new)
- `frontend/src/features/dashboard/components/kpi-card.test.tsx` (new)
- `frontend/src/features/dashboard/use-dashboard-params.test.ts` (new)

## Tasks

### A. BE integration tests

Đọc pattern hiện có ở [backend/tests/OrderMgmt.IntegrationTests/Admin/](../../../backend/tests/OrderMgmt.IntegrationTests/Admin/) (vừa thêm tuần này) hoặc các test cũ cho `CustomersController` để biết WebApplicationFactory + Testcontainers setup.

Tests trong `DashboardScopingTests`:
- `Summary_AsSales_ReturnsOnlyOwnQuotations` — seed 2 user (sale1, sale2), 5 BG cho mỗi, login sale1 → summary.totalCount == 5.
- `Summary_AsAdmin_NoSaleFilter_ReturnsAll` — login admin, không filter → totalCount == 10.
- `Summary_AsAdmin_WithSaleFilter_ReturnsScoped` — login admin, `?saleUserId=<sale1>` → totalCount == 5.
- `Summary_AsSales_IgnoresSaleUserIdParam` — login sale1, `?saleUserId=<sale2>` → vẫn totalCount == 5 (data của sale1, không leak).
- `Summary_DeltaPct_ZeroPrevious_ReturnsNull` — seed BG chỉ trong range hiện tại, previous range rỗng → `deltaPct == null`.
- `Leaderboard_AsSales_Returns403` — sale1 gọi `/sales-leaderboard` → 403.
- `Leaderboard_AsAdmin_OrdersByRevenueDesc` — admin, top 5 → sort đúng.

Tests trong `DashboardEndpointsTests`:
- `RevenueSeries_Day_FillsZeroGaps` — seed 3 BG ở 3 ngày không liên tục, range 7 ngày → response có 7 points, ngày trống `total=0`.
- `Funnel_OnlyCountsWithinRange` — seed BG `Confirmed` ngày 2026-04-30 và 2026-05-05; range 2026-05-01 → 2026-05-15 → funnel chỉ đếm BG trong range.
- `TopCustomers_OrdersByRevenueDesc_LimitsCorrectly` — seed 7 customer, gọi `?limit=5` → trả 5 theo revenue.
- `RevenueRule_ExcludesCancelled` — BG `Status=Confirmed CancelledAt!=null` không cộng vào revenue.

### B. FE unit tests

`kpi-card.test.tsx` (Vitest + Testing Library):
- Render với `kpi.value = 1_234_567` `format='currency'` → text chứa "1.234.567" (định dạng VND).
- `kpi.deltaPct = 12.5` → badge "▲ 12.5%", class `bg-emerald-50`.
- `kpi.deltaPct = -3.2` → "▼ 3.2%", class `bg-rose-50`.
- `kpi.deltaPct = null` → "—", neutral badge.
- `kpi.spark = []` → không render `<svg>` sparkline.
- `kpi.spark = [1,2,3,4,5,6,7]` → render `<svg>` (Recharts).

`use-dashboard-params.test.ts`:
- Mount trong `<MemoryRouter initialEntries={['/dashboard?from=2026-05-01&to=2026-05-15']}>` → hook trả `from='2026-05-01' to='2026-05-15'`.
- `setPreset('7d')` → URL search params cập nhật.
- `setSaleUserId('abc')` → URL có `?saleUserId=abc`.
- `setSaleUserId(undefined)` → URL không còn `saleUserId`.

### C. Manual responsive QA

Checklist:
1. **1280px** (desktop default): grid 4 KPI / 4 col, row 3 chia đôi.
2. **1024px**: 4 KPI / 2 col, row 3 chia đôi.
3. **768px** (tablet): 4 KPI / 2 col, row 3 stack 1 col.
4. **375px** (mobile): 4 KPI / 1 col, mọi chart full width, sparkline không vỡ.
5. Tab navigation: TAB qua các button preset + dropdown filter + chart granularity → focus ring rõ ràng (`focus-visible:ring-2 ring-ring`).
6. Dark mode: nếu app có toggle, switch → mọi card đúng `border-border` `bg-card`, không lộ hardcoded hex.

### D. Cleanup

- Xoá file dev sandbox `frontend/src/features/dashboard/_sandbox.tsx` nếu Phase 03 tạo.
- Xoá hook `useQuotationStats` cũ trong `features/dashboard/hooks.ts` nếu không còn ai dùng (grep trước khi xoá).
- Hồi quy: load `/customers`, `/quotations`, `/products` → đảm bảo không gãy do refactor sidebar.

## Verification

```powershell
# Backend
dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~Dashboard"

# Frontend
npm --prefix frontend test
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

## Exit Criteria

- 100% test mới pass (backend + frontend).
- Manual responsive checklist 6 mục đều xanh.
- Không có regression ở các trang ngoài dashboard (customers/quotations/products/settings mở bình thường).
- `feedback_build_skip_when_app_running` được tôn trọng: không restart WebApi tay trong quá trình verify (chỉ rebuild library project).
