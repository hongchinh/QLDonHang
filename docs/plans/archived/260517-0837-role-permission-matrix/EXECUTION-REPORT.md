# Execution Report — Role × Permission Matrix Management

**Plan:** [SUMMARY.md](SUMMARY.md)
**Mode:** Interactive
**Executed:** 2026-05-17

## Phases

| Phase | Status | Note |
|---|---|---|
| Phase 01 — Backend service + API + seeder + tests | [x] complete | Build pass; integration tests deferred (Docker unavailable at execute time) |
| Phase 02 — Frontend feature module + matrix page + tests | [x] complete | 7/7 vitest pass |
| Phase 03 — Wire route + sidebar + doc + smoke | [x] complete | 17/17 tests pass (matrix + route-permissions); doc updated |

## Files changed

### Backend — new (11)
- DTOs: `OrderMgmt.Application/Identity/Admin/Models/{PermissionDto,RoleListItemDto,RoleDetailDto,CreateRoleRequest,UpdateRoleRequest,UpdateRolePermissionsRequest}.cs`
- Validators: `OrderMgmt.Application/Identity/Admin/Validators/{CreateRoleRequest,UpdateRoleRequest,UpdateRolePermissionsRequest}Validator.cs`
- Service: `OrderMgmt.Application/Identity/Admin/Interfaces/IAdminRoleService.cs` + `OrderMgmt.Application/Identity/Admin/Services/AdminRoleService.cs`
- Controller: `OrderMgmt.WebApi/Controllers/AdminRolesController.cs`

### Backend — modified (2)
- `OrderMgmt.Application/DependencyInjection.cs` — registered `IAdminRoleService`
- `OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs` — `SeedRolesAsync` branch logic (ADMIN re-apply / non-Admin skip / 0-perm fallback)

### Backend — tests (3)
- `OrderMgmt.IntegrationTests/Admin/AdminRolesCrudTests.cs` (13 cases)
- `OrderMgmt.IntegrationTests/Admin/AdminRolesPermissionsTests.cs` (6 cases incl. token-refresh live-update)
- `OrderMgmt.IntegrationTests/Admin/DbSeederUpgradeTests.cs` (3 cases)

### Frontend — new (10)
- Feature module: `features/admin-roles/{types,keys,api,hooks}.ts`
- Page + components: `pages/admin/roles-matrix-page.tsx`, `pages/admin/components/{role-matrix-table,role-create-dialog,role-rename-dialog,role-delete-confirm}.tsx`
- Test: `pages/admin/roles-matrix-page.test.tsx` (7 cases)

### Frontend — modified (4)
- `lib/permissions.ts` — added `roles.view`, `roles.manage`
- `lib/route-permissions.ts` + `.test.ts` — added rule + 2 test cases
- `App.tsx` — wired `/admin/roles` ProtectedRoute
- `components/layout/app-layout.tsx` — added "Phân quyền" nav item with `ShieldCheck` icon

### Docs — modified (1)
- `docs/architecture/system-architecture.md` — new "Role × Permission management" subsection under Authorization

## Verification commands run

```powershell
# Backend
dotnet build backend/OrderMgmt.sln                       # ✓ clean
# dotnet test (integration) — DEFERRED — Docker unavailable

# Frontend
cd frontend
npx tsc -b                                                # ✓ clean
npm run lint                                              # ✓ 0 errors, 3 warnings (all pre-existing)
npx vitest run                                            # ✓ 105/105 tests pass (15 files)
```

## Deviations from plan

1. **`RolePermission` cascade soft-delete** — Plan originally assumed `AppDbContext.CascadeSoftDeleteAsync` would propagate to `RolePermission` rows when `Role.IsDeleted = true`. During build the compiler revealed `RolePermission` is a pure join entity (no `BaseEntity` / `ISoftDeletable`), so the cascade SKIPS it. Adjusted `AdminRoleService.DeleteAsync` to **hard-delete** `RolePermission` rows via `_db.RolePermissions.RemoveRange(role.RolePermissions)` before soft-deleting the role. Plan SUMMARY + phase-01 + test names updated to reflect this.

2. **Vitest test infrastructure** — Plan referenced MSW; this codebase already uses `vi.mock` of API modules + `QueryClientProvider` instead. Followed the existing codebase pattern.

3. **Self-lockout safeguard** — Implemented via `ConfirmDialog` (existing component) per the [B1] fix in the plan, not a new `AlertDialog`.

4. **Integration tests deferred** — Docker Desktop was not running at execute time; user chose to mark Phase 01 as build-pass-only and defer test run. Tests compile and are ready to run; they will execute on next CI / when Docker is available locally.

## Residual risks / follow-ups

- **Integration tests** (~22 new cases) must run before deploy. They depend on Docker / Testcontainers PostgreSQL.
- **Frontend `PERMISSIONS` const drift** (pre-existing, flagged in earlier review): the array is missing `quotations.cancel_confirmed`, `quotations.view_cost`, `reports.{profit,debt,delivery}` and still lists obsolete `quotations.approve`. Not in scope of this plan; consider a follow-up to sync to backend.
- **No audit table** for role/permission changes. Logged structured via `ILogger<AdminRoleService>` (info-level) but not persisted to DB. Manager-level audit was flagged in review as out-of-scope; revisit if compliance need surfaces.
- **No optimistic concurrency** on role permission updates. Plan accepts last-write-wins for the small-admin-team use case.

## Manual smoke test checklist (from plan)

The following should be validated by the user on a dev environment (frontend + backend running):

1. Login `admin/Admin@123` → sidebar shows "Phân quyền" under Setting group.
2. Click → matrix loads, ADMIN column checkboxes are disabled+checked.
3. Toggle `quotations.delete` on SALES → "1 thay đổi chưa lưu" badge + Save button enables.
4. Save → success toast + 60-minute live-update info toast; reload → change persists.
5. "Thêm role" → form → create `TEST_LEAD` with 2 permissions → new column appears.
6. `/admin/users` → assign `TEST_LEAD` to a user.
7. Login as that user (incognito) → permissions visible.
8. Back as admin → delete `TEST_LEAD` → 409 "đang được gán cho 1 user".
9. Change user role → delete `TEST_LEAD` → OK.
10. Login as plain SALES → "Phân quyền" hidden; force-navigate `/admin/roles` → redirect / 403.
