# Fix login post-redirect → /403 for permission-restricted users

## Goal
Sau khi login thành công, validate `location.state.from.pathname` với permissions của
user. Nếu user không có quyền cho route đó (vd: sale user, `from = /admin/dashboard`),
fallback về `/` thay vì để ProtectedRoute redirect sang `/403`. Giữ UX deep-link
return cho session-expiry case khi user vẫn có quyền vào route gốc.

## Scope
- In scope:
  - Tạo module `frontend/src/lib/route-permissions.ts` với `canAccessRoute(pathname, perms, roles)`.
  - Unit tests Vitest cho `canAccessRoute`.
  - Sửa `frontend/src/pages/login-page.tsx` để gọi `canAccessRoute` trước khi
    navigate (cả nhánh `onSuccess` của login và nhánh `isAuthenticated` redirect).
- Out of scope:
  - Không sửa `ProtectedRoute` — vẫn redirect `/403` khi user click trực tiếp
    vào route restricted sau khi đã login (đây là hành vi đúng).
  - Không bỏ trang `/403`.
  - Không thay đổi backend (permissions seeder + API trả permissions đúng — đã
    verify qua DevTools trong brainstorm).
  - Không thêm test sync giữa `App.tsx` và `route-permissions.ts` (rủi ro nhỏ,
    có thể bổ sung sau).

## Assumptions
- Login response (`LoginResponse.User`) trả `permissions: string[]` và `roles: string[]`
  với giá trị chính xác. Đã verify qua DevTools (user xác nhận có list permissions).
- React Router không dùng trailing slash, nên regex không cần handle.
- Pattern map ở `route-permissions.ts` mirror đúng các `<ProtectedRoute permission=...>`
  trong `App.tsx`. Nếu sau này thêm route mới mà quên update map: user vẫn bị bounce
  sang `/403` sau login (= baseline hiện tại) — không tệ hơn.

## Risks
- **Pattern regex sai** → user có quyền cũng bị fallback về `/`. Mitigation: unit
  tests cover các pattern chính + manual test sau deploy.
- **Loop infinite** nếu `from = /login` hoặc `/403`. Mitigation: guard exclude
  hai prefix này trước khi gọi `canAccessRoute`.
- **Route mới thêm sau này quên update map** → hành vi giống cũ (bị /403 sau
  login). Acceptable, không tệ hơn baseline.

## Phases
- [x] Phase 01 — Create route-permissions module + tests (S) — `phase-01-route-permissions-module.md`
- [x] Phase 02 — Wire into LoginPage + verify (S) — `phase-02-wire-loginpage.md`

## Final Verification
Chạy trong `frontend/`:
```
npm run test -- route-permissions
npm run typecheck
npm run build
```
Tất cả phải pass clean. Sau đó manual test trên Railway production (xem
phase-02 → "Verification" section để biết kịch bản chi tiết).

## Rollback / Recovery
Frontend-only change, không có data migration. Rollback bằng:
```
git revert <commit-hash>
git push
```
Railway sẽ auto-rebuild và quay lại hành vi cũ (sale user vẫn bị /403 nếu
deep-link → admin route, nhưng đó là baseline hiện tại).
