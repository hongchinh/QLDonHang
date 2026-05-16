# Plan — Admin Users page + Config navigation cho per-user quotation scoping

**Brainstorm gốc**: phiên brainstorm ngày 2026-05-15 (Approach A — 2 menu đứng riêng + Settings hub đơn giản).

## Goal

Khép kín UX cho 3 trang config đã có sẵn từ Phase 06 của plan `260514-1614-per-user-quotation-scoping/` bằng cách bổ sung:

1. Endpoint backend `GET /api/admin/users` list user cho users page và picker chọn user nhận trong bulk-transfer (chưa có).
2. Trang FE `/admin/users` (list + per-row action vào per-user-settings hoặc bulk-transfer) và bulk-transfer picker thay cho nhập GUID thô.
3. Trang FE `/settings` chuyển từ placeholder thành Settings Hub với card conditional theo permission.
4. Mở rộng `/admin/user-settings/:userId` (thêm template info read-only + shortcut bulk-transfer).
5. Cập nhật routing + sidebar (2 entry mới: "Cài đặt của tôi", "Quản lý người dùng").

## Scope

**In scope**
- Backend: `AdminUsersController` mới + `IAdminUserService` + `AdminUserListItemDto` + DI registration + integration test.
- Frontend: feature module `features/admin-users/`; pages mới `users-list-page.tsx` + `settings-hub-page.tsx`; sửa `user-settings-page.tsx`, `bulk-transfer-page.tsx`, `App.tsx`, `app-layout.tsx`.
- Final verification: `dotnet build` project liên quan + `dotnet test --filter`; `npm typecheck/lint/build` cho FE.

**Out of scope**
- CRUD user (create/edit/disable/reset password/assign role).
- Pagination cho users list (~30 user → client-side filter là đủ).
- Audit log cho list users.
- System-config khác trên hub (chỉ 2 card hiện tại).
- Sửa logic backend per-user-settings / bulk-transfer / template (Phase 06 cũ đã làm).

## Assumptions

- Permission `user_settings.manage` và `quotations.transfer_any` đã tồn tại ở cả BE (`Permissions.UserSettings.Manage`, `Permissions.Quotations.TransferAny`) và FE (`PERMISSIONS` array).
- `User` entity (verified): `Username`, `Email`, `FullName`, `PhoneNumber?`, `Status: UserStatus`, `LastLoginAt?`, `UserRoles → Role`. `BaseEntity` có `IsDeleted` (soft-delete).
- `UserStatus` enum: `Active = 1`, `Disabled = 0`.
- "Active user" định nghĩa: `!IsDeleted && Status == UserStatus.Active`.
- DTO `UserQuotationSettingsDto` (BE) đã có sẵn `TemplateFileName`, `TemplateOriginalName`, `TemplateUploadedAt` (verified ở `UserQuotationSettingsDto.cs`).
- FE pattern: feature module 4 file (types/api/keys/hooks), pages dùng TanStack Table + Query, `Can` component cho permission-conditional render, `ProtectedRoute` cho route guard.
- Build memory rule (memory: `feedback_build_skip_when_app_running.md`): chỉ build project changed, không full-rebuild solution.
- Không có migration DB cho plan này.

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Endpoint list users lộ thông tin không cần thiết (email, phone, password hash) | Trung | DTO chỉ trả `Id/Username/FullName/RoleCode/IsActive/LastLoginAt`. Permission guard `user_settings.manage`. |
| Performance khi DB nhiều user | Thấp | ~30 user; client-side filter; thêm pagination chỉ khi vượt ngưỡng (out of scope). |
| User soft-deleted không hiện trong list → admin không bulk-transfer được | Trung | Query dùng `IgnoreQueryFilters()`; param `activeOnly?` default `false`. UI mặc định hiện cả 2 với badge. |
| Sidebar 2 entry mới đẩy menu dài | Thấp | Chấp nhận; nếu user phản hồi có thể gom vào Hub sau. |
| Trùng route `/settings` cũ (placeholder admin-only) với hub mới | Thấp | Thay luôn route + bỏ menu sidebar cũ — placeholder hiện không có giá trị thật. |

## Phases

- [x] Phase 01 — Backend list users endpoint (S) — [phase-01-backend-list-users-endpoint.md](phase-01-backend-list-users-endpoint.md)
- [x] Phase 02 — Frontend feature module + Users list page (M) — [phase-02-frontend-users-list-page.md](phase-02-frontend-users-list-page.md)
- [x] Phase 03 — Settings hub + per-user page expansion (S) — [phase-03-settings-hub-and-per-user-expand.md](phase-03-settings-hub-and-per-user-expand.md)
- [x] Phase 04 — Routing + sidebar wiring (S) — [phase-04-routing-and-sidebar.md](phase-04-routing-and-sidebar.md)
- [x] Phase 05 — Final verification + manual smoke (S) — [phase-05-final-verification.md](phase-05-final-verification.md)

## Final Verification

Chạy ở thư mục repo root:

```powershell
# Backend (chỉ build project chạm, không full solution)
dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -c Debug
dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AdminUsers"

# Frontend
npm --prefix frontend run typecheck
npm --prefix frontend run lint
npm --prefix frontend run build
```

Manual smoke ở browser (3 account: ADMIN, SALES1 active, SALES2 soft-deleted) — xem chi tiết tại [phase-05-final-verification.md](phase-05-final-verification.md).

## Rollback / Recovery

- **Không có migration DB** → rollback = revert PR thuần code.
- **Revert FE only**: 2 nav entry biến mất, `/settings` trở về placeholder cũ; các page cũ vẫn truy cập được qua URL trực tiếp (như trước).
- **Revert BE only**: endpoint `/api/admin/users` 404 → trang `/admin/users` FE hỏng (list empty + error toast). Nếu cần giữ BE và revert FE, FE chỉ ảnh hưởng entry sidebar.
- **Thứ tự ưu tiên rollback**: revert FE trước (nhanh, không hỏng BE); BE giữ lại an toàn (chỉ là dead endpoint).
