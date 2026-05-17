# Execution Report — Fix login post-redirect → /403 for permission-restricted users

**Plan:** `docs/plans/260516-1612-fix-login-redirect-403/SUMMARY.md`
**Executed:** 2026-05-16
**Mode:** Batch

## Phases Completed

- [x] Phase 01 — Create route-permissions module + tests (S)
- [x] Phase 02 — Wire route-permissions into LoginPage + verify (S)

## Files Changed

- `frontend/src/lib/route-permissions.ts` (new) — RULES map + `canAccessRoute()`.
- `frontend/src/lib/route-permissions.test.ts` (new) — 8 Vitest cases covering allow/deny, specificity ordering, and rule-less routes.
- `frontend/src/pages/login-page.tsx` (modify) — Added `pickPostLoginTarget` helper; both the `isAuthenticated` redirect branch and the login `onSuccess` callback now route to `/` if the user cannot access `from`. `/login` and `/403` `from` values are guarded to avoid redirect loops.

## Verification Run

From `frontend/`:

| Command | Outcome |
|---|---|
| `npm run test -- route-permissions` (after Phase 01) | 8/8 passed |
| `npm run typecheck` (after Phase 01) | 0 errors |
| `npm run typecheck` (after Phase 02) | 0 errors |
| `npm run lint` | 0 errors (3 pre-existing warnings unchanged) |
| `npm run test` | 75/75 passed across 12 files |
| `npm run build` | Built successfully in 6.64s |

## Deviations from Plan

None. Implementation matches the plan text verbatim (RULES, helper, both navigation branches, guards for `/login`/`/403`).

## Residual Risks / Follow-ups

- **RULES vs App.tsx drift**: If a new `<ProtectedRoute permission=...>` is added to `App.tsx` without updating `route-permissions.ts`, deep-linked sale-style users will fall back to `/403` after login (= baseline before this fix). Acceptable per plan; consider an automated sync test later.
- **`/customers/:id` rule uses `customers.view`** while `App.tsx` requires `customers.update` for edit. A user with `customers.view` but not `customers.update` will be allowed past the post-login fallback and then bounce to `/403` via `ProtectedRoute`. This is the documented simplification and matches baseline UX.
- **Manual smoke test on production** (dev + Railway) was not performed in this execution — left for the user per plan, since the change is frontend-only and reversible via `git revert`.
