# Directory Structure

```
QLDonHang/
├── backend/
│   ├── OrderMgmt.sln
│   ├── Directory.Build.props          # pinned EF/AspNetCore/Npgsql versions
│   ├── .editorconfig                  # code style + analyzer severities
│   ├── src/
│   │   ├── OrderMgmt.Domain/          # Entities, BaseEntity, DomainException, Permissions/RoleCodes
│   │   │   ├── Common/                # BaseEntity, IEntity, ISoftDeletable, IAuditableEntity, DomainException, AuthenticationException
│   │   │   ├── Constants/             # Permissions (per-module codes), RoleCodes (ADMIN/SALES/...)
│   │   │   ├── Entities/
│   │   │   │   ├── Catalog/           # Customer, CustomerAddress, Product, ProductGroup, Unit
│   │   │   │   └── Identity/          # User, Role, Permission, UserRole, RolePermission, RefreshToken
│   │   │   └── Enums/                 # CustomerStatus, OrderStatus, PaymentStatus, ...
│   │   ├── OrderMgmt.Application/     # Use-case services, validators, DTOs, ports
│   │   │   ├── Common/                # IAppDbContext, ICurrentUser, IDateTime, ApiResponse, PagedResult, PageRequestValidator
│   │   │   ├── Catalog/Customers/     # Service + Models + Validators + Mappings (Mapster IRegister)
│   │   │   └── Identity/              # IAuthService, AuthService, IRefreshTokenService (port), Validators
│   │   ├── OrderMgmt.Infrastructure/  # EF Core, JWT, BCrypt, RefreshTokenService, Seed
│   │   │   ├── Persistence/
│   │   │   │   ├── AppDbContext.cs    # cascade soft-delete + audit
│   │   │   │   ├── Configurations/    # EF entity type configurations
│   │   │   │   ├── Migrations/        # EF migrations (CreatePermissionsTable, AddFilteredUniqueIndexes, AddRefreshTokens)
│   │   │   │   └── Seed/              # DbSeeder + SeedOptions (advisory-lock guarded)
│   │   │   ├── Identity/              # JwtTokenGenerator, RefreshTokenService, BcryptPasswordHasher, JwtOptions, RefreshTokenOptions
│   │   │   └── Services/              # SystemDateTime
│   │   └── OrderMgmt.WebApi/          # Controllers, middleware, Program
│   │       ├── Authorization/         # PermissionPolicyProvider, HasPermissionAttribute, RateLimitPolicies
│   │       ├── Controllers/           # ApiControllerBase, AuthController, CustomersController
│   │       ├── Middleware/            # GlobalExceptionMiddleware, LoggingContextMiddleware
│   │       ├── Services/              # CurrentUser (claims-based ICurrentUser)
│   │       ├── appsettings.json       # production defaults (secrets blank — provide via env)
│   │       ├── appsettings.Development.json
│   │       └── Program.cs
│   └── tests/
│       └── OrderMgmt.IntegrationTests/   # xUnit + WebApplicationFactory + Testcontainers Postgres
├── frontend/                            # React + Vite (separate review)
├── docs/                                 # Documentation (this folder)
├── docker-compose.yml                    # Postgres 16 + pgAdmin for dev
├── AGENTS.md                             # Cross-agent protocols
└── CLAUDE.md                             # Claude-specific protocols
```

## Backend layering rules

- **Domain** depends on nothing.
- **Application** depends only on Domain; defines ports (`IAppDbContext`, `IDateTime`, `ICurrentUser`, `IJwtTokenGenerator`, `IPasswordHasher`, `IRefreshTokenService`).
- **Infrastructure** implements those ports (EF Core, BCrypt, JWT, Npgsql).
- **WebApi** composes everything via DI and exposes HTTP endpoints.

## Entry points

| Concern | File |
|---|---|
| App startup | [src/OrderMgmt.WebApi/Program.cs](../../backend/src/OrderMgmt.WebApi/Program.cs) |
| Test entry | [tests/OrderMgmt.IntegrationTests/](../../backend/tests/OrderMgmt.IntegrationTests/) |
| DB seed (dev) | [src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs](../../backend/src/OrderMgmt.Infrastructure/Persistence/Seed/DbSeeder.cs) — gated by `Database:AutoMigrateAndSeed` |

## Runtime artifacts (gitignored)

| Path | Notes |
|---|---|
| `backend/src/OrderMgmt.WebApi/logs/qldh-{date}.log` | Serilog rolling file, 14-day retention |
| `backend/**/bin/`, `backend/**/obj/` | Build outputs |

## Required configuration (production)

Provide via env vars / user-secrets:

| Key | Purpose |
|---|---|
| `ConnectionStrings__Default` | Npgsql connection string |
| `Jwt__Secret` | ≥ 32 chars symmetric key |
| `Jwt__Issuer`, `Jwt__Audience` | (have defaults but override per env) |
| `RefreshToken__ExpiresInDays` | optional, default 14 |
| `Seed__AdminPassword` | only needed first-run when seeding admin user |
| `Cors__Origins__0`, `Cors__Origins__1`, ... | allowed origins |
