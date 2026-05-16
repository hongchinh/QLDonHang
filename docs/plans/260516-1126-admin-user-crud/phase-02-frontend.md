# Phase 02 — Frontend dialogs + actions

**Status:** [ ] pending
**Complexity:** M

## Objective
Mở rộng `features/admin-users` với mutation hooks + types mới; thêm 2 dialog (create/edit, reset-password) và dropdown action menu (toggle status, delete) lên `users-list-page.tsx`. Tất cả gate qua `<Can>` theo permission `users.{create,update,delete}`.

## Files

### New (shadcn primitives)
- `frontend/src/components/ui/alert-dialog.tsx` — copy chuẩn shadcn (Radix `@radix-ui/react-alert-dialog` đã có trong `package.json`? — kiểm tra; nếu chưa, `npm install @radix-ui/react-alert-dialog`).
- `frontend/src/components/ui/dropdown-menu.tsx` — copy chuẩn shadcn (Radix `@radix-ui/react-dropdown-menu` — kiểm tra; cài nếu thiếu).

### New (feature components)
- `frontend/src/pages/admin/components/user-form-dialog.tsx` — dùng cho cả Create + Edit (props `mode: 'create' | 'edit'`, `userId?: string`).
- `frontend/src/pages/admin/components/reset-password-dialog.tsx`.
- `frontend/src/pages/admin/components/user-actions-menu.tsx` — dropdown gói các action mỗi row (Edit, Reset password, Toggle status, Delete) + state mở dialog tương ứng.

### Modified
- `frontend/src/features/admin-users/types.ts` — thêm 5 type.
- `frontend/src/features/admin-users/api.ts` — thêm 6 hàm.
- `frontend/src/features/admin-users/hooks.ts` — thêm 6 hook.
- `frontend/src/features/admin-users/keys.ts` — thêm `detail(id)`.
- `frontend/src/pages/admin/users-list-page.tsx` — header thêm nút "Thêm user", thay 2 icon hiện tại bằng `UserActionsMenu` (giữ icon "Cấu hình báo giá" + "Chuyển nhượng" + thêm action mới).

## Tasks

### Types (`features/admin-users/types.ts`)

1. Bổ sung:
   ```ts
   export type UserStatus = 'Active' | 'Disabled';

   export interface AdminUserDetail {
     id: string;
     username: string;
     email: string;
     fullName: string;
     phoneNumber: string | null;
     roleCode: string | null;
     status: UserStatus;
     isDeleted: boolean;
     lastLoginAt: string | null;
     createdAt: string;
     updatedAt: string | null;
   }

   export interface CreateUserPayload {
     username: string;
     email: string;
     fullName: string;
     phoneNumber?: string | null;
     roleCode: string;
     password: string;
     status: UserStatus;
   }

   export interface UpdateUserPayload {
     fullName: string;
     email: string;
     phoneNumber?: string | null;
     roleCode: string;
     status: UserStatus;
   }

   export interface ResetPasswordPayload { newPassword: string; }
   export interface SetUserStatusPayload { status: UserStatus; }
   ```
   - **Backend gửi enum dạng số** (`UserStatus.Active = 1`, `Disabled = 0`) — kiểm tra `JsonStringEnumConverter` trong `Program.cs`/`Startup`. Nếu repo dùng string enum (như Quotation) thì giữ string; nếu số thì đổi `UserStatus = 0 | 1`. Khớp với types đang dùng ở `quotations/types.ts`.

### API client (`features/admin-users/api.ts`)

2. Bổ sung:
   ```ts
   import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api-client';
   // ...

   getDetail: (id: string) => apiGet<AdminUserDetail>(`/admin/users/${id}`),
   create: (payload: CreateUserPayload) => apiPost<AdminUserDetail>('/admin/users', payload),
   update: (id: string, payload: UpdateUserPayload) => apiPut<AdminUserDetail>(`/admin/users/${id}`, payload),
   resetPassword: (id: string, payload: ResetPasswordPayload) =>
     apiPost<void>(`/admin/users/${id}/reset-password`, payload),
   setStatus: (id: string, payload: SetUserStatusPayload) =>
     apiPost<void>(`/admin/users/${id}/status`, payload),
   remove: (id: string) => apiDelete<void>(`/admin/users/${id}`),
   ```
   - Verify tên helper `apiPost/apiPut/apiDelete` chính xác trong `lib/api-client.ts` trước khi import.

### Keys (`features/admin-users/keys.ts`)

3. Thêm `detail: (id: string) => [...adminUsersKeys.all, 'detail', id] as const` (nếu chưa có).

### Hooks (`features/admin-users/hooks.ts`)

4. Bổ sung:
   ```ts
   export function useAdminUserDetail(id: string | undefined) {
     return useQuery({
       queryKey: adminUsersKeys.detail(id ?? ''),
       queryFn: () => adminUsersApi.getDetail(id!),
       enabled: !!id,
     });
   }
   ```

5. 5 mutation hook (`useCreateAdminUser`, `useUpdateAdminUser`, `useResetAdminUserPassword`, `useSetAdminUserStatus`, `useDeleteAdminUser`):
   - Mỗi hook dùng `useMutation` + `queryClient.invalidateQueries({ queryKey: adminUsersKeys.lists() })` (hoặc `all` nếu repo convention vậy).
   - `useUpdateAdminUser` cũng invalidate `detail(id)`.

### Shadcn primitives

6. Kiểm tra `frontend/package.json` xem đã có `@radix-ui/react-alert-dialog` và `@radix-ui/react-dropdown-menu` chưa. Nếu thiếu:
   ```powershell
   cd frontend; npm install @radix-ui/react-alert-dialog @radix-ui/react-dropdown-menu
   ```
7. Tạo `frontend/src/components/ui/alert-dialog.tsx` và `dropdown-menu.tsx` theo template shadcn chuẩn (lookup từ `components/ui/dialog.tsx` để giữ style consistent — class `cn(...)`, import `cva` nếu cần).

### Dialog: `user-form-dialog.tsx`

8. Component nhận props:
   ```ts
   {
     open: boolean;
     onOpenChange: (open: boolean) => void;
     mode: 'create' | 'edit';
     userId?: string; // chỉ cần cho edit
   }
   ```

9. Khi `mode === 'edit'`: gọi `useAdminUserDetail(userId)` → fill form. Loading state hiển thị skeleton/spinner trong dialog.

10. Form fields (react-hook-form + zod hoặc theo pattern hiện tại trong `pages/quotations/quotation-form-page.tsx`):
    - Username (chỉ create, disabled nếu edit).
    - Email, FullName, PhoneNumber, RoleCode (Select từ danh sách role cố định: ADMIN/SALES/MANAGER/ACCOUNTANT/WAREHOUSE — hardcode hoặc fetch từ endpoint `/api/roles` nếu đã có).
    - Status (Select Active/Disabled).
    - Password (chỉ create).

11. Submit:
    - Create → `useCreateAdminUser.mutateAsync(payload)`. Success → toast "Tạo user thành công" → close dialog.
    - Edit → `useUpdateAdminUser.mutateAsync({ id, payload })`. Success → toast → close.
    - Error → đọc message từ `getErrorMessage(error)` (đã có trong `lib/api-client`), hiện inline alert trong dialog.

### Dialog: `reset-password-dialog.tsx`

12. Props: `{ open, onOpenChange, userId, username }`.
13. 2 input: `newPassword`, `confirmPassword` (validate khớp). Submit → `useResetAdminUserPassword.mutateAsync({ id: userId, payload: { newPassword } })`.
14. Success → toast "Đã đặt lại mật khẩu cho {username}. Người dùng sẽ bị đăng xuất khỏi mọi thiết bị." → close.

### Dropdown action menu: `user-actions-menu.tsx`

15. Component nhận `user: AdminUserListItem`. Render `DropdownMenu` với các item gate qua `<Can>`:
    - **Edit** (`users.update`) → mở `UserFormDialog mode="edit"`.
    - **Reset mật khẩu** (`users.update`) → mở `ResetPasswordDialog`.
    - **Khoá / Mở khoá** (`users.update`) — label động theo `user.isActive`. Click → confirm `AlertDialog` → `useSetAdminUserStatus.mutate({ id, payload: { status: isActive ? 'Disabled' : 'Active' } })`.
    - Separator.
    - **Cấu hình báo giá** (link `/admin/user-settings/{id}` — giữ chức năng cũ, dùng `<Can permission="user_settings.manage">`).
    - **Chuyển nhượng báo giá** (link `/admin/users/{id}/transfer-quotations`, gate `quotations.transfer_any`).
    - Separator.
    - **Xoá** (destructive style, `users.delete`) → `AlertDialog` confirm → `useDeleteAdminUser.mutate(id)`. On error 409 (còn báo giá): hiện toast với button action "Chuyển nhượng ngay" → navigate sang `/admin/users/{id}/transfer-quotations`.

### `users-list-page.tsx`

16. Thêm state `[createDialogOpen, setCreateDialogOpen] = useState(false)` ở top page.
17. Header: thêm nút "Thêm user" bên phải tiêu đề (gate `<Can permission="users.create">`); click → `setCreateDialogOpen(true)`.
18. Cột `actions`: thay block hiện tại bằng `<UserActionsMenu user={row.original} />`.
19. Render `<UserFormDialog mode="create" open={createDialogOpen} onOpenChange={setCreateDialogOpen} />` ở cuối page.

## Verification

```powershell
cd frontend
npm run typecheck
npm run lint
npm run build
```

Manual smoke (browser):
- Mở `/admin/users` → click "Thêm user" → tạo user mới → list refresh, user mới xuất hiện.
- Click `⋯` trên row → "Sửa" → form pre-fill đúng → đổi role → lưu → badge cập nhật.
- "Reset mật khẩu" → submit 2 trường khớp → toast.
- "Khoá" user X → confirm → status badge đổi.
- "Xoá" user còn báo giá → toast lỗi 409 + có button "Chuyển nhượng ngay".

## Exit Criteria

- `npm run typecheck` + `npm run lint` + `npm run build` đều xanh.
- 2 file shadcn primitive (`alert-dialog.tsx`, `dropdown-menu.tsx`) tồn tại với type chuẩn shadcn.
- 3 component mới (`user-form-dialog`, `reset-password-dialog`, `user-actions-menu`) hoạt động khi smoke test.
- `users-list-page.tsx` không còn 2 button icon cũ — đã merge vào dropdown.
- Tất cả mutation đã invalidate đúng query key, list tự refresh sau mỗi op.
- Permission gating đúng: user role SALES login không thấy "Thêm user" / dropdown items mutation.
