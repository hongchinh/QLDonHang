# Phase 04 — Routing + sidebar wiring

**Status:** [x] complete
**Complexity:** S

## Objective

Wire 2 page mới (`UsersListPage`, `SettingsHubPage`) vào `App.tsx`; thay placeholder `/settings`; thêm 2 nav item vào `app-layout.tsx`; bỏ nav item placeholder `/settings` cũ để tránh trùng. Sau phase này UX khép kín — user/admin tìm thấy lối vào trực tiếp từ sidebar.

## Files

**Modified**
- `frontend/src/App.tsx`
- `frontend/src/components/layout/app-layout.tsx`

## Tasks

### `App.tsx`

1. Thêm 2 import:
   ```ts
   import { UsersListPage } from '@/pages/admin/users-list-page';
   import { SettingsHubPage } from '@/pages/settings/settings-hub-page';
   ```

2. Thêm route `/admin/users` (đặt cạnh route `/admin/user-settings/:userId` để dễ tìm), sau dòng 135 (cuối block route admin):
   ```tsx
   <Route
     path="admin/users"
     element={
       <ProtectedRoute permission="user_settings.manage">
         <UsersListPage />
       </ProtectedRoute>
     }
   />
   ```

3. Thay route `/settings` placeholder (App.tsx:148):
   - **Trước**: `<Route path="settings" element={<ProtectedRoute requireRole="ADMIN">{PLACEHOLDER('Cấu hình hệ thống')}</ProtectedRoute>} />`
   - **Sau**: `<Route path="settings" element={<SettingsHubPage />} />`
   - Bỏ `ProtectedRoute requireRole="ADMIN"` — landing tự lọc card theo permission; user thường vẫn truy cập được (chỉ thấy 1 card).

4. Nếu `PLACEHOLDER` helper không còn route nào dùng → giữ nguyên (`/orders`, `/deliveries`, `/payments`, `/reports` vẫn cần). Không xóa.

### `app-layout.tsx`

5. Mở `frontend/src/components/layout/app-layout.tsx`. Hiện tại `navItems` (line 31-41) có 9 entry cuối là `/settings` với `role: 'ADMIN'`.

6. Cập nhật import icons — thêm:
   ```ts
   import {
     // ...existing icons,
     UserCog,
     Users2,
   } from 'lucide-react';
   ```

7. Cập nhật `navItems`:
   - **Bỏ** entry `/settings` cũ (placeholder admin-only) — vì hub đã có 2 entry cụ thể hơn ở dưới.
   - **Thêm** 2 entry mới, đặt sau `/quotations` để gom các "lối vào" liên quan báo giá:
     ```ts
     { to: '/settings/my-quotation-settings', label: 'Cài đặt của tôi', icon: UserCog },
     { to: '/admin/users', label: 'Quản lý người dùng', icon: Users2, permission: 'user_settings.manage' },
     ```
   - Kết quả `navItems` cuối cùng (theo thứ tự):
     ```ts
     const navItems: NavItem[] = [
       { to: '/', label: 'Tổng quan', icon: LayoutDashboard },
       { to: '/customers', label: 'Khách hàng', icon: Users, permission: 'customers.view' },
       { to: '/products', label: 'Hàng hóa', icon: Package, permission: 'products.view' },
       { to: '/quotations', label: 'Báo giá', icon: FileText, permission: 'quotations.view' },
       { to: '/settings/my-quotation-settings', label: 'Cài đặt của tôi', icon: UserCog },
       { to: '/admin/users', label: 'Quản lý người dùng', icon: Users2, permission: 'user_settings.manage' },
       { to: '/orders', label: 'Đơn hàng', icon: ClipboardList, permission: 'orders.view' },
       { to: '/deliveries', label: 'Bàn giao', icon: Truck, permission: 'orders.deliver' },
       { to: '/payments', label: 'Thanh toán & Công nợ', icon: Wallet, permission: 'orders.pay' },
       { to: '/reports', label: 'Báo cáo', icon: BarChart3, permission: 'reports.revenue' },
     ];
     ```

8. Verify import cũ `Settings` icon nếu không còn dùng → có thể xoá khỏi import. Không bắt buộc — giữ cũng không gây warning.

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Manual:
1. Đăng nhập ADMIN → sidebar có cả "Cài đặt của tôi" và "Quản lý người dùng"; **không** có "Cấu hình" cũ.
2. Đăng nhập SALES1 → sidebar có "Cài đặt của tôi", **không** có "Quản lý người dùng".
3. Click "Quản lý người dùng" (ADMIN) → vào trang list users ổn.
4. SALES1 gõ tay `/admin/users` → redirect `/403`.
5. ADMIN gõ `/settings` → vào hub mới với 2 card.

## Exit Criteria

- `App.tsx` có route `/admin/users` (guarded) và route `/settings` trỏ `SettingsHubPage`.
- `app-layout.tsx` có 2 nav entry mới + đã bỏ entry `/settings` cũ.
- Typecheck + lint + build pass.
- Manual 5 case ở trên đều đúng.
- Sidebar không có duplicate hoặc nav item dẫn tới placeholder.
