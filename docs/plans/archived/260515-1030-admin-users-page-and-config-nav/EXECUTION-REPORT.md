# Execution Report — Admin Users page + Config navigation

**Plan**: [SUMMARY.md](SUMMARY.md) (260515-1030-admin-users-page-and-config-nav)
**Executed**: 2026-05-15
**Mode**: Batch

## Phases

| # | Name | Status |
|---|---|---|
| 01 | Backend list users endpoint | [x] complete |
| 02 | Frontend feature module + Users list page | [x] complete |
| 03 | Settings hub + per-user page expansion | [x] complete |
| 04 | Routing + sidebar wiring | [x] complete |
| 05 | Final verification + manual smoke | [x] complete |

## Files changed

### Backend (new)
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/AdminUserListItemDto.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Models/AdminUserListQuery.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Interfaces/IAdminUserService.cs`
- `backend/src/OrderMgmt.Application/Identity/Admin/Services/AdminUserService.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/AdminUsersController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Admin/AdminUsersListTests.cs`

### Backend (modified)
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` (DI: `IAdminUserService → AdminUserService`)

### Frontend (new)
- `frontend/src/features/admin-users/{types,api,keys,hooks}.ts`
- `frontend/src/pages/admin/users-list-page.tsx`
- `frontend/src/pages/settings/settings-hub-page.tsx`

### Frontend (modified)
- `frontend/src/pages/admin/user-settings-page.tsx` (thêm card template read-only + nút bulk-transfer)
- `frontend/src/pages/admin/bulk-transfer-page.tsx` (thay raw GUID input bằng picker dropdown user active, exclude source user)
- `frontend/src/App.tsx` (route `/admin/users` + thay placeholder `/settings` thành SettingsHubPage)
- `frontend/src/components/layout/app-layout.tsx` (thêm 2 nav entry: "Cài đặt của tôi", "Quản lý người dùng"; bỏ entry placeholder `/settings`)

## Verification commands run

| Command | Result |
|---|---|
| `dotnet build backend/src/OrderMgmt.Application/OrderMgmt.Application.csproj -c Debug` | Build succeeded, 0 warning |
| `dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -c Debug` | Build succeeded, 0 warning (sau khi user stop dev WebApi process) |
| `dotnet test --filter "FullyQualifiedName~AdminUsersList"` | **5/5 pass** (9s, dùng `TEST_DB_CONNECTION` trỏ về local Postgres DB `qldonhang_int_test`) |
| `npm --prefix frontend run typecheck` | clean |
| `npm --prefix frontend run lint` | clean |
| `npm --prefix frontend run build` | OK (5.04s) |

## Deviations from plan

1. **Tests dùng local Postgres thay vì Testcontainers**. Plan giả định Testcontainers (Docker) khả dụng; thực tế Docker không chạy nên dùng env `TEST_DB_CONNECTION` trỏ vào Postgres 18 đang chạy local (`qldonhang_int_test` được tạo riêng cho test, drop sau khi xong). Test fixture hỗ trợ sẵn override này.
2. **Stop dev WebApi**: do file lock `OrderMgmt.WebApi/bin/*.dll` ngăn build WebApi, user đã stop dev WebApi (PID 23780) để chạy integration test (theo memory rule + xác nhận tương tác). Sau khi xong test, build WebApi lại đã pass và sẵn sàng khởi động lại.
3. **Manual smoke browser (16 case) chưa chạy**: user chấp nhận chốt phase 05 dựa trên automated checks; manual smoke sẽ do user thực hiện sau khi khởi động lại dev WebApi và FE dev server.

## Residual risks / follow-ups

- **Manual smoke pending**: 16 case ở [phase-05-final-verification.md](phase-05-final-verification.md#3-manual-smoke-browser-3-account) chưa thực hiện. Khuyến nghị: user start dev WebApi + `npm --prefix frontend run dev`, kiểm tra với 3 account (ADMIN, SALES1 active, SALES2 soft-deleted) trước khi merge/deploy.
- **Seeding test users cho dev**: dev DB hiện có thể chưa có sales1/sales2 để smoke. Có thể tạo qua SQL trực tiếp hoặc seeder.
- **Endpoint security**: DTO `AdminUserListItemDto` không trả `Email`/`PhoneNumber`/`PasswordHash` (verified). Permission guard `user_settings.manage` test case `Sales_user_gets_forbidden` xác nhận 403 cho user thường.
- **Performance**: ~30 user → client-side filter ổn. Khi DB vượt vài trăm user cần thêm pagination ở BE (out of scope plan này).
