# Phần mềm Quản lý Đơn hàng - Báo giá - Bàn giao - Báo cáo

Hệ thống quản lý quy trình bán hàng theo luồng:

> Báo giá → Xác nhận → Lập đơn hàng → In chứng từ → Bàn giao / Phiếu xuất kho → Thanh toán → Công nợ → Báo cáo

> **Scope hiện tại**: Foundation only — toàn bộ khung kiến trúc, auth/RBAC, danh mục khách hàng làm reference. Các module nghiệp vụ còn lại (Hàng hóa, Báo giá, Đơn hàng, Bàn giao, Thanh toán, Báo cáo) đã được khai báo permission/route nhưng cần phát triển tiếp theo pattern reference.

---

## Tech stack

**Backend**

- .NET 9 Web API, Clean Architecture (Domain / Application / Infrastructure / WebApi)
- PostgreSQL 16 + EF Core 9 (Npgsql)
- JWT Bearer + Role-based + Permission-based authorization (policy `perm:<permission_code>`)
- FluentValidation, Mapster, Serilog, Swagger
- BCrypt password hashing
- Global exception handler → standard API response

**Frontend**

- Vite + React 18 + TypeScript
- TailwindCSS + shadcn/ui style components
- React Router v6, TanStack Query, TanStack Table
- Axios, React Hook Form + Zod, Zustand (persisted auth store)
- Lucide React, Recharts, date-fns

**Database**

- PostgreSQL local qua docker-compose (`postgres:16-alpine`)
- pgAdmin tại `http://localhost:5050`

---

## Cấu trúc thư mục

```
QLDonHang/
├── backend/
│   ├── OrderMgmt.sln
│   ├── Directory.Build.props
│   └── src/
│       ├── OrderMgmt.Domain/        # Entities, Enums, Constants, DomainException
│       ├── OrderMgmt.Application/   # DTOs, Interfaces, Services, Validators, Mapping
│       ├── OrderMgmt.Infrastructure/# DbContext, Migrations, JWT, BCrypt, Seed
│       └── OrderMgmt.WebApi/        # Controllers, Middleware, Authorization, Program.cs
├── frontend/
│   ├── package.json
│   ├── vite.config.ts
│   └── src/
│       ├── components/    # ui/ (shadcn-style), layout/
│       ├── features/      # auth/, customers/  ← business hooks + API
│       ├── pages/         # route components
│       ├── routes/        # ProtectedRoute
│       ├── stores/        # zustand stores
│       ├── lib/           # api-client, query-client, utils
│       ├── App.tsx
│       └── main.tsx
├── docs/                  # BD / PDR / architecture
├── docker-compose.yml
└── README.md
```

---

## Chạy local

### 1) Khởi động PostgreSQL

```powershell
docker compose up -d postgres
```

(Tùy chọn) Mở pgAdmin tại http://localhost:5050 (admin@qldh.local / admin).

### 2) Backend

```powershell
cd backend
dotnet restore
dotnet build
dotnet run --project src/OrderMgmt.WebApi
```

- API: http://localhost:5050
- Swagger: http://localhost:5050/swagger
- Health: http://localhost:5050/health

Khi khởi động, nếu `Database:AutoMigrateAndSeed = true` (mặc định), hệ thống sẽ tự động:

- Chạy EF migrations.
- Seed permissions, 5 roles (`ADMIN`, `SALES`, `ACCOUNTANT`, `WAREHOUSE`, `MANAGER`).
- Seed user `admin` / `Admin@123`.
- Seed danh mục nhóm hàng và đơn vị tính mặc định.

### 3) Frontend

```powershell
cd frontend
npm install
npm run dev
```

- App: http://localhost:5173
- Dev proxy `/api` → http://localhost:5050 đã cấu hình sẵn.

Đăng nhập: `admin` / `Admin@123`.

---

## EF Core migrations

Khi thêm/đổi entity trong `OrderMgmt.Domain`:

```powershell
cd backend
dotnet ef migrations add <Name> --project src/OrderMgmt.Infrastructure --startup-project src/OrderMgmt.WebApi -o Persistence/Migrations
dotnet ef database update --project src/OrderMgmt.Infrastructure --startup-project src/OrderMgmt.WebApi
```

Cần cài CLI lần đầu: `dotnet tool install --global dotnet-ef`.

---

## Phân quyền

- Roles được seed sẵn — đã ánh xạ phù hợp với 5 vai trò trong tài liệu BD (Admin / Sales / Accountant / Warehouse / Manager).
- Permission codes theo chuẩn `<module>.<action>` (xem `OrderMgmt.Domain/Constants/Permissions.cs`).
- Bảo vệ endpoint bằng attribute:

```csharp
[HasPermission(Permissions.Customers.Update)]
public async Task<...> Update(...)
```

- Frontend kiểm tra qua `useAuthStore.hasPermission('customers.update')` hoặc `<ProtectedRoute permission="customers.update">`.

---

## Standard API response

```json
{
  "success": true,
  "data": { /* ... */ },
  "error": null,
  "timestamp": "2026-05-12T10:30:00+00:00"
}
```

Khi lỗi:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION",
    "message": "Dữ liệu không hợp lệ.",
    "details": { "Name": ["Tên khách hàng là bắt buộc"] }
  }
}
```

`GlobalExceptionMiddleware` map các exception (`DomainException`, `NotFoundException`, `ConflictException`, `ForbiddenException`, FluentValidation `ValidationException`, …) sang HTTP status tương ứng.

---

## Thêm một module mới (pattern reference: Customer)

**Backend** (mỗi bước là 1 file mới):

1. `Domain/Entities/<Module>/Foo.cs` — entity kế thừa `BaseEntity`.
2. `Infrastructure/Persistence/Configurations/FooConfiguration.cs` — EF mapping + indexes + query filter.
3. `Application/Common/Interfaces/IAppDbContext.cs` — thêm `DbSet<Foo>`.
4. `Application/<Module>/Foos/Models/FooDto.cs` — DTO + request types.
5. `Application/<Module>/Foos/Validators/FooValidators.cs` — FluentValidation.
6. `Application/<Module>/Foos/Interfaces/IFooService.cs` + `Services/FooService.cs`.
7. `Application/DependencyInjection.cs` — đăng ký service.
8. `Domain/Constants/Permissions.cs` — thêm permission codes.
9. `Infrastructure/Persistence/Seed/DbSeeder.cs` — seed permissions vào roles phù hợp.
10. `WebApi/Controllers/FoosController.cs` — endpoints + `[HasPermission(...)]`.
11. Migration: `dotnet ef migrations add Add<Module>`.

**Frontend**:

1. `features/<module>/types.ts` — TS types khớp DTO.
2. `features/<module>/api.ts` — gọi API.
3. `features/<module>/hooks.ts` — TanStack Query hooks.
4. `features/<module>/schema.ts` — Zod schema.
5. `pages/<module>/<module>-list-page.tsx` + `<module>-form-page.tsx`.
6. `App.tsx` — thêm `<Route>` bọc trong `<ProtectedRoute permission="...">`.
7. `components/layout/app-layout.tsx` — thêm nav item.

---

## Tài liệu nghiệp vụ

- [docs/SUMMARY.md](docs/SUMMARY.md) — index tài liệu.
- [docs/bd/phan-tich-yeu-cau-phan-mem-quan-ly-don-hang.md](docs/bd/phan-tich-yeu-cau-phan-mem-quan-ly-don-hang.md) — toàn bộ phân tích yêu cầu (23 chương).
- [docs/architecture/](docs/architecture/) — sẽ bổ sung sơ đồ kiến trúc hệ thống.

---

## Lộ trình tiếp theo (theo BD §21 MVP)

- **Giai đoạn 1**: Hàng hóa → Báo giá → In báo giá PDF → Đơn hàng → In đơn hàng → Biên bản bàn giao → Báo cáo doanh thu/lợi nhuận cơ bản.
- **Giai đoạn 2**: Trạng thái giao hàng, ghi nhận thanh toán, công nợ, QR thanh toán, lịch sử chỉnh sửa, cấu hình mẫu in.
- **Giai đoạn 3**: Dashboard tổng quan, báo cáo nâng cao theo khách hàng/sản phẩm/nhân viên, import Excel.
