using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.Admin.Interfaces;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Constants;
using OrderMgmt.Domain.Entities.Identity;

namespace OrderMgmt.Application.Identity.Admin.Services;

public class AdminRoleService : IAdminRoleService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdminRoleService> _logger;

    public AdminRoleService(IAppDbContext db, ICurrentUser currentUser, ILogger<AdminRoleService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default)
    {
        return await _db.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .Select(p => new PermissionDto
            {
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Description = p.Description,
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RoleListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Roles
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Code)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count(ur => !ur.User.IsDeleted),
            })
            .ToListAsync(ct);
    }

    public async Task<RoleDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == id && !ur.User.IsDeleted, ct);

        return MapToDetail(role, userCount);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest req, CancellationToken ct = default)
    {
        var code = req.Code.Trim();
        var name = req.Name.Trim();

        if (await _db.Roles.AnyAsync(r => r.Code.ToLower() == code.ToLower(), ct))
            throw new ConflictException($"Mã vai trò '{code}' đã tồn tại.");

        if (await NameExistsAsync(name, excludedId: null, ct))
            throw new ConflictException($"Tên vai trò '{name}' đã tồn tại.");

        var permissions = await LoadAndValidatePermissionsAsync(req.PermissionCodes, ct);

        var role = new Role
        {
            Code = code,
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            IsSystem = false,
        };
        foreach (var perm in permissions)
        {
            role.RolePermissions.Add(new RolePermission { Role = role, Permission = perm });
        }
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Role {RoleCode} created by user {UserId} with {PermissionCount} permissions.",
            role.Code, _currentUser.UserId, permissions.Count);

        return await GetAsync(role.Id, ct);
    }

    public async Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest req, CancellationToken ct = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        if (role.IsSystem)
            throw new ForbiddenException("Không thể đổi tên role hệ thống.");

        var newName = req.Name.Trim();
        if (!string.Equals(role.Name, newName, StringComparison.Ordinal)
            && await NameExistsAsync(newName, excludedId: id, ct))
            throw new ConflictException($"Tên vai trò '{newName}' đã tồn tại.");

        role.Name = newName;
        role.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Role {RoleCode} renamed by user {UserId}.", role.Code, _currentUser.UserId);

        return await GetAsync(id, ct);
    }

    public async Task<RoleDetailDto> UpdatePermissionsAsync(
        Guid id, UpdateRolePermissionsRequest req, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        if (role.Code == RoleCodes.Admin)
            throw new ForbiddenException("Không thể chỉnh sửa permission của role ADMIN.");

        var permissions = await LoadAndValidatePermissionsAsync(req.PermissionCodes, ct);
        var newCodes = permissions.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var existingCodes = role.RolePermissions
            .Select(rp => rp.Permission.Code)
            .ToHashSet(StringComparer.Ordinal);

        var codesToAdd = newCodes.Except(existingCodes).ToList();
        var codesToRemove = existingCodes.Except(newCodes).ToList();

        if (codesToRemove.Count > 0)
        {
            var rowsToRemove = role.RolePermissions
                .Where(rp => codesToRemove.Contains(rp.Permission.Code))
                .ToList();
            foreach (var rp in rowsToRemove)
            {
                _db.RolePermissions.Remove(rp);
                role.RolePermissions.Remove(rp);
            }
        }

        if (codesToAdd.Count > 0)
        {
            var permsToAdd = permissions.Where(p => codesToAdd.Contains(p.Code));
            foreach (var perm in permsToAdd)
            {
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Role {RoleCode} permissions updated by user {UserId}. Added: [{Added}]. Removed: [{Removed}].",
            role.Code,
            _currentUser.UserId,
            string.Join(",", codesToAdd),
            string.Join(",", codesToRemove));

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException(nameof(Role), id);

        if (role.IsSystem)
            throw new ForbiddenException("Không thể xoá role hệ thống.");

        var userCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == id && !ur.User.IsDeleted, ct);
        if (userCount > 0)
            throw new ConflictException(
                $"Role này đang được gán cho {userCount} user. Vui lòng đổi role các user trước khi xoá.");

        // RolePermission is a pure join entity (no BaseEntity / ISoftDeletable), so the cascade
        // soft-delete in AppDbContext does NOT touch it. Hard-delete the rows here so the role's
        // permission grants don't dangle after the role is soft-deleted.
        if (role.RolePermissions.Count > 0)
            _db.RolePermissions.RemoveRange(role.RolePermissions);

        role.IsDeleted = true;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Role {RoleCode} deleted by user {UserId}.", role.Code, _currentUser.UserId);
    }

    private async Task<List<Permission>> LoadAndValidatePermissionsAsync(
        IReadOnlyList<string> requestedCodes, CancellationToken ct)
    {
        if (requestedCodes.Count == 0) return new List<Permission>();

        var distinctCodes = requestedCodes.Distinct(StringComparer.Ordinal).ToList();
        var permissions = await _db.Permissions
            .Where(p => distinctCodes.Contains(p.Code))
            .ToListAsync(ct);

        var found = permissions.Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var missing = distinctCodes.Where(c => !found.Contains(c)).ToList();
        if (missing.Count > 0)
            throw new ConflictException($"Permission '{missing[0]}' không tồn tại.");

        return permissions;
    }

    private Task<bool> NameExistsAsync(string name, Guid? excludedId, CancellationToken ct)
    {
        return _db.Roles.AnyAsync(r =>
            r.Id != (excludedId ?? Guid.Empty)
            && EF.Functions.ILike(
                EF.Functions.Unaccent(r.Name),
                EF.Functions.Unaccent(name)),
            ct);
    }

    private static RoleDetailDto MapToDetail(Role role, int userCount) => new()
    {
        Id = role.Id,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        IsSystem = role.IsSystem,
        PermissionCodes = role.RolePermissions
            .Select(rp => rp.Permission.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList(),
        UserCount = userCount,
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt,
    };
}
