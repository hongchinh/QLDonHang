# Documentation Summary

**QLDonHang** — Phần mềm Quản lý Đơn hàng, Báo giá, Bàn giao, Báo cáo.
Backend: .NET 9 Clean Architecture + Npgsql/EF Core. Frontend: React + Vite + TanStack.

## Architecture

System design, project layering, auth flow, error handling.

| File | Description |
| ---- | ----------- |
| [architecture/system-architecture.md](architecture/system-architecture.md) | Clean Architecture layering, auth flow (JWT + refresh token), `ApiResponse` contract, exception → HTTP status map, soft-delete & audit semantics |

## Codebase

Directory layout and entry points for the QLDonHang repo.

| File | Description |
| ---- | ----------- |
| [codebase/directory-structure.md](codebase/directory-structure.md) | Backend `src/` (Domain → Application → Infrastructure → WebApi) + `tests/` integration test project, frontend tree, and runtime artifact paths |

## Code Standard

Conventions for backend + frontend.

| File | Description |
| ---- | ----------- |
| [code-standard/skill-authoring.md](code-standard/skill-authoring.md) | (Legacy fork-doc.) Backend conventions are encoded in [backend/.editorconfig](../backend/.editorconfig) and CLAUDE.md |

## Project PDR

Product goals, target users, scope.

| File | Description |
| ---- | ----------- |
| [project-pdr/product-goals.md](project-pdr/product-goals.md) | Purpose, target users, business scope, non-goals |
