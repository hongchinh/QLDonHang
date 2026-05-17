# Phase 01 — Create route-permissions module + tests

**Status:** [x] complete
**Complexity:** S

## Objective
Tạo module `route-permissions.ts` export hàm `canAccessRoute(pathname, perms, roles)`
trả về `boolean`. Module chứa array RULES mirror các `<ProtectedRoute permission=...>`
trong `App.tsx`. Đi kèm Vitest test cover các nhánh quan trọng. Không thay đổi UI
trong phase này — phase 02 mới wire vào LoginPage.

## Files
- `frontend/src/lib/route-permissions.ts` (new)
- `frontend/src/lib/route-permissions.test.ts` (new)

## Tasks

1. Tạo `frontend/src/lib/route-permissions.ts` với content sau:

   ```ts
   import type { Permission, Role } from '@/lib/permissions';

   interface RouteRule {
     pattern: RegExp;
     permission?: Permission;
     role?: Role;
   }

   // Mirror các <ProtectedRoute permission=...> trong App.tsx.
   // Order matters: pattern cụ thể hơn đặt trước (vd /admin/users/:id/transfer-quotations
   // phải trước /admin/users/:id).
   // Routes không có rule = accessible cho mọi authenticated user.
   const RULES: RouteRule[] = [
     { pattern: /^\/admin\/users\/[^/]+\/transfer-quotations$/, permission: 'quotations.transfer_any' },
     { pattern: /^\/admin\/user-settings\/[^/]+$/, permission: 'user_settings.manage' },
     { pattern: /^\/admin\/users(\/[^/]+)?$/, permission: 'user_settings.manage' },
     { pattern: /^\/admin\/dashboard$/, permission: 'quotations.view_all' },
     { pattern: /^\/reports\/sales-performance$/, permission: 'quotations.view_all' },
     { pattern: /^\/reports\/(revenue|sales-revenue)$/, permission: 'reports.revenue' },
     { pattern: /^\/customers(\/[^/]+)?$/, permission: 'customers.view' },
     { pattern: /^\/products(\/[^/]+)?$/, permission: 'products.view' },
     { pattern: /^\/quotations(\/[^/]+)?$/, permission: 'quotations.view' },
   ];

   export function canAccessRoute(
     pathname: string,
     perms: readonly string[],
     roles: readonly string[],
   ): boolean {
     const rule = RULES.find((r) => r.pattern.test(pathname));
     if (!rule) return true;
     if (rule.permission && !perms.includes(rule.permission)) return false;
     if (rule.role && !roles.includes(rule.role)) return false;
     return true;
   }
   ```

2. Tạo `frontend/src/lib/route-permissions.test.ts` với content sau (theo pattern
   của `auth-store.test.ts` — Vitest, `describe`/`it`/`expect`):

   ```ts
   import { describe, expect, it } from 'vitest';
   import { canAccessRoute } from './route-permissions';

   const SALES_PERMS = [
     'customers.view', 'customers.create', 'customers.update',
     'products.view',
     'quotations.view', 'quotations.create', 'quotations.update',
     'quotations.print', 'quotations.transfer_own',
   ];

   describe('canAccessRoute', () => {
     it('allows route không có rule cho mọi user', () => {
       expect(canAccessRoute('/', [], [])).toBe(true);
       expect(canAccessRoute('/settings', [], [])).toBe(true);
       expect(canAccessRoute('/settings/my-quotation-settings', [], [])).toBe(true);
     });

     it('chặn /admin/dashboard với sale (thiếu view_all)', () => {
       expect(canAccessRoute('/admin/dashboard', SALES_PERMS, ['SALES'])).toBe(false);
     });

     it('cho phép /admin/dashboard nếu có view_all', () => {
       expect(canAccessRoute('/admin/dashboard', ['quotations.view_all'], ['ADMIN'])).toBe(true);
     });

     it('cho phép /quotations và /quotations/:id với sale', () => {
       expect(canAccessRoute('/quotations', SALES_PERMS, ['SALES'])).toBe(true);
       expect(canAccessRoute('/quotations/abc-123', SALES_PERMS, ['SALES'])).toBe(true);
       expect(canAccessRoute('/quotations/new', SALES_PERMS, ['SALES'])).toBe(true);
     });

     it('chặn /reports/sales-performance nếu thiếu view_all (kể cả có reports.revenue)', () => {
       expect(canAccessRoute('/reports/sales-performance', ['reports.revenue'], [])).toBe(false);
     });

     it('chặn /reports/revenue nếu thiếu reports.revenue, cho phép nếu có', () => {
       expect(canAccessRoute('/reports/revenue', [], [])).toBe(false);
       expect(canAccessRoute('/reports/revenue', ['reports.revenue'], [])).toBe(true);
       expect(canAccessRoute('/reports/sales-revenue', ['reports.revenue'], [])).toBe(true);
     });

     it('chặn /admin/users và /admin/users/:id nếu thiếu user_settings.manage', () => {
       expect(canAccessRoute('/admin/users', SALES_PERMS, ['SALES'])).toBe(false);
       expect(canAccessRoute('/admin/users/abc', SALES_PERMS, ['SALES'])).toBe(false);
     });

     it('phân biệt /admin/users/:id/transfer-quotations cần transfer_any', () => {
       // user_settings.manage không đủ — pattern cụ thể hơn match trước
       expect(canAccessRoute(
         '/admin/users/abc/transfer-quotations',
         ['user_settings.manage'],
         [],
       )).toBe(false);
       expect(canAccessRoute(
         '/admin/users/abc/transfer-quotations',
         ['quotations.transfer_any'],
         [],
       )).toBe(true);
     });
   });
   ```

3. Chạy verify ngay trong phase này để bắt lỗi syntax/regex sớm:

   ```
   cd frontend
   npm run test -- route-permissions
   npm run typecheck
   ```

## Verification

Chạy từ `frontend/`:

- `npm run test -- route-permissions` → tất cả test pass, không skipped.
- `npm run typecheck` → 0 errors.

## Exit Criteria

- `route-permissions.ts` tồn tại và export đúng `canAccessRoute`.
- `route-permissions.test.ts` tồn tại, chạy Vitest pass tất cả cases ở trên.
- TypeScript check không lỗi.
- Không thay đổi file khác — UI behavior chưa đổi.
