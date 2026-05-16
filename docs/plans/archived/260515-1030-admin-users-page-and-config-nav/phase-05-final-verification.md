# Phase 05 — Final verification + manual smoke

**Status:** [x] complete
**Complexity:** S

## Objective

Chạy verification cuối + smoke test end-to-end với 3 account để xác nhận UX khép kín, không có regression.

## Files

(Không sửa file source nào trong phase này — chỉ chạy commands và quan sát.)

## Tasks

### 1. Automated checks

1. Build BE chỉ project chạm (memory rule: skip full solution):
   ```powershell
   dotnet build backend/src/OrderMgmt.WebApi/OrderMgmt.WebApi.csproj -c Debug
   ```

2. Chạy integration test mới (cần WebApi không đang chạy để tránh xung đột port test):
   ```powershell
   dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~AdminUsersList"
   ```
   *Note*: Nếu user yêu cầu chạy thêm để chắc chắn không regression, mở rộng filter:
   ```powershell
   dotnet test backend/tests/OrderMgmt.IntegrationTests/OrderMgmt.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~Admin|FullyQualifiedName~UserSettings"
   ```

3. FE chạy đủ 3 step:
   ```powershell
   npm --prefix frontend run typecheck
   npm --prefix frontend run lint
   npm --prefix frontend run build
   ```

### 2. Seed test data (manual via DB hoặc API)

4. Đảm bảo DB có đủ user cho smoke. Nếu chưa, tạo:
   - SALES1: role `SALES`, status Active (qua API auth/login đăng ký nếu có, hoặc INSERT SQL).
   - SALES2: role `SALES`, status Active, sau đó set `IsDeleted = true` (soft-deleted để test inactive).
   - ADMIN: đã có sẵn từ seeder.

   Nếu repo không có cách tạo user qua UI (Phase 06 cũ ghi chú "trang Users chưa tồn tại"), dùng SQL trực tiếp hoặc seeder.

### 3. Manual smoke (browser, 3 account)

Chạy `npm --prefix frontend run dev` + WebApi local.

| # | Account | Action | Expected |
|---|---|---|---|
| 1 | SALES1 | Đăng nhập → quan sát sidebar | Có "Cài đặt của tôi"; **không** có "Quản lý người dùng" |
| 2 | SALES1 | Click "Cài đặt của tôi" | Vào `/settings/my-quotation-settings`, page render OK |
| 3 | SALES1 | Gõ tay `/admin/users` | Redirect `/403` |
| 4 | SALES1 | Gõ tay `/settings` | Vào hub, chỉ thấy 1 card "Cài đặt báo giá của tôi" |
| 5 | ADMIN | Đăng nhập → sidebar | Có cả 2 entry mới; **không** có "Cấu hình" placeholder cũ |
| 6 | ADMIN | Click "Quản lý người dùng" | Vào `/admin/users`, list hiển thị ≥ ADMIN + SALES1 + SALES2 (SALES2 có badge "Đã nghỉ") |
| 7 | ADMIN | Search "sales" | Còn 2 row SALES1 + SALES2 |
| 8 | ADMIN | Bật toggle "Chỉ user đang hoạt động" | SALES2 biến mất |
| 9 | ADMIN | Click icon "Cấu hình báo giá" trên row SALES1 | Vào `/admin/user-settings/<sales1-id>`, có 3 card (lock-at, template read-only, chuyển nhượng) |
| 10 | ADMIN | Click "Mở trang chuyển nhượng" | Navigate `/admin/users/<sales1-id>/transfer-quotations`, page render OK; user nhận chọn bằng dropdown active users, không nhập GUID thô |
| 11 | ADMIN | Trên bulk-transfer page SALES1, mở dropdown user nhận | Không có SALES1; có ADMIN/SALES active khác nếu tồn tại |
| 12 | ADMIN | Quay lại Users list, click icon "Chuyển nhượng" trên row SALES2 (đã nghỉ) | Vẫn vào bulk-transfer page OK; source soft-deleted vẫn được phép, user nhận vẫn chỉ active |
| 13 | ADMIN | Gõ tay `/settings` | Vào hub mới với 2 card |

### 4. Regression spot-check

14. ADMIN → vào `/quotations` list → vẫn hoạt động, không lỗi.
15. ADMIN → vào `/customers` list → vẫn hoạt động.
16. SALES1 → upload template ở `/settings/my-quotation-settings` → vẫn upload được (xác nhận Phase 06 không bị ảnh hưởng).

### 5. Mark plan complete

17. Update file `SUMMARY.md` của plan: check tất cả phase `[x]`.
18. Commit changes (nếu user yêu cầu — không tự commit).

## Verification

Đã liệt kê ở task 1-3.

## Exit Criteria

- `dotnet build` WebApi project pass, không warning mới.
- `dotnet test --filter AdminUsersList` 5/5 test pass.
- FE `typecheck` + `lint` + `build` pass.
- 16 case manual smoke pass.
- Sidebar 2 account (admin/sales) hiển thị đúng entry; user thường không tiếp cận được trang admin.
- Không có regression ở page báo giá / khách hàng / cài đặt-của-tôi cũ.
