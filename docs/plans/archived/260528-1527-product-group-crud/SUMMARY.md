# Product Group CRUD

## Goal

Expose full CRUD management for the `ProductGroup` catalog entity. The domain entity and
database table already exist. This plan adds the Application-layer service, WebApi controller,
integration tests, and a React frontend (list page + modal dialog) so that users can create,
read, update, and delete product groups from the UI.

## Scope

**In scope:**
- `GET /api/product-groups` — paginated list with search + isActive filter
- `GET /api/product-groups/{id}` — single item
- `POST /api/product-groups` — create (code auto-generated if omitted)
- `PUT /api/product-groups/{id}` — update (code immutable after creation)
- `DELETE /api/product-groups/{id}` — soft delete
- Integration tests covering CRUD happy paths, code-conflict, auto-code, delete-with-products, and auth
- React list page at `/product-groups` with inline create/edit dialog
- Sidebar nav item "Nhóm hàng hóa" below "Hàng hóa"
- Reuse `products.*` permissions (no new permission constants)
- Invalidate `lookupKeys.productGroups()` cache after mutations so the ProductForm dropdown stays fresh

**Out of scope:**
- New database migration (table already exists)
- New permission constants
- Separate form page/route for create/edit (dialog only)

## Assumptions

- `ProductGroup` entity, table `product_groups`, `ProductGroupConfiguration`, and
  `IAppDbContext.ProductGroups` are all in place and require no changes.
- Seeded data includes at least one `ProductGroup` (e.g., code `"EPS"`) for tests to reference.
- `OnDelete(SetNull)` on `Product.ProductGroupId` is already configured; deleting a group
  sets `ProductGroupId = null` on linked products — no extra handling needed.
- FluentValidation validators are discovered via `AddValidatorsFromAssembly` — no manual
  registration required for validators.
- Frontend `apiGet/apiPost/apiPut/apiDelete` helpers from `@/lib/api-client` handle the
  `/api` base prefix.

## Risks

- Test seed data: tests look up product groups by code. If seeded codes differ, tests will
  fail — verify against actual seed data before running.
- Lookup cache invalidation: if `lookupKeys` is not also invalidated inside the new
  product-group hooks, the ProductForm group dropdown lags one mutation behind.

## Phases

- [x] Phase 01 — Backend: Models, Service Stub & DI (S) — `phase-01-backend-scaffold.md`
- [x] Phase 02 — Backend: Integration Tests → Full Implementation (M) — `phase-02-backend-impl.md`
- [x] Phase 03 — Frontend: Feature Module (M) — `phase-03-frontend-module.md`
- [x] Phase 04 — Frontend: Pages & App Wiring (M) — `phase-04-frontend-pages.md`

## Final Verification

```bash
# Backend tests (requires Docker for Testcontainers)
cd backend
dotnet test tests/OrderMgmt.IntegrationTests \
  --filter "FullyQualifiedName~ProductGroupCrud" \
  --logger "console;verbosity=normal"

# Frontend type-check
cd frontend
npm run typecheck
```

## Rollback / Recovery

- No migration was added, so there is nothing to roll back at the database level.
- Backend: revert the new files under `Application/Catalog/ProductGroups/` and
  `WebApi/Controllers/ProductGroupsController.cs`, remove the DI line from
  `DependencyInjection.cs`.
- Frontend: revert new files under `features/product-groups/` and
  `pages/product-groups/`, remove the route from `App.tsx`, the nav item from
  `app-layout.tsx`, and the extra `invalidateQueries` call from `features/products/hooks.ts`.
