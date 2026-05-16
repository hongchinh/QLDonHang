using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Entities.Catalog;
using OrderMgmt.Domain.Entities.Identity;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    // Stable arbitrary key so concurrent application instances serialize migration + seed.
    private const long MigrationAdvisoryLockKey = 7426091732641_5L;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;

        // Open a single connection for the whole migrate + seed cycle so the
        // session-level advisory lock survives across operations.
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_lock({0})", new object[] { MigrationAdvisoryLockKey }, ct);
            try
            {
                await db.Database.MigrateAsync(ct);

                await SeedPermissionsAsync(db, ct);
                await SeedRolesAsync(db, ct);
                await SeedAdminUserAsync(db, hasher, seedOptions, logger, ct);
                await SeedReferenceDataAsync(db, ct);

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Database seeding completed.");
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_unlock({0})", new object[] { MigrationAdvisoryLockKey }, CancellationToken.None);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Code).ToListAsync(ct);
        var permissionDefs = new (string Code, string Module, string Name)[]
        {
            (Permissions.Users.View, Permissions.SystemModule, "Xem người dùng"),
            (Permissions.Users.Create, Permissions.SystemModule, "Tạo người dùng"),
            (Permissions.Users.Update, Permissions.SystemModule, "Cập nhật người dùng"),
            (Permissions.Users.Delete, Permissions.SystemModule, "Xóa người dùng"),
            (Permissions.Roles.View, Permissions.SystemModule, "Xem vai trò"),
            (Permissions.Roles.Manage, Permissions.SystemModule, "Quản lý vai trò"),

            (Permissions.Customers.View, Permissions.CatalogModule, "Xem khách hàng"),
            (Permissions.Customers.Create, Permissions.CatalogModule, "Tạo khách hàng"),
            (Permissions.Customers.Update, Permissions.CatalogModule, "Cập nhật khách hàng"),
            (Permissions.Customers.Delete, Permissions.CatalogModule, "Xóa khách hàng"),

            (Permissions.Products.View, Permissions.CatalogModule, "Xem hàng hóa"),
            (Permissions.Products.Create, Permissions.CatalogModule, "Tạo hàng hóa"),
            (Permissions.Products.Update, Permissions.CatalogModule, "Cập nhật hàng hóa"),
            (Permissions.Products.Delete, Permissions.CatalogModule, "Xóa hàng hóa"),

            (Permissions.Quotations.View, Permissions.SalesModule, "Xem báo giá"),
            (Permissions.Quotations.Create, Permissions.SalesModule, "Tạo báo giá"),
            (Permissions.Quotations.Update, Permissions.SalesModule, "Cập nhật báo giá"),
            (Permissions.Quotations.Delete, Permissions.SalesModule, "Xóa báo giá"),
            (Permissions.Quotations.Print, Permissions.SalesModule, "In báo giá"),
            (Permissions.Quotations.CancelConfirmed, Permissions.SalesModule, "Huỷ báo giá đã xác nhận"),
            (Permissions.Quotations.ViewCost, Permissions.SalesModule, "Xem giá vốn/lợi nhuận báo giá"),
            (Permissions.Quotations.ViewAll, Permissions.SalesModule, "Xem mọi báo giá (bypass owner)"),
            (Permissions.Quotations.TransferOwn, Permissions.SalesModule, "Chuyển báo giá của mình cho user khác"),
            (Permissions.Quotations.TransferAny, Permissions.SalesModule, "Chuyển báo giá của bất kỳ user nào"),
            (Permissions.Quotations.CloneOrphan, Permissions.SalesModule, "Clone báo giá của user đã nghỉ"),
            (Permissions.Quotations.BypassLock, Permissions.SalesModule, "Bypass khoá trạng thái báo giá"),
            (Permissions.UserSettings.Manage, Permissions.SystemModule, "Cấu hình thiết lập của user khác"),

            (Permissions.Reports.Revenue, Permissions.ReportModule, "Báo cáo doanh thu"),
            (Permissions.Reports.Profit, Permissions.ReportModule, "Báo cáo lợi nhuận"),
            (Permissions.Reports.Debt, Permissions.ReportModule, "Báo cáo công nợ"),
            (Permissions.Reports.Delivery, Permissions.ReportModule, "Báo cáo giao hàng"),
        };

        foreach (var (code, module, name) in permissionDefs)
        {
            if (existing.Contains(code)) continue;
            db.Permissions.Add(new Permission { Code = code, Module = module, Name = name });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        var allPermissions = await db.Permissions.ToListAsync(ct);
        var existingRoles = await db.Roles.Include(r => r.RolePermissions).ToListAsync(ct);

        var roleDefs = new (string Code, string Name, string[] Permissions)[]
        {
            (RoleCodes.Admin, "Quản trị hệ thống", allPermissions.Select(p => p.Code).ToArray()),
            (RoleCodes.Sales, "Nhân viên kinh doanh", new[]
            {
                Permissions.Customers.View, Permissions.Customers.Create, Permissions.Customers.Update,
                Permissions.Products.View,
                Permissions.Quotations.View, Permissions.Quotations.Create, Permissions.Quotations.Update,
                Permissions.Quotations.Print,
                Permissions.Quotations.TransferOwn,
            }),
            (RoleCodes.Accountant, "Kế toán", new[]
            {
                Permissions.Customers.View, Permissions.Products.View,
                Permissions.Quotations.View,
                Permissions.Reports.Revenue, Permissions.Reports.Debt,
            }),
            (RoleCodes.Warehouse, "Kho / giao hàng", new[]
            {
                Permissions.Customers.View, Permissions.Products.View,
            }),
            (RoleCodes.Manager, "Quản lý", allPermissions.Select(p => p.Code).ToArray()),
        };

        foreach (var (code, name, permCodes) in roleDefs)
        {
            var role = existingRoles.FirstOrDefault(r => r.Code == code);
            if (role is null)
            {
                role = new Role { Code = code, Name = name, IsSystem = true };
                db.Roles.Add(role);
            }

            foreach (var pcode in permCodes)
            {
                var perm = allPermissions.FirstOrDefault(p => p.Code == pcode);
                if (perm is null) continue;
                if (role.RolePermissions.Any(rp => rp.PermissionId == perm.Id)) continue;
                role.RolePermissions.Add(new RolePermission { Role = role, Permission = perm });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAdminUserAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        SeedOptions seedOptions,
        ILogger logger,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Username == "admin", ct)) return;

        if (string.IsNullOrWhiteSpace(seedOptions.AdminPassword))
        {
            logger.LogWarning(
                "Skipping admin user seed: Seed:AdminPassword is not configured. " +
                "Provide it via environment variable Seed__AdminPassword to enable seeding.");
            return;
        }

        var adminRole = await db.Roles.FirstAsync(r => r.Code == RoleCodes.Admin, ct);

        var admin = new User
        {
            Username = "admin",
            Email = "admin@qldh.local",
            FullName = "Quản trị hệ thống",
            PasswordHash = hasher.Hash(seedOptions.AdminPassword),
            Status = UserStatus.Active,
            UserRoles = new List<UserRole> { new() { RoleId = adminRole.Id } },
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedReferenceDataAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.ProductGroups.AnyAsync(ct))
        {
            var groups = new[]
            {
                new ProductGroup { Code = "EPS", Name = "Tấm xốp EPS", SortOrder = 1 },
                new ProductGroup { Code = "XPS", Name = "Tấm xốp XPS", SortOrder = 2 },
                new ProductGroup { Code = "PE", Name = "Tấm xốp PE Foam", SortOrder = 3 },
                new ProductGroup { Code = "CSN", Name = "Cao su non", SortOrder = 4 },
                new ProductGroup { Code = "THUNG", Name = "Thùng xốp", SortOrder = 5 },
                new ProductGroup { Code = "DAGEL", Name = "Da gel", SortOrder = 6 },
                new ProductGroup { Code = "BK", Name = "Bông khoáng", SortOrder = 7 },
                new ProductGroup { Code = "BTT", Name = "Bông thủy tinh", SortOrder = 8 },
                new ProductGroup { Code = "VC", Name = "Vận chuyển", SortOrder = 9 },
                new ProductGroup { Code = "KHAC", Name = "Khác", SortOrder = 99 },
            };
            db.ProductGroups.AddRange(groups);
        }

        if (!await db.Units.AnyAsync(ct))
        {
            var units = new[]
            {
                new Unit { Code = "TAM", Name = "Tấm" },
                new Unit { Code = "M2", Name = "m²" },
                new Unit { Code = "M3", Name = "m³" },
                new Unit { Code = "THUNG", Name = "Thùng" },
                new Unit { Code = "TUI", Name = "Túi" },
                new Unit { Code = "KG", Name = "Kg" },
                new Unit { Code = "CHUYEN", Name = "Chuyến" },
                new Unit { Code = "BO", Name = "Bộ" },
                new Unit { Code = "CAI", Name = "Cái" },
            };
            db.Units.AddRange(units);
        }

        await db.SaveChangesAsync(ct);
    }
}
