# Phase 02 — Frontend feature module + Users list page

**Status:** [x] complete
**Complexity:** M

## Objective

Tạo feature module `features/admin-users/`, trang `/admin/users` (list user với search + per-row action vào per-user-settings hoặc bulk-transfer), và dùng cùng data source để thay ô nhập GUID thô trong `BulkTransferPage` bằng picker user nhận. Tuân theo pattern feature/page hiện có (`features/customers/`, `pages/customers/customer-list-page.tsx`).

## Files

**New**
- `frontend/src/features/admin-users/types.ts`
- `frontend/src/features/admin-users/api.ts`
- `frontend/src/features/admin-users/keys.ts`
- `frontend/src/features/admin-users/hooks.ts`
- `frontend/src/pages/admin/users-list-page.tsx`

**Modified**
- `frontend/src/pages/admin/bulk-transfer-page.tsx`

## Tasks

### Feature module

1. `types.ts`:
   ```ts
   export interface AdminUserListItem {
     id: string;
     username: string;
     fullName: string;
     roleCode: string | null;
     isActive: boolean;
     lastLoginAt: string | null;
   }

   export interface AdminUserListParams {
     search?: string;
     activeOnly?: boolean;
   }
   ```

2. `api.ts`:
   ```ts
   import { apiGet } from '@/lib/api-client';
   import type { AdminUserListItem, AdminUserListParams } from './types';

   export const adminUsersApi = {
     list: (params: AdminUserListParams) =>
       apiGet<AdminUserListItem[]>('/admin/users', params),
   };
   ```
   *Xác minh* `apiGet` ký hiệu params: kiểm tra ở `features/customers/api.ts` → đã truyền object params trực tiếp; pattern OK.

3. `keys.ts`:
   ```ts
   import type { AdminUserListParams } from './types';

   export const adminUsersKeys = {
     all: ['admin', 'users'] as const,
     lists: () => [...adminUsersKeys.all, 'list'] as const,
     list: (params: AdminUserListParams) => [...adminUsersKeys.lists(), params] as const,
   };
   ```

4. `hooks.ts`:
   ```ts
   import { keepPreviousData, useQuery } from '@tanstack/react-query';
   import { adminUsersApi } from './api';
   import { adminUsersKeys } from './keys';
   import type { AdminUserListParams } from './types';

   export function useAdminUsers(params: AdminUserListParams) {
     return useQuery({
       queryKey: adminUsersKeys.list(params),
       queryFn: () => adminUsersApi.list(params),
       placeholderData: keepPreviousData,
       staleTime: 30_000,
     });
   }
   ```

### Page `users-list-page.tsx`

5. Tạo `pages/admin/users-list-page.tsx` theo pattern `customer-list-page.tsx`:
   - Import:
     ```ts
     import { useMemo, useState } from 'react';
     import { Link } from 'react-router-dom';
     import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from '@tanstack/react-table';
     import { Search, Settings2, ArrowRightLeft } from 'lucide-react';
     import { useAdminUsers } from '@/features/admin-users/hooks';
     import type { AdminUserListItem } from '@/features/admin-users/types';
     import { useSearchParamString } from '@/lib/use-search-param-state';
     import { useDebouncedValue } from '@/lib/use-debounced-value';
     import { Card, CardContent } from '@/components/ui/card';
     import { Input } from '@/components/ui/input';
     import { Button } from '@/components/ui/button';
     import { Badge } from '@/components/ui/badge';
     import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
     import { Can } from '@/components/auth/can';
     import { getErrorMessage } from '@/lib/api-client';
     ```

   - State:
     ```ts
     const [search, setSearch] = useSearchParamString('q');
     const [activeOnly, setActiveOnly] = useState(false);
     const debouncedSearch = useDebouncedValue(search, 300);
     const { data, isLoading, isError, error } = useAdminUsers({
       search: debouncedSearch || undefined,
       activeOnly,
     });
     ```

   - Columns (5 cột):
     1. `username` — Username (mono font nếu có style sẵn; nếu không thì plain).
     2. `fullName` — Họ tên.
     3. `roleCode` — Role: `<Badge variant="outline">{roleCode}</Badge>` hoặc `—` khi null.
     4. `isActive` — Trạng thái: `<Badge variant="success">Đang dùng</Badge>` vs `<Badge variant="secondary">Đã nghỉ</Badge>`.
     5. `actions` — 2 nút icon-only:
        - "Cấu hình báo giá" → `<Link to={"/admin/user-settings/" + row.original.id}><Settings2 /></Link>` (luôn hiện).
        - "Chuyển nhượng báo giá" → `<Can permission="quotations.transfer_any"><Link to={"/admin/users/" + row.original.id + "/transfer-quotations"}><ArrowRightLeft /></Link></Can>`.
        Dùng `<Button asChild variant="ghost" size="icon" aria-label="...">`.

   - Header (filter UI):
     - Search input (giống customer page).
     - Checkbox/toggle "Chỉ user đang hoạt động" → set `activeOnly`. Đặt cạnh search box.

   - Empty/loading state: copy y nguyên pattern `customer-list-page` (rows colspan, message "Đang tải..." / "Không có user nào.").

   - **Không** dùng pagination (BE không paginate — ~30 user).

6. Sửa lại nếu cần: route param naming. Page sẽ navigate qua `<Link to="/admin/user-settings/${id}">` và `<Link to="/admin/users/${id}/transfer-quotations">` — match route hiện tại trong `App.tsx`.

7. **Không** sửa route hay sidebar trong phase này (để phase 04 lo). Page `UsersListPage` mới chỉ được typecheck/lint trong phase này; browser smoke cho `/admin/users` thực hiện sau khi Phase 04 wire route.

### Bulk-transfer user picker

8. Sửa `frontend/src/pages/admin/bulk-transfer-page.tsx` để không bắt admin nhập raw GUID:
   - Import `useMemo` nếu cần, `useAdminUsers`, `AdminUserListItem`, và component select/label hiện có trong repo (nếu chưa có `Select`, dùng `<select>` native giống `user-settings-page.tsx`).
   - Gọi `useAdminUsers({ activeOnly: true })` để lấy danh sách user nhận còn active.
   - Exclude chính `fromUserId` (`userId` route param) khỏi danh sách user nhận.
   - Render dropdown hiển thị `fullName` + `username` + `roleCode`; value là `id`.
   - Giữ state `toUserId` và payload `bulk.mutateAsync({ toUserId, includeCancelled, reason: ... })` như hiện tại để không đổi API.
   - Loading/error state nhỏ trong form: disable submit khi đang tải danh sách user hoặc chưa chọn `toUserId`; nếu load lỗi thì hiển thị message từ `getErrorMessage(error)`.
   - Không đổi semantics source user: source có thể là soft-deleted/inactive vì vào trang từ `/admin/users/:userId/transfer-quotations`.

## Verification

```powershell
# FE typecheck + lint chỉ
npm --prefix frontend run typecheck
npm --prefix frontend run lint
```

Manual (yêu cầu BE Phase 01 đã chạy):
1. Start FE dev server (nếu chưa chạy): `npm --prefix frontend run dev`.
2. Đăng nhập ADMIN, gõ tay route bulk-transfer hiện có `http://localhost:<port>/admin/users/<source-user-id>/transfer-quotations`.
3. Quan sát:
   - Ở bulk-transfer page, trường "User ID nhận" đã thành dropdown user active; không cho chọn chính user nguồn.
   - Chọn user nhận từ dropdown, submit vẫn gọi API bulk-transfer với `toUserId` đúng.
   - Browser smoke cho list `/admin/users` (search/filter/action) để sang Phase 04 sau khi route được wire.

## Exit Criteria

- 4 file feature module + 1 file page tạo xong; `bulk-transfer-page.tsx` dùng picker user nhận thay cho raw GUID.
- Typecheck + lint pass.
- Manual smoke bulk-transfer picker hoạt động; list user search/filter/action sẽ được smoke ở Phase 04/05 sau khi route `/admin/users` được wire.
- Không có console error trong DevTools.
- Đảm bảo permission `user_settings.manage` (cấp ở backend) đủ để API trả 200 — admin role mặc định có permission này.
