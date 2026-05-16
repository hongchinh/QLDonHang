using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.Admin.Interfaces;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.Admin.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IAppDbContext _db;

    public AdminUserService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(AdminUserListQuery query, CancellationToken ct = default)
    {
        var queryable = _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (query.ActiveOnly)
            queryable = queryable.Where(u => !u.IsDeleted && u.Status == UserStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            queryable = queryable.Where(u =>
                EF.Functions.ILike(u.Username, pattern)
                || EF.Functions.ILike(EF.Functions.Unaccent(u.FullName), EF.Functions.Unaccent(pattern)));
        }

        return await queryable
            .OrderBy(u => u.Username)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                RoleCode = u.UserRoles
                    .OrderBy(ur => ur.Role.Code)
                    .Select(ur => ur.Role.Code)
                    .FirstOrDefault(),
                IsActive = !u.IsDeleted && u.Status == UserStatus.Active,
                LastLoginAt = u.LastLoginAt,
            })
            .ToListAsync(ct);
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
