# Phase 02 — Wire route-permissions vào LoginPage + verify

**Status:** [x] complete
**Complexity:** S

## Objective
Sửa `LoginPage.tsx` để dùng `canAccessRoute` từ phase 01. Sau khi login thành công
(hoặc trong nhánh `isAuthenticated` redirect), nếu `from` route không accessible
cho user → fallback về `/`. Cũng exclude `/login` và `/403` để tránh loop.

## Files
- `frontend/src/pages/login-page.tsx` (modify)

## Tasks

1. Mở `frontend/src/pages/login-page.tsx`.

2. Thêm 2 import:
   - `import { canAccessRoute } from '@/lib/route-permissions';`
   - Đảm bảo có `useAuthStore` import (đã có) — sẽ dùng `useAuthStore.getState().user`
     trong nhánh `isAuthenticated` để truy permissions.

3. Thêm helper local (ngay trên function component, sau imports):

   ```ts
   function pickPostLoginTarget(
     from: string | undefined,
     perms: readonly string[],
     roles: readonly string[],
   ): string {
     if (!from || from.startsWith('/login') || from.startsWith('/403')) return '/';
     return canAccessRoute(from, perms, roles) ? from : '/';
   }
   ```

4. Sửa nhánh `if (isAuthenticated)` (hiện tại ở dòng ~32-35):

   ```tsx
   if (isAuthenticated) {
     const currentUser = useAuthStore.getState().user;
     const target = pickPostLoginTarget(
       location.state?.from?.pathname,
       currentUser?.permissions ?? [],
       currentUser?.roles ?? [],
     );
     return <Navigate to={target} replace />;
   }
   ```

5. Sửa `onSubmit` → `onSuccess` (hiện tại ở dòng ~38-50):

   ```ts
   const onSubmit = (values: FormValues) => {
     login.mutate(values, {
       onSuccess: (data) => {
         toast({ variant: 'success', title: 'Đăng nhập thành công' });
         const target = pickPostLoginTarget(
           location.state?.from?.pathname,
           data.user.permissions,
           data.user.roles,
         );
         navigate(target, { replace: true });
       },
       onError: (err) => {
         toast({
           variant: 'destructive',
           title: 'Đăng nhập thất bại',
           description: getErrorMessage(err),
         });
       },
     });
   };
   ```

   Lưu ý: kiểu của `data` ở `onSuccess` là `LoginResponse` (xem
   `frontend/src/features/auth/api.ts`). Confirm `data.user.permissions: string[]`
   và `data.user.roles: string[]` đều có trên type.

6. Chạy verify ngay sau khi sửa:

   ```
   cd frontend
   npm run typecheck
   npm run lint
   npm run test
   npm run build
   ```

## Verification

### Automated (chạy từ `frontend/`)

- `npm run typecheck` → 0 errors.
- `npm run lint` → 0 errors (chấp nhận warning có sẵn).
- `npm run test` → tất cả test pass (gồm `route-permissions.test.ts` từ phase 01
  và auth-store + dashboard tests đã có).
- `npm run build` → build thành công, không có TypeScript error.

### Manual smoke test (dev local)

1. `cd frontend && npm run dev` (cần backend chạy song song).
2. Mở browser ở http://localhost:5173, đảm bảo chưa login (clear cookies nếu cần).
3. Paste URL `http://localhost:5173/admin/dashboard` → bị redirect về `/login`
   (URL bar hiện `/login`).
4. Login bằng tài khoản sale.
5. **Verify**: landing trên `/` (Dashboard), KHÔNG phải `/403`.
6. Type `/admin/dashboard` vào address bar sau khi đã login as sale.
7. **Verify**: lúc này bị redirect `/403` (ProtectedRoute không đổi, đây là hành
   vi đúng).
8. Logout → login lại as admin (`Admin@123`).
9. Lặp lại bước 3: paste `/admin/dashboard` → bị về `/login` → login admin.
10. **Verify**: admin land trên `/admin/dashboard` (route gốc) vì có quyền.

### Manual smoke test (production sau deploy)

Sau khi `git push` lên main và Railway build xong, lặp lại 10 bước trên với
`https://frontend-production-988a.up.railway.app`.

## Exit Criteria

- LoginPage navigate về `/` khi user không có quyền cho `from`.
- LoginPage navigate về `from` khi user có quyền.
- Cả 2 nhánh (`isAuthenticated` redirect và `onSuccess`) đều dùng helper.
- `/login` và `/403` không gây loop khi xuất hiện trong `from`.
- Tất cả automated checks pass.
- Manual smoke test pass cho cả sale và admin.
