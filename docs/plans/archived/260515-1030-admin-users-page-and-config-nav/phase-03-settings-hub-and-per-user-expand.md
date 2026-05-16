# Phase 03 — Settings hub + per-user page expansion

**Status:** [x] complete
**Complexity:** S

## Objective

Tạo trang `SettingsHubPage` mới (file page trước, routing thực hiện ở Phase 04) và mở rộng `user-settings-page.tsx` thêm 2 section: template info read-only + shortcut sang bulk-transfer.

## Files

**New**
- `frontend/src/pages/settings/settings-hub-page.tsx`

**Modified**
- `frontend/src/pages/admin/user-settings-page.tsx`

## Tasks

### `settings-hub-page.tsx` (new)

1. Tạo file với layout đơn giản:
   ```tsx
   import { Link } from 'react-router-dom';
   import { UserCog, Users2 } from 'lucide-react';
   import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
   import { useAuthStore } from '@/stores/auth-store';

   export function SettingsHubPage() {
     const hasPermission = useAuthStore((s) => s.hasPermission);
     return (
       <div className="space-y-6">
         <div>
           <h1 className="text-2xl font-bold">Cấu hình</h1>
           <p className="text-sm text-muted-foreground">
             Điểm vào cho các cài đặt cá nhân và quản trị.
           </p>
         </div>

         <div className="grid gap-4 md:grid-cols-2">
           <Link to="/settings/my-quotation-settings" className="block">
             <Card className="h-full transition hover:border-primary">
               <CardHeader>
                 <div className="flex items-center gap-2">
                   <UserCog className="h-5 w-5" />
                   <CardTitle>Cài đặt báo giá của tôi</CardTitle>
                 </div>
                 <CardDescription>
                   Upload template Excel cá nhân, xem cấu hình khoá trạng thái.
                 </CardDescription>
               </CardHeader>
             </Card>
           </Link>

           {hasPermission('user_settings.manage') && (
             <Link to="/admin/users" className="block">
               <Card className="h-full transition hover:border-primary">
                 <CardHeader>
                   <div className="flex items-center gap-2">
                     <Users2 className="h-5 w-5" />
                     <CardTitle>Quản lý người dùng</CardTitle>
                   </div>
                   <CardDescription>
                     Cấu hình lock-at theo từng user và chuyển nhượng báo giá hàng loạt.
                   </CardDescription>
                 </CardHeader>
               </Card>
             </Link>
           )}
         </div>
       </div>
     );
   }
   ```

2. *Không* dùng `ProtectedRoute` cho route; landing tự lọc card. User không có permission `user_settings.manage` chỉ thấy 1 card.

### `user-settings-page.tsx` (modify)

3. Mở `frontend/src/pages/admin/user-settings-page.tsx`. Current state: chỉ có form lock-at.

4. Thêm imports cần thiết (nếu chưa có):
   ```ts
   import { useNavigate } from 'react-router-dom';
   import { Can } from '@/components/auth/can';
   import { ArrowRightLeft } from 'lucide-react';
   ```

5. Trong component, sau `useSetLockAt`, thêm:
   ```ts
   const navigate = useNavigate();
   ```

6. Trong JSX, sau `<Card>` chứa "Khoá theo trạng thái" (kết thúc ở dòng `</Card>` cuối hiện tại), bổ sung 2 block:

   **6a. Card "Template báo giá của user" — read-only**
   ```tsx
   <Card>
     <CardHeader>
       <CardTitle>Template báo giá của user</CardTitle>
       <CardDescription>Thông tin template do user tự upload (chỉ xem).</CardDescription>
     </CardHeader>
     <CardContent>
       {settings.templateFileName ? (
         <div className="space-y-1 text-sm">
           <p>
             <strong>{settings.templateOriginalName ?? settings.templateFileName}</strong>
           </p>
           {settings.templateUploadedAt && (
             <p className="text-muted-foreground">
               Cập nhật {new Date(settings.templateUploadedAt).toLocaleString('vi-VN')}
             </p>
           )}
         </div>
       ) : (
         <p className="text-sm text-muted-foreground">
           User đang dùng template mặc định của hệ thống.
         </p>
       )}
     </CardContent>
   </Card>
   ```
   *Note*: `settings` type là `UserSettings` (alias của `MyQuotationSettings`) — verified ở `features/admin-user-settings/types.ts:4`. Đã có các field cần thiết.

   **6b. Card "Hành động khác" — shortcut bulk-transfer**
   ```tsx
   <Can permission="quotations.transfer_any">
     <Card>
       <CardHeader>
         <CardTitle>Chuyển nhượng báo giá</CardTitle>
         <CardDescription>
           Chuyển toàn bộ báo giá thuộc user này sang user khác (dùng khi user nghỉ việc).
         </CardDescription>
       </CardHeader>
       <CardContent>
         <Button
           variant="outline"
           onClick={() => navigate(`/admin/users/${userId}/transfer-quotations`)}
         >
           <ArrowRightLeft className="mr-2 h-4 w-4" />
           Mở trang chuyển nhượng
         </Button>
       </CardContent>
     </Card>
   </Can>
   ```

7. Verify import `Can` từ `@/components/auth/can` — nếu chưa, kiểm tra path tồn tại (đã có tham chiếu ở `customer-list-page.tsx`).

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
```

Manual:
1. ADMIN gõ `/admin/user-settings/<sales1-id>` → 3 card hiện ra: lock-at form, template info read-only, hành động chuyển nhượng.
2. Click "Mở trang chuyển nhượng" → navigate đúng `/admin/users/<sales1-id>/transfer-quotations`.
3. Route `/settings` vẫn là placeholder ở phase này; chỉ verify hub sau khi Phase 04 wire route.

## Exit Criteria

- File `settings-hub-page.tsx` tạo xong.
- File `user-settings-page.tsx` có thêm 2 card.
- Typecheck + lint pass.
- Manual 3 case ở trên đều đúng.
- Route `/settings` chưa đổi (sẽ làm ở phase 04) — hiện tại vẫn placeholder; verify gõ `/settings` ra placeholder cũ, gõ `/settings/my-quotation-settings` vẫn ra page user — page hub mới chỉ tồn tại file nhưng chưa route.
