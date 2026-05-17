using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Interfaces;

public interface IAdminRoleService
{
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RoleListItemDto>> ListAsync(CancellationToken ct = default);
    Task<RoleDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<RoleDetailDto> CreateAsync(CreateRoleRequest req, CancellationToken ct = default);
    Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest req, CancellationToken ct = default);
    Task<RoleDetailDto> UpdatePermissionsAsync(Guid id, UpdateRolePermissionsRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
