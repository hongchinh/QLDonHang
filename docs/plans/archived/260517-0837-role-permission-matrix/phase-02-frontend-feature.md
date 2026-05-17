# Phase 02 — Frontend feature module + matrix page + tests

**Status:** [x] complete
**Complexity:** L

## Objective

Tạo feature module `admin-roles` (api/hooks/types/keys), matrix page `roles-matrix-page.tsx` với 3 dialog (create/rename/delete), state dirty diff, save bằng `Promise.allSettled` per-role. Bổ sung permission constants. Phase này CHƯA wire route hoặc sidebar — chỉ build và unit-test components.

**UI primitives**: codebase chưa có shadcn `<Checkbox>`, `<Tooltip>`, `<AlertDialog>` và package.json chưa có radix tương ứng. Để tránh phình dep, phase này dùng:
- **Checkbox**: native `<input type="checkbox">` style bằng Tailwind (`className="h-4 w-4 rounded border-input accent-primary disabled:cursor-not-allowed disabled:opacity-50"`).
- **Tooltip cho ADMIN column**: thay tooltip bằng `<caption>` / sub-text dưới header role (`<span className="text-xs text-muted-foreground">Luôn full quyền</span>`); cell vẫn dùng `title="ADMIN luôn có toàn bộ quyền"` attr cho hover hint.
- **AlertDialog cho destructive**: tái dùng [ConfirmDialog](../../../frontend/src/components/ui/confirm-dialog.tsx) (đã có `destructive` + `loading` props). Self-lockout warning cũng dùng `ConfirmDialog`.

## Files

### New (feature module — pattern theo `frontend/src/features/admin-users/`)

- `frontend/src/features/admin-roles/types.ts`
- `frontend/src/features/admin-roles/keys.ts`
- `frontend/src/features/admin-roles/api.ts`
- `frontend/src/features/admin-roles/hooks.ts`

### New (page + components)

- `frontend/src/pages/admin/roles-matrix-page.tsx`
- `frontend/src/pages/admin/components/role-matrix-table.tsx`
- `frontend/src/pages/admin/components/role-create-dialog.tsx`
- `frontend/src/pages/admin/components/role-rename-dialog.tsx`
- `frontend/src/pages/admin/components/role-delete-confirm.tsx`

### Modify

- `frontend/src/lib/permissions.ts` — thêm `'roles.view'` và `'roles.manage'` vào const `PERMISSIONS` array.

### New (tests)

- `frontend/src/pages/admin/roles-matrix-page.test.tsx`

## Tasks

### A. Permission constants

1. Sửa `frontend/src/lib/permissions.ts`: thêm 2 entries vào array `PERMISSIONS` const (sau `'users.delete'` để gom nhóm system):
   ```ts
   'roles.view',
   'roles.manage',
   ```

### B. Feature module

2. `types.ts`:
   ```ts
   export interface PermissionDto {
     code: string;
     name: string;
     module: 'system' | 'catalog' | 'sales' | 'report';
     description?: string | null;
   }
   export interface RoleListItem {
     id: string;
     code: string;
     name: string;
     description?: string | null;
     isSystem: boolean;
     permissionCount: number;
     userCount: number;
   }
   export interface RoleDetail {
     id: string;
     code: string;
     name: string;
     description?: string | null;
     isSystem: boolean;
     permissionCodes: string[];
     userCount: number;
     createdAt: string;
     updatedAt?: string | null;
   }
   export interface CreateRolePayload {
     code: string;
     name: string;
     description?: string | null;
     permissionCodes: string[];
   }
   export interface UpdateRolePayload {
     name: string;
     description?: string | null;
   }
   export interface UpdateRolePermissionsPayload {
     permissionCodes: string[];
   }
   ```

3. `keys.ts` (theo pattern `admin-users/keys.ts`):
   ```ts
   export const adminRolesKeys = {
     all: ['admin-roles'] as const,
     lists: () => [...adminRolesKeys.all, 'list'] as const,
     detail: (id: string) => [...adminRolesKeys.all, 'detail', id] as const,
     permissionsCatalog: ['admin-permissions-catalog'] as const,
   };
   ```

4. `api.ts`:
   ```ts
   export const adminRolesApi = {
     listPermissions: () => apiGet<PermissionDto[]>('/admin/permissions'),
     list: () => apiGet<RoleListItem[]>('/admin/roles'),
     getDetail: (id: string) => apiGet<RoleDetail>(`/admin/roles/${id}`),
     create: (payload: CreateRolePayload) => apiPost<RoleDetail>('/admin/roles', payload),
     update: (id: string, payload: UpdateRolePayload) => apiPut<RoleDetail>(`/admin/roles/${id}`, payload),
     updatePermissions: (id: string, payload: UpdateRolePermissionsPayload) =>
       apiPut<RoleDetail>(`/admin/roles/${id}/permissions`, payload),
     remove: (id: string) => apiDelete<void>(`/admin/roles/${id}`),
   };
   ```

5. `hooks.ts`: `usePermissionsCatalog`, `useAdminRoles`, `useAdminRoleDetail`, `useCreateAdminRole`, `useUpdateAdminRole`, `useUpdateAdminRolePermissions`, `useDeleteAdminRole`. Invalidate `lists()` + `detail(id)` sau mutation. `staleTime: 60_000` cho list/permissions catalog (rất ít đổi).

### C. Matrix page

6. `roles-matrix-page.tsx` — fetch song song `useAdminRoles()` + `usePermissionsCatalog()` + một `useAdminRoleDetail` cho từng role qua hook composite. Đơn giản hơn: thêm endpoint trên backend đã trả `permissionCodes` trong list — nhưng list dto hiện chỉ có count. → Approach: dùng `useQueries` từ react-query để fetch detail từng role song song.
   - Actually đơn giản hơn nữa: thêm 1 endpoint phụ trợ không cần — render bằng cách composite từ list (id+code+name+isSystem+counts) + parallel `getDetail` cho từng role để lấy `permissionCodes`. State derive: `Map<roleId, Set<permissionCode>>`.
   - Layout:
     - Header: `<h1>Phân quyền</h1>` + nút "Thêm role" (gate `<Can permission="roles.manage">`).
     - Khi `dirty.size > 0`: hiện badge `"N thay đổi chưa lưu"` + button "Lưu thay đổi" (primary) + "Huỷ".
     - Body: render `<RoleMatrixTable />` truyền props: `roles`, `permissions`, `state`, `onToggle`, `onRequestRename`, `onRequestDelete`.

7. `role-matrix-table.tsx`:
   - Table sticky-header + sticky-first-column (`<div className="overflow-auto max-h-[calc(100vh-220px)]">`).
   - First column: permission rows, group theo module với row header (`<tr class="bg-muted"><td colSpan>...</td></tr>`).
   - Mỗi module group: hiển thị tên Việt: `system→"Hệ thống"`, `catalog→"Danh mục"`, `sales→"Bán hàng"`, `report→"Báo cáo"`.
   - Header cells (cột): code role + `<span title={role.name}>` cho hover; sub-text dòng dưới hiển thị `name` (truncate). Menu (...) ở góc trên (chỉ custom role mới có; system role không có nút rename/delete) — dùng `<DropdownMenu>` từ shadcn (đã có sẵn).
   - Cells: **native `<input type="checkbox">`** (lý do: không có shadcn `<Checkbox>` + radix `@radix-ui/react-checkbox` chưa cài). Class chuẩn:
     ```tsx
     <input
       type="checkbox"
       className="h-4 w-4 rounded border-input accent-primary disabled:cursor-not-allowed disabled:opacity-50"
       checked={...}
       disabled={...}
       onChange={...}
     />
     ```
     - ADMIN column: `checked={true}` `disabled={true}` + `title="ADMIN luôn có toàn bộ quyền"` attr.
     - Other: controlled bởi state map.
   - "Select-all module" row header có 1 checkbox cho mỗi cột (trừ ADMIN) — toggle áp cho mọi permission của module trong cột đó. Indeterminate state set qua `ref` callback: `el => { if (el) el.indeterminate = someChecked && !allChecked; }` (native API).

8. `role-create-dialog.tsx`: Form (`react-hook-form` + zod):
   - `Code`: text, hint regex.
   - `Name`: text.
   - `Description`: textarea optional.
   - `PermissionCodes`: dùng component `<MultiSelect>` đã có ở `frontend/src/components/ui/multi-select.tsx` (group theo module).
   - Submit → `useCreateAdminRole`.

9. `role-rename-dialog.tsx`: Form `Name` + `Description`. Submit → `useUpdateAdminRole`.

10. `role-delete-confirm.tsx`: tái dùng [ConfirmDialog](../../../frontend/src/components/ui/confirm-dialog.tsx) với `destructive={true}`, `title="Xoá role {role.code}?"`. Nếu `userCount > 0` → disable confirm button (`loading={true}` không đúng semantic → ưu tiên: không mở ConfirmDialog mà thay bằng inline `Dialog` đơn giản hiển thị warning "N user đang dùng role này, vui lòng đổi role trước." + chỉ có nút "Đóng"). Submit → `useDeleteAdminRole`; bắt 409 hiện toast.

11. Save handler trong `roles-matrix-page.tsx`:
    - Self-lockout check: nếu current user (`useAuthStore`) đang `isInRole(role.code)` và state mới bỏ `roles.manage` khỏi role đó → mở `ConfirmDialog` với `destructive={true}`, `title="Bạn sắp bỏ quyền quản lý vai trò khỏi role của chính mình"`, `description="Sau khi access token hết hạn (~60 phút) bạn sẽ không vào lại được màn hình này. Tiếp tục?"`. `onConfirm` → tiếp tục save flow.
    - Build diff: `Object.entries(dirty).map(([roleId, codes]) => adminRolesApi.updatePermissions(roleId, { permissionCodes: [...codes] }))`.
    - `const results = await Promise.allSettled([...])`; đếm `fulfilled` / `rejected`:
      - Toast success `"Đã lưu N role"` nếu có fulfilled; toast error `"M role lỗi, vui lòng thử lại"` nếu có rejected.
      - Chỉ reset dirty state cho role đã fulfilled (giữ lại role lỗi để admin retry).
    - Invalidate `adminRolesKeys.lists()` + `adminRolesKeys.detail(roleId)` cho role fulfilled.
    - Toast info bổ sung: `"Thay đổi sẽ có hiệu lực với user đang đăng nhập sau khi access token làm mới (tối đa 60 phút)."` (chỉ hiện 1 lần per save).

### D. Tests

12. `roles-matrix-page.test.tsx` (vitest + RTL):
    - Render với MSW handlers mock 3 roles (ADMIN, SALES, CUSTOM) + 6 permissions trong 2 module.
    - Assert ADMIN column native checkbox có `disabled` và `checked` (`getByRole('checkbox', { name: /admin.*quotations.view/i })`).
    - Toggle SALES.quotations.view checkbox → button "Lưu thay đổi" enable + badge "1 thay đổi chưa lưu".
    - Click save → assert `PUT /admin/roles/{salesId}/permissions` được gọi đúng 1 lần với payload đúng, các role không thay đổi KHÔNG gọi.
    - Partial-failure case: mock 1 role 500 → assert toast error + dirty state cho role lỗi vẫn còn, role thành công đã clear.
    - Click trên ADMIN checkbox → no-op (input disabled, change handler không fire).
    - Test thêm custom role: click "Thêm role" → fill form → submit → assert `POST /admin/roles`.
    - Self-lockout: mock current user role MANAGER, toggle bỏ `roles.manage` khỏi MANAGER → assert ConfirmDialog mở; click "Hủy" → không có request; click "Xác nhận" → request được gửi.

## Verification

```powershell
cd frontend
npm run lint
npx tsc -b
npm test -- --run roles-matrix-page
```

Visual smoke (dev server `npm run dev`, login admin, truy cập route tạm via dev tool console hoặc paste URL `/admin/roles` sau khi Phase 03 wire — phase này TỰ TEST qua vitest là chính):

## Exit Criteria

- [ ] `npm run lint` clean.
- [ ] `npx tsc -b` không lỗi.
- [ ] `roles-matrix-page.test.tsx` pass tất cả case (bao gồm partial-failure + self-lockout).
- [ ] Code review: dirty state reset CHỈ cho role fulfilled (role rejected giữ lại để retry); ADMIN column hard-disabled ở UI; self-lockout `ConfirmDialog` có path execute; KHÔNG cài thêm radix package mới (`grep "@radix-ui/react-checkbox\|tooltip\|alert-dialog" package.json` phải trống).
