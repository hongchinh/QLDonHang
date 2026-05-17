# System Architecture

## Tổng quan

```
┌──────────────────────────┐         HTTPS/JSON        ┌─────────────────────────────┐
│ Frontend (React + Vite)  │ ────────────────────────► │ Backend (.NET 9 Web API)    │
│ Tailwind + shadcn/ui     │                            │ Clean Architecture          │
│ TanStack Query / Router  │ ◄──────────────────────── │ JWT Bearer + Refresh Token  │
└──────────────────────────┘                            └────────────┬────────────────┘
                                                                     │ Npgsql/EF Core 9
                                                                     ▼
                                                        ┌────────────────────────────┐
                                                        │ PostgreSQL 16              │
                                                        └────────────────────────────┘
```

## Backend — Clean Architecture

```
OrderMgmt.WebApi          ← Controllers, Middleware, Authorization, Program.cs
        ▲
        │ depends on
OrderMgmt.Infrastructure  ← EF Core DbContext, Migrations, JWT, BCrypt, RefreshTokenService, Seed
        ▲
        │ depends on (interfaces)
OrderMgmt.Application     ← DTOs, Use-case Services, Validators, Mapster IRegister
        ▲
        │ depends on
OrderMgmt.Domain          ← Entities, Enums, Constants, DomainException
```

**Phụ thuộc**: Domain isolated. Application chỉ phụ thuộc Domain — định nghĩa ports (`IAppDbContext`, `ICurrentUser`, `IDateTime`, `IJwtTokenGenerator`, `IPasswordHasher`, `IRefreshTokenService`). Infrastructure cài đặt port. WebApi compose qua DI.

## Auth flow

```
POST /api/auth/login (rate-limited 5/min/IP)
  │
  ├─► AuthService.LoginAsync
  │     ├─ BCrypt.Verify(password, user.PasswordHash)
  │     ├─ IJwtTokenGenerator.Generate(user, roles, permissions) → access token (60min)
  │     └─ IRefreshTokenService.IssueAsync(user, ip, ua)         → refresh token (14 days, SHA-256 in DB)
  │
  ▼ Body: { accessToken, refreshToken, expiresAt, refreshTokenExpiresAt, user }

POST /api/auth/refresh (rate-limited)
  │
  ├─► RefreshTokenService.RotateAsync
  │     ├─ Hash incoming → look up in refresh_tokens
  │     ├─ If RevokedAt is set → REUSE-DETECTION → revoke entire active family for user, 401
  │     ├─ If ExpiresAt ≤ now → 401
  │     ├─ Mark old token RevokedAt + ReplacedByTokenHash
  │     └─ Issue new access + new refresh
  │
  ▼ Body: { accessToken, refreshToken, ... }

POST /api/auth/logout  → RevokeAsync(reason="LOGOUT")

GET /api/auth/me  (Authorize)
  │
  └─► reads ICurrentUser (JWT claims only — no DB hit)
```

**Access token claims**: `sub`, `name`, `email`, `full_name`, `role[]`, `permission[]`.

## Authorization

- Custom `PermissionPolicyProvider` resolves `perm:<code>` policy names dynamically.
- `[HasPermission(Permissions.X.Y)]` attribute on controller actions.
- `PermissionAuthorizationHandler` checks `permission` claim in JWT.
- Role checks (`User.IsInRole`) available via `ClaimTypes.Role`.

### Role × Permission management

- UI `/admin/roles` (gated by `roles.view`) cho phép user có `roles.manage` quản lý ma trận Role × Permission qua `AdminRolesController`. Mặc định cả hai permission được seed cho ADMIN + MANAGER.
- 7 endpoint: `GET /api/admin/permissions`, `GET/POST/PUT/DELETE /api/admin/roles[...]`, `PUT /api/admin/roles/{id}/permissions`.
- **ADMIN role** bị server từ chối mọi mutation lên `RolePermissions` (`ForbiddenException`) — luôn full quyền.
- **System role khác** (SALES/ACCOUNTANT/WAREHOUSE/MANAGER, `IsSystem=true`): admin có thể thêm/bớt permission; chặn rename/delete.
- **Custom role** (`IsSystem=false`): full CRUD; xoá bị chặn (`ConflictException`) nếu còn user gán.
- **Hard-delete `RolePermission`**: `RolePermission` là pure join entity, không phải `ISoftDeletable` → khi xoá role, `AdminRoleService.DeleteAsync` hard-delete các `RolePermission` rows trước, rồi soft-delete role.
- **DbSeeder behavior** (`SeedRolesAsync`): ADMIN re-apply full permissions mỗi startup (tự nhận permission mới deploy); các system role khác chỉ seed khi `RolePermissions` rỗng (fallback an toàn) — không ghi đè customisation của admin sau deploy.
- **Live update**: thay đổi permission được áp dụng cho user đang đăng nhập sau khi access token làm mới (~60 phút) hoặc gọi `/api/auth/refresh` — `RefreshTokenService.RotateAsync` re-load `RolePermissions` từ DB và cấp access token mới với claim mới.
- **Audit**: mỗi mutation log structured (`role code`, `user id`, `added/removed codes`) qua `ILogger<AdminRoleService>`.

## Standard API response

Mọi controller trả `ApiResponse<T>` (success) hoặc `ApiResponse` với `error` (fail). `GlobalExceptionMiddleware` chuyển exception → HTTP status:

| Exception                    | HTTP Status | error.code |
| ---------------------------- | ----------- | ---------- |
| FluentValidation Validation  | 400         | VALIDATION |
| DomainException              | 400         | (custom)   |
| AuthenticationException      | 401         | UNAUTHENTICATED |
| UnauthorizedAccessException  | 401         | UNAUTHORIZED |
| ForbiddenException           | 403         | FORBIDDEN  |
| NotFoundException            | 404         | NOT_FOUND  |
| ConflictException            | 409         | CONFLICT   |
| RateLimiter rejection        | 429         | (handler-defined) |
| (others)                     | 500         | INTERNAL_ERROR |

4xx được log ở mức `Information`; 5xx ở mức `Error`.

## Soft delete & audit

- Mọi entity nghiệp vụ kế thừa `BaseEntity` (Id init-only, `IAuditableEntity`, `ISoftDeletable`).
- `AppDbContext.SaveChangesAsync` tự điền `CreatedAt/By`, `UpdatedAt/By` từ `ICurrentUser + IDateTime`.
- **Cascade soft-delete**: khi 1 entity `ISoftDeletable` chuyển `IsDeleted false → true`, `AppDbContext` tự cascade qua collection navigation tới các child cũng là `ISoftDeletable` (fixed-point loop, nhiều cấp).
- EF query filter `HasQueryFilter(x => !x.IsDeleted)` loại bản ghi đã xóa khỏi mọi query.
- Unique index **filtered** trên `IsDeleted = false` để cho phép tái sử dụng `Code`/`Username`/`Email` sau khi soft-delete.
- Join tables (`UserRole`, `RolePermission`) có query filter dựa trên principal entities (`!User.IsDeleted && !Role.IsDeleted`).

## Per-user quotation scoping

- `Quotation.OwnerUserId` (NOT NULL, FK Users, Restrict) — chủ sở hữu báo giá; service set `OwnerUserId = currentUser.UserId` ở `CreateAsync`.
- `QuotationService.ApplyOwnerScope` lọc list/detail theo owner; user có permission `quotations.view_all` bypass (mặc định gán ADMIN/MANAGER).
- Permissions mới: `quotations.{view_all, transfer_own, transfer_any, clone_orphan, bypass_lock}`, `user_settings.manage`.
- Feature flag `Features:QuotationOwnerScope = false` để rollback nhanh (bỏ qua scope + access guard, vẫn giữ `[HasPermission]` & lock-at).
- `UserQuotationSettings (UserId UNIQUE, LockAtStatus, TemplateFileName, ...)` — config per-user; admin sửa qua `/api/admin/user-settings/{userId}/lock-at`; user chỉ xem qua `/api/me/quotation-settings`.
- Lock-at: user không có `quotations.bypass_lock` không sửa được báo giá có `status >= LockAtStatus` (thứ tự logic Draft<Sent<Confirmed; Cancelled tách riêng).
- Template Excel per-user: upload qua `PUT /api/me/quotation-settings/template` (validate magic bytes, MIME, .xlsx, zip-bomb cap, ClosedXML parse). File lưu `templates/users/{userId}.xlsx`. Render dùng template của *owner báo giá*, fallback về `QuotationExport:TemplatePath`.
- Audit `QuotationOwnerHistory` ghi mọi lần chuyển nhượng (single hoặc bulk).
- Bulk-transfer: `POST /api/admin/users/{userId}/transfer-quotations` (idempotent, chuyển toàn bộ owner=userId; option `IncludeCancelled`).
- Clone: `POST /api/quotations/{id}/clone` — bản sao Draft với owner = currentUser; orphan source cần `quotations.clone_orphan`.
- Dashboard: `GET /api/dashboard/quotation-stats` — tự scope theo owner; revenue loại Cancelled.

## Migration & seed

- 10 migrations: `CreatePermissionsTable`, `AddFilteredUniqueIndexes`, `AddRefreshTokens`, `SnakeCaseNamingConvention`, `AddPricingModeToProduct`, `AddQuotations`, `EnableUnaccent`, `AddQuotationOwner`, `AddQuotationOwnerHistory`, `AddUserQuotationSettings`.
- Auto-migrate được bật qua config `Database:AutoMigrateAndSeed` (Development only). Production dùng CI/CD `dotnet ef database update`.
- `DbSeeder` chạy bên trong Postgres `pg_advisory_lock` để concurrent app instances tuần tự hoá migrate + seed.
- Admin user chỉ seed khi `Seed:AdminPassword` được cung cấp.

## Logging & observability

- Serilog với enrichers: `FromLogContext` + custom `LoggingContextMiddleware` push `CorrelationId` (header `X-Correlation-Id` hoặc TraceIdentifier) + `UserId` (từ claims).
- Console + rolling file `logs/qldh-{date}.log` (14 ngày).
- Health endpoints: `/health/live` (liveness, no deps) và `/health/ready` (readiness, includes DB check qua `AddDbContextCheck<AppDbContext>`).

## Frontend layering

```
pages/        ← route-level screens (orchestrate hooks + UI)
features/     ← business modules: api.ts + hooks.ts + schema.ts + types.ts
components/   ← reusable UI (ui/ là shadcn-style)
routes/       ← ProtectedRoute (auth + permission/role guards)
stores/       ← zustand global state (auth, persists access/refresh tokens)
lib/          ← api-client (axios + interceptors), query-client, utils
```
