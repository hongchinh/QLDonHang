# Phase 03 — Wire route + sidebar + doc + smoke

**Status:** [x] complete
**Complexity:** S

## Objective

Wire route `/admin/roles` vào `App.tsx`, thêm nav item vào sidebar, cập nhật `route-permissions.ts` + test, ghi note thay đổi vào doc. Sau phase này feature available end-to-end và admin có thể vào màn hình từ menu.

## Files

### Modify

- `frontend/src/App.tsx` — thêm `<Route path="admin/roles" ...>`
- `frontend/src/lib/route-permissions.ts` — thêm rule `/admin/roles` → `roles.view`
- `frontend/src/lib/route-permissions.test.ts` — thêm 2 test case
- `frontend/src/components/layout/app-layout.tsx` — thêm nav item "Phân quyền" vào group "Setting"
- `docs/architecture/system-architecture.md` — thêm note Authorization + DbSeeder behavior

## Tasks

### A. Route

1. Trong `frontend/src/App.tsx`, sau block `admin/users/:userId/transfer-quotations` (line ~141-147):
   ```tsx
   <Route
     path="admin/roles"
     element={
       <ProtectedRoute permission="roles.view">
         <RolesMatrixPage />
       </ProtectedRoute>
     }
   />
   ```
2. Import: `import { RolesMatrixPage } from '@/pages/admin/roles-matrix-page';` cạnh các import admin page hiện có.

### B. Route permissions

3. Trong `frontend/src/lib/route-permissions.ts` array `RULES`, thêm entry trước rule `/admin/dashboard`:
   ```ts
   { pattern: /^\/admin\/roles$/, permission: 'roles.view' },
   ```
4. Trong `frontend/src/lib/route-permissions.test.ts`, thêm describe block hoặc `it` mới:
   - `expect(canAccessRoute('/admin/roles', SALES_PERMS, ['SALES'])).toBe(false);`
   - `expect(canAccessRoute('/admin/roles', ['roles.view'], [])).toBe(true);`

### C. Sidebar

5. Trong `frontend/src/components/layout/app-layout.tsx` array `navGroups`, group `'Setting'`, sau entry `'admin/users'`:
   ```ts
   { to: '/admin/roles', label: 'Phân quyền', icon: ShieldCheck, permission: 'roles.view' },
   ```
6. Import `ShieldCheck` từ `lucide-react` (thêm vào import list ở đầu file).

### D. Documentation

7. Trong `docs/architecture/system-architecture.md` section "Authorization", thêm 1 bullet về flow phân quyền runtime + DbSeeder behavior change. Đề xuất nội dung:
   ```markdown
   ## Role × Permission management
   
   - `/api/admin/roles` (gate `roles.view`/`roles.manage`) cho phép quản lý ma trận Role × Permission qua UI `/admin/roles`.
   - ADMIN role bị server từ chối mọi mutation lên `RolePermissions` (luôn full quyền).
   - System role khác (SALES/ACCOUNTANT/WAREHOUSE/MANAGER): admin có thể thêm/bớt permission; chỉ chặn rename/delete (`IsSystem=true`).
   - Custom role: full CRUD; xoá bị chặn nếu còn user gán.
   - **DbSeeder behavior** (`SeedRolesAsync`): ADMIN re-apply full permissions mỗi startup; các role khác (system + custom) chỉ seed permissions khi `RolePermissions` rỗng — tránh ghi đè chỉnh sửa của admin sau deploy.
   - Live update: thay đổi permission được áp dụng cho user đang đăng nhập sau khi access token hết hạn (60p) hoặc refresh — `RefreshTokenService.RotateAsync` re-load `RolePermissions` từ DB.
   ```

## Verification

```powershell
# Frontend full check
cd frontend
npm run lint
npx tsc -b
npm test -- --run route-permissions roles-matrix-page

# Backend giữ nguyên — confirm không regress
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj
```

Manual smoke (cần dev server frontend + backend chạy):

1. Login `admin/Admin@123` → sidebar có "Phân quyền" dưới Setting.
2. Click → load matrix, ADMIN column checkbox disabled.
3. Tick `quotations.delete` cho SALES → badge "1 thay đổi chưa lưu" + button Save enable.
4. Save → toast success, reload trang vẫn còn thay đổi.
5. Tạo custom role `TEST_LEAD` với 2 permission → xuất hiện cột mới.
6. Mở `/admin/users`, tạo user role `TEST_LEAD`.
7. Login user mới (tab incognito) → permission đúng.
8. Logout, login admin, vào `/admin/roles` → click delete `TEST_LEAD` → expect 409 "đang được gán cho 1 user".
9. Đổi user về SALES, quay lại xoá `TEST_LEAD` → OK.
10. Login user SALES → menu KHÔNG có "Phân quyền"; force URL `/admin/roles` → redirect/403.

## Exit Criteria

- [ ] `npm run lint`, `tsc -b` clean.
- [ ] `route-permissions.test.ts` pass (kể cả 2 case mới).
- [ ] Backend test suite vẫn xanh.
- [ ] Manual smoke 10 bước trên pass hết.
- [ ] Doc architecture có section "Role × Permission management".
