# Execution Report — Header GMO-style (Phases 01–04)

**Plan reference:** [SUMMARY.md](SUMMARY.md)
**Date (Phase 01):** 2026-05-17 — visual scaffold (FE only)
**Date (Phases 02–04):** 2026-05-17 — branding + global search + notifications
**Mode:** Phase 01 = Interactive; Phases 02–04 = Batch

## Phases Completed

- [x] **Phase 01 — Visual scaffold (FE only)** — [phase-01-visual-scaffold.md](phase-01-visual-scaffold.md)
- [x] **Phase 02 — Branding upload (BE + FE)** — [phase-02-branding-upload.md](phase-02-branding-upload.md)
- [x] **Phase 03 — Global search (BE + FE)** — [phase-03-global-search.md](phase-03-global-search.md)
- [x] **Phase 04 — Notifications (BE + FE)** — [phase-04-notifications.md](phase-04-notifications.md)

---

## Files Changed (Phases 02–04)

### Backend — Phase 02 (Branding)

**Added**
- `backend/src/OrderMgmt.Domain/Branding/SystemBranding.cs` — singleton entity (Id=1) with `LogoFull`/`LogoMark` BLOBs.
- `backend/src/OrderMgmt.Application/Branding/Interfaces/IBrandingService.cs`
- `backend/src/OrderMgmt.Application/Branding/Services/BrandingService.cs`
- `backend/src/OrderMgmt.Application/Branding/Models/BrandingDto.cs`
- `backend/src/OrderMgmt.Application/Branding/Models/LogoStreamResult.cs`
- `backend/src/OrderMgmt.Application/Branding/Models/LogoUpload.cs` (Application-layer upload abstraction)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/SystemBrandingConfiguration.cs` (`bytea` columns + singleton `HasData`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/20260517070913_AddSystemBranding.cs` (+ Designer)
- `backend/src/OrderMgmt.WebApi/Controllers/SettingsController.cs`

**Modified**
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` — added `DbSet<SystemBranding>`
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` — added `SystemBranding` DbSet
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — `AddScoped<IBrandingService, BrandingService>()`
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (regenerated)

### Backend — Phase 03 (Global Search)

**Added**
- `backend/src/OrderMgmt.Application/Search/Interfaces/ISearchService.cs`
- `backend/src/OrderMgmt.Application/Search/Services/SearchService.cs`
- `backend/src/OrderMgmt.Application/Search/Models/GlobalSearchResultDto.cs`
- `backend/src/OrderMgmt.Application/Search/Models/QuotationSearchItemDto.cs`
- `backend/src/OrderMgmt.WebApi/Controllers/SearchController.cs`

**Modified**
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — `AddScoped<ISearchService, SearchService>()`

### Backend — Phase 04 (Notifications)

**Added**
- `backend/src/OrderMgmt.Domain/Notifications/Notification.cs`
- `backend/src/OrderMgmt.Application/Notifications/Interfaces/INotificationService.cs`
- `backend/src/OrderMgmt.Application/Notifications/Services/NotificationService.cs`
- `backend/src/OrderMgmt.Application/Notifications/Models/NotificationDto.cs`
- `backend/src/OrderMgmt.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs` (composite index `(UserId, IsRead, CreatedAt DESC)`)
- `backend/src/OrderMgmt.Infrastructure/Persistence/Migrations/20260517094942_AddNotifications.cs` (+ Designer)
- `backend/src/OrderMgmt.WebApi/Controllers/NotificationsController.cs`
- `backend/tests/OrderMgmt.IntegrationTests/Notifications/NotificationsControllerTests.cs` (6 test cases)

**Modified**
- `backend/src/OrderMgmt.Application/Common/Interfaces/IAppDbContext.cs` — added `DbSet<Notification>`
- `backend/src/OrderMgmt.Infrastructure/Persistence/AppDbContext.cs` — added `Notifications` DbSet
- `backend/src/OrderMgmt.Application/DependencyInjection.cs` — `AddScoped<INotificationService, NotificationService>()`
- `backend/src/OrderMgmt.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (regenerated)

### Frontend — Phase 02 (Branding)

**Added**
- `frontend/src/components/ui/tabs.tsx` — shadcn Tabs wrapper (`@radix-ui/react-tabs`)
- `frontend/src/features/branding/api.ts` — meta + upload + `logoUrl()` URL helper
- `frontend/src/features/branding/hooks.ts` — `useBrandingMeta`, `useUploadBranding`
- `frontend/src/features/branding/branding-tab.tsx` — admin-only tab UI with 2 upload cards (logo ngang + logo vuông)

**Modified**
- `frontend/src/components/layout/header/brand-block.tsx` — uses `useBrandingMeta()`, swaps to logo full/mark, falls back to placeholder icon+text
- `frontend/src/pages/settings/my-quotation-settings-page.tsx` — wraps in Tabs when user has `user_settings.manage`; renders single content otherwise
- `frontend/package.json` + `frontend/package-lock.json` — added `@radix-ui/react-tabs`

### Frontend — Phase 03 (Global Search)

**Added**
- `frontend/src/components/ui/popover.tsx` — Radix Popover wrapper
- `frontend/src/components/ui/sheet.tsx` — Radix Dialog as side-sheet (for mobile search overlay)
- `frontend/src/features/search/api.ts` — `searchApi.global(q)`
- `frontend/src/features/search/hooks.ts` — `useGlobalSearch` with `enabled: q.length >= 3`
- `frontend/src/components/layout/header/header-search.tsx` — desktop search with Popover, debounce 250ms, Ctrl/Cmd+K, arrow keys, Enter, Esc
- `frontend/src/components/layout/header/header-search-mobile-sheet.tsx` — mobile fullscreen search sheet
- `frontend/src/components/layout/header/search-results-list.tsx` — shared result list UI (customers + quotations groups)
- `frontend/src/components/layout/header/search-results-helpers.ts` — `flattenResultIndex`, `totalResultCount` (extracted to silence react-refresh)

**Modified**
- `frontend/src/components/layout/header/app-header.tsx` — replaced `HeaderSearchPlaceholder`/`HeaderSearchMobileButton` with real components
- `frontend/package.json` + `frontend/package-lock.json` — added `@radix-ui/react-popover`, `@radix-ui/react-dialog`

**Deleted**
- `frontend/src/components/layout/header/header-search-placeholder.tsx`
- `frontend/src/components/layout/header/header-search-mobile-button.tsx`

### Frontend — Phase 04 (Notifications)

**Added**
- `frontend/src/features/notifications/api.ts`
- `frontend/src/features/notifications/hooks.ts` — `useUnreadCount` (polling 60s, paused in background), `useNotifications`, `useMarkRead`, `useMarkAllRead`
- `frontend/src/components/layout/header/header-notifications.tsx` — bell + badge (count, "9+" when >9), popover list, mark-all-read, auto-mark-on-click + navigate, `formatDistanceToNow` with `vi` locale

**Modified**
- `frontend/src/components/layout/header/app-header.tsx` — replaced placeholder with real `<HeaderNotifications />`

**Deleted**
- `frontend/src/components/layout/header/header-notifications-placeholder.tsx`

---

## Verification Commands Run

| Phase | Command | Outcome |
|---|---|---|
| 02 | `dotnet build backend/src/OrderMgmt.Application` | ✅ pass |
| 02 | `dotnet build backend/src/OrderMgmt.Infrastructure` | ✅ pass |
| 02 | `dotnet ef migrations add AddSystemBranding -o Persistence/Migrations` | ✅ generated |
| 02 | `npm run typecheck` | ✅ pass |
| 02 | `npm run lint` | ✅ pass (3 pre-existing warnings, no new) |
| 02 | `npx vitest run --no-file-parallelism` | ✅ 109/109 pass |
| 03 | `dotnet build backend/src/OrderMgmt.Application` | ✅ pass |
| 03 | `npm run typecheck` | ✅ pass |
| 03 | `npm run lint` | ✅ pass (3 pre-existing warnings; 2 react-refresh warnings fixed by moving helpers to separate file) |
| 03 | `npx vitest run --no-file-parallelism` | ✅ 109/109 pass |
| 04 | `dotnet build backend/src/OrderMgmt.Application` | ✅ pass |
| 04 | `dotnet build backend/src/OrderMgmt.Infrastructure` | ✅ pass |
| 04 | `dotnet build backend/src/OrderMgmt.WebApi` | ✅ pass (after user stopped running WebApi) |
| 04 | `dotnet build backend/tests/OrderMgmt.IntegrationTests` | ✅ pass (2 pre-existing warnings, no new) |
| 04 | `dotnet ef migrations add AddNotifications -o Persistence/Migrations` | ✅ generated |
| 04 | `npm run typecheck` | ✅ pass |
| 04 | `npm run lint` | ✅ pass (3 pre-existing warnings) |
| 04 | `npx vitest run --no-file-parallelism` | ✅ 109/109 pass |
| 04 | `dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~NotificationsControllerTests"` | ⏭ **Skipped** per user request (no TEST_DB_CONNECTION provided). Test file compiles. |

---

## Deviations from Plan

### Phase 02

1. **`Application/System/Branding/...` → `Application/Branding/...`** — The plan's namespace `OrderMgmt.Application.System.Branding` shadowed `System.Globalization` inside `QuotationService.cs:256` (`error CS0234`). Files were moved one level up (`OrderMgmt.Application.Branding.*`). No external API impact — `Application/System/` was a new directory, never referenced elsewhere.

2. **`IFormFile` → custom `LogoUpload` record** — `OrderMgmt.Application` does not (and should not) reference `Microsoft.AspNetCore.Http`. Followed the existing pattern from `OrderMgmt.Application.Identity.UserSettings.Models.UploadedFile`: the controller wraps `IFormFile` into a local `LogoUpload(fileName, contentType, length, openReadStream)` before passing to the service.

3. **`GET /api/settings/branding/logo` is `[AllowAnonymous]`, not `[Authorize]`** — The plan specified `[Authorize]` plus direct `<img src={logoUrl(...)} />`. These are inconsistent: browser `<img>` requests do not carry the `Authorization: Bearer` header (the app stores access token in memory, not as a cookie). Without auth on the request, the endpoint would always 401 and the logo never renders. Branding logos are public visual identity (equivalent to favicon), so I marked the logo stream as `[AllowAnonymous]` while keeping `GET /api/settings/branding` (meta) and `PUT /api/settings/branding` (upload) protected. Documented inline with a comment in `SettingsController.cs`.

4. **shadcn CLI artifact** — `npx shadcn@latest add tabs` (and `sheet` in Phase 03) wrote files into a literal `frontend/@/components/ui/` folder. Files were moved to `frontend/src/components/ui/` and the stray `frontend/@/` folder removed. Style normalised to single quotes to match repo convention.

5. **Migration output dir** — The default `dotnet ef migrations add` placed the new migration at `backend/src/OrderMgmt.Infrastructure/Migrations/` but the existing migrations live at `Persistence/Migrations/`. Re-ran with `-o Persistence/Migrations` to match convention.

### Phase 03

1. **Sequential queries instead of `Task.WhenAll`** — Plan called for parallel customer + quotation queries via `IDbContextFactory<AppDbContext>`. The codebase has no `IDbContextFactory` wiring and the scoped `DbContext` cannot serve two concurrent queries. Two sequential `await … ToListAsync()` calls were used instead. Documented inline. Dataset scale (small) makes this a non-issue; if perf ever matters, registering a context factory is a localised change.

2. **`useDebouncedValue` already existed** — The plan asked to create `frontend/src/lib/use-debounced-value.ts`. It existed already (different signature: `delayMs = 300` default). Reused the existing one with explicit `250` ms delay at call sites.

3. **Helper extraction for react-refresh** — `flattenResultIndex` and `totalResultCount` were initially co-located with `SearchResultsList` in `search-results-list.tsx`, triggering `react-refresh/only-export-components` warnings. Helpers moved to `search-results-helpers.ts`.

### Phase 04

1. **`CurrentUserId` source** — The plan suggested `ApiControllerBase.CurrentUserId`. That helper doesn't exist on `ApiControllerBase`. Injected `ICurrentUser` directly into `NotificationsController` and resolved `UserId` at the call site, matching existing controller patterns.

2. **No seed/manual notifications inserted** — Plan task #10 offered "A. add to DbSeeder" or "B. SQL". Skipped both: the migration was not applied (per user choice — "Generate migration files only"). The integration tests seed their own notifications directly via DbContext in `SeedNotificationAsync`. Manual end-to-end verification requires the migration to be applied first.

3. **Integration tests skipped, not failed** — Per user choice, no `TEST_DB_CONNECTION` was provided. The test file compiles (`dotnet build` ✅) and follows existing `WebAppFactory`/`PostgresCollection` patterns; it can be run later with `dotnet test backend/tests/OrderMgmt.IntegrationTests --filter "FullyQualifiedName~NotificationsControllerTests"` after providing a non-`qldonhang_test` connection string.

---

## Residual Risks / Follow-ups

1. **Migrations not applied to any database.** Both `AddSystemBranding` and `AddNotifications` migration files exist but were never executed against dev/staging/prod. Before manual testing of Phase 02/04 features, run:
   ```
   dotnet ef database update -p backend/src/OrderMgmt.Infrastructure -s backend/src/OrderMgmt.WebApi
   ```
   (This will apply both new migrations to whichever DB the WebApi is configured for.)

2. **Integration tests not run.** The new `NotificationsControllerTests` (6 cases) compiles but has not executed. Schedule a run with `TEST_DB_CONNECTION` set to a throw-away DB (NOT `qldonhang_test`).

3. **No manual browser verification of any phase 02–04 feature.** The plan's manual test checklists (e.g. "upload PNG → header swap on F5", "Ctrl+K focus", "60s polling visible in Network tab") have not been performed in this session. The user should:
   - Apply migrations.
   - Restart WebApi.
   - Login as admin → `/settings/my-quotation-settings` → upload logo (PNG ≤2MB).
   - Verify header brand block updates.
   - Search "test" in header → verify popover with 2 groups + Ctrl/Cmd+K + arrow nav.
   - Manually `INSERT INTO notifications (id, user_id, type, title, body, link, is_read, created_at) VALUES ('<guid>', '<admin user id>', 'Test', 'Test notification', 'Body', '/quotations', false, NOW())` to verify badge + popover + auto-mark-read.

4. **`logoUrl()` cache vs admin re-upload race.** On admin re-upload, `updatedAt` changes and React Query invalidates `['branding-meta']`. The next `<img>` render uses the new `v=<new updatedAt>` query string, so browser cache reads a fresh URL. The 5-min `Cache-Control: private, max-age=300` is keyed by full URL including `v`. This is the intended design and should "just work", but worth verifying in browser DevTools Network tab after re-upload.

5. **Pre-existing flaky test** (`roles-matrix-page.test.tsx`) still flakes with the default test pool; passes 109/109 with `--no-file-parallelism`. Not addressed in this session per the "surgical changes" rule. Follow-up: bump `waitFor` timeout to 3000ms in `waitForMatrix` (~1-line fix).

---

## Final Verification Status

- [x] Backend builds (Application + Infrastructure + WebApi)
- [x] Backend Integration tests project builds (NotificationsControllerTests compiles)
- [x] Frontend typecheck passes
- [x] Frontend lint passes (3 pre-existing warnings, no new)
- [x] Frontend tests 109/109 pass (with `--no-file-parallelism` to avoid the documented flake)
- [ ] Migrations applied to a database (deferred to user)
- [ ] Integration tests executed (deferred to user)
- [ ] Manual browser verification (deferred to user)

## Completion

Phases 01–04 implementation complete. Migration files generated but not applied. Integration test code written but not executed. Manual QA pending.
