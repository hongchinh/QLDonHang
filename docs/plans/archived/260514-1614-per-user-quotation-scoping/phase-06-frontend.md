# Phase 06 — Frontend pages + integration

**Status:** [x] complete
**Complexity:** L

## Objective

Triển khai UI cho 3 nhóm chức năng. Tích hợp với 9 endpoint backend đã có. Dashboard nối số liệu thật, scope theo permission backend trả.

## Files

### Feature modules (TanStack Query pattern)

- `frontend/src/features/me-settings/types.ts` (new)
- `frontend/src/features/me-settings/api.ts` (new)
- `frontend/src/features/me-settings/keys.ts` (new)
- `frontend/src/features/me-settings/hooks.ts` (new)
- `frontend/src/features/admin-user-settings/types.ts` (new)
- `frontend/src/features/admin-user-settings/api.ts` (new)
- `frontend/src/features/admin-user-settings/keys.ts` (new)
- `frontend/src/features/admin-user-settings/hooks.ts` (new)
- `frontend/src/features/dashboard/types.ts` (new)
- `frontend/src/features/dashboard/api.ts` (new)
- `frontend/src/features/dashboard/hooks.ts` (new)
- `frontend/src/features/quotations/types.ts` (modify — thêm `ownerUserId`, `ownerFullName`, `canEdit`, `canClone`)
- `frontend/src/features/quotations/api.ts` (modify — thêm `transferOwner`, `clone`)
- `frontend/src/features/quotations/hooks.ts` (modify — useTransferOwner, useClone)

### Pages

- `frontend/src/pages/settings/my-quotation-settings-page.tsx` (new)
- `frontend/src/pages/admin/user-settings-page.tsx` (new — list + per-user form)
- `frontend/src/pages/admin/bulk-transfer-page.tsx` (new — Admin: chọn From/To user, preview, confirm)
- `frontend/src/pages/dashboard-page.tsx` (modify — nối stats)
- `frontend/src/pages/quotations/quotation-list-page.tsx` (modify — cột Owner, badge Orphan, nút Transfer, nút Clone)
- `frontend/src/pages/quotations/quotation-form-page.tsx` (modify — disable inputs khi `canEdit=false`, banner lý do)
- `frontend/src/pages/quotations/components/transfer-owner-dialog.tsx` (new)

### Routing

- `frontend/src/App.tsx` (modify — route mới: `/settings/my-quotation-settings`, `/admin/user-settings`, `/admin/users/:userId/transfer-quotations`)
- `frontend/src/components/layout/app-sidebar.tsx` (verify or modify — menu item)

## Tasks

### Feature modules

1. `features/me-settings`:
   ```ts
   // types.ts
   export interface MyQuotationSettings {
     userId: string;
     userFullName?: string;
     lockAtStatus: 'Sent' | 'Confirmed' | 'ConvertedToOrder' | null;
     templateFileName: string | null;
     templateOriginalName: string | null;
     templateUploadedAt: string | null;
   }
   ```
   - `api.ts`: `getMine()`, `uploadTemplate(file: File)` (multipart), `deleteTemplate()`, `downloadTemplateUrl()` (return URL for `<a href>`).
   - `keys.ts`: `meSettingsKey = ['me', 'quotation-settings']`.
   - `hooks.ts`: `useMySettings()`, `useUploadTemplate()`, `useDeleteTemplate()`.
2. `features/admin-user-settings`:
   - `api.ts`: `getForUser(userId)`, `setLockAt(userId, { lockAtStatus })`, `bulkTransfer(fromUserId, { toUserId, includeCancelled, reason })`.
   - `keys.ts`: `userSettingsKey(userId)`.
   - `hooks.ts`: `useUserSettings(userId)`, `useSetLockAt(userId)`, `useBulkTransfer(fromUserId)`.
3. `features/dashboard`:
   - `types.ts`: `QuotationStats { totalCount; draftCount; sentCount; confirmedCount; convertedCount; cancelledCount; totalRevenue; todayRevenue; from; to }`.
   - `api.ts`: `getQuotationStats(from?, to?)`.
   - `hooks.ts`: `useQuotationStats(from?, to?)`.

### Quotation feature update

4. `features/quotations/types.ts` thêm vào `Quotation` và `QuotationListItem`:
   ```ts
   ownerUserId: string;
   ownerFullName?: string;
   isOwnerDeleted: boolean;
   canEdit: boolean;       // (chỉ Quotation, không trên ListItem)
   canClone: boolean;      // (cả Quotation và QuotationListItem để list hiển thị action)
   ```
5. `api.ts` thêm:
   ```ts
   transferOwner(id, { newOwnerUserId, reason }): Promise<Quotation>;
   clone(id): Promise<Quotation>;
   ```
6. `hooks.ts` thêm `useTransferOwner()`, `useClone()` — invalidate keys `['quotations']` after mutate.

### Pages

7. `pages/settings/my-quotation-settings-page.tsx`:
   - Layout 2 section: "Template báo giá" + "Khoá theo trạng thái" (read-only, hiển thị giá trị admin đã cấu hình).
   - Section template:
     - Khi `templateFileName == null`: banner "Đang dùng template mặc định" + nút "Tải lên".
     - Khi có: hiển thị `templateOriginalName` + `templateUploadedAt`; nút "Tải về" (`<a href={downloadUrl}>`), "Tải lên thay thế", "Xoá".
     - Upload dialog: `<input type="file" accept=".xlsx">`; client-side check size 5MB; gọi `useUploadTemplate`.
     - Toast error/success.
8. `pages/admin/user-settings-page.tsx`:
   - Route param `:userId` → load `useUserSettings(userId)`.
   - Form: `<select>` cho LockAtStatus với 4 option: "Không khoá" (null) / "Từ Đã gửi" / "Từ Đã xác nhận" / "Từ Đã chuyển đơn hàng".
   - Nút "Lưu" → `useSetLockAt`.
   - Đường dẫn từ trang Users list (nếu trang Users đã có — nếu không, **mở rộng tối thiểu**: thêm column "Cấu hình" với link `/admin/user-settings/{id}`). *Nếu trang Users chưa tồn tại*: skip nav, để route truy cập trực tiếp; ghi chú vào exit criteria.
9. `pages/admin/bulk-transfer-page.tsx`:
   - Route `/admin/users/:userId/transfer-quotations`.
   - Form: user nhận (select từ list users), checkbox "Bao gồm Cancelled", textarea Reason.
   - Confirm dialog: hiển thị "Sẽ chuyển X báo giá" (gọi 1 endpoint preview hoặc bỏ qua preview, chỉ confirm by text).
   - Submit → `useBulkTransfer` → toast với AffectedCount.
10. `pages/dashboard-page.tsx`:
    - Replace placeholder cards with real values từ `useQuotationStats()`.
    - Title "Tổng quan của tôi" (sales) hoặc "Tổng quan hệ thống" (admin/manager) — phân biệt theo `useAuth().hasPermission('quotations.view_all')`.
11. `pages/quotations/quotation-list-page.tsx`:
    - Thêm cột "Chủ sở hữu" (chỉ hiển thị khi user có `quotations.view_all`); fallback "—" nếu null.
    - Badge "Owner đã nghỉ" cạnh tên owner khi `isOwnerDeleted=true`. BE trả `ownerFullName` bằng lookup `IgnoreQueryFilters()` để vẫn hiển thị tên owner đã soft-delete; FE không tự suy luận từ `ownerFullName == null`.
    - Hàng action: thêm menu "Chuyển nhượng" (visible khi user là owner OR có `transfer_any`), "Clone" (visible khi `canClone`).
12. `pages/quotations/quotation-form-page.tsx`:
    - Khi `canEdit === false`: disable mọi `<input>`, `<select>`, `<button type="submit">`. Hiện banner màu warning ở đầu form với 1 trong các message tuỳ lý do (suy từ status/owner/lock):
      - "Báo giá đã chuyển đơn hàng — không thể chỉnh sửa."
      - "Báo giá đã huỷ — không thể chỉnh sửa."
      - "Chủ sở hữu báo giá đã ngừng hoạt động — vui lòng clone để tiếp tục."
      - "Báo giá đã ở trạng thái '{status}' — cấu hình khoá của bạn không cho phép sửa."
    - Nút "Clone" hiện khi `canClone=true`; gọi `useClone` → navigate sang form Q mới.
13. `pages/quotations/components/transfer-owner-dialog.tsx`:
    - User picker (search-as-you-type) + Reason textarea.
    - Submit → `useTransferOwner`.

### Routing & nav

14. `App.tsx` bổ sung routes (dưới `<Route element={AppLayout}>`):
    ```tsx
    <Route path="settings/my-quotation-settings" element={<MyQuotationSettingsPage />} />
    <Route
      path="admin/user-settings/:userId"
      element={<ProtectedRoute permission="user_settings.manage"><UserSettingsPage /></ProtectedRoute>}
    />
    <Route
      path="admin/users/:userId/transfer-quotations"
      element={<ProtectedRoute permission="quotations.transfer_any"><BulkTransferPage /></ProtectedRoute>}
    />
    ```
15. Thêm menu item trong sidebar (nếu sidebar có data-driven): "Cài đặt của tôi" → `/settings/my-quotation-settings`. Admin menu: link đến user settings (nếu chưa có Users page, để admin truy cập qua URL trực tiếp).

## Verification

```powershell
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Manual smoke (browser, 3 accounts):

1. SALES1 đăng nhập → trang Báo giá chỉ thấy báo giá của mình.
2. SALES1 → "Cài đặt báo giá của tôi" → upload `.xlsx` hợp lệ → toast success → click "Tải về" → file download đúng.
3. SALES1 thử upload file `.txt` rename `.xlsx` → toast error 400.
4. SALES1 form báo giá Sent (sau khi ADMIN set lock=Sent) → form disable, banner đúng.
5. ADMIN → User Settings của SALES1 → đặt lock-at = Confirmed → SALES1 reload form Q Sent → giờ sửa được.
6. ADMIN → Báo giá list → cột "Chủ sở hữu" hiển thị → chọn báo giá SALES1 → "Chuyển nhượng" → chuyển SALES2 → toast → list update.
7. ADMIN → Bulk transfer SALES1 → SALES2 → toast hiển thị "Đã chuyển N báo giá".
8. ADMIN soft-delete SALES1 (qua endpoint Users, nếu có) → list báo giá hiển thị "(đã nghỉ)" → form Q của SALES1 không sửa được + nút Clone hiện.
9. Dashboard SALES1 cards có số; Dashboard ADMIN cards lớn hơn (toàn bộ).

## Exit Criteria

- 3 trang mới render đúng; 2 trang sửa (list/form) hoạt động.
- Tất cả endpoint phase 02-05 được call từ FE.
- Type FE đồng bộ với DTO BE (build typecheck pass).
- 9 case smoke manual pass.
- `npm run build` không lỗi.
