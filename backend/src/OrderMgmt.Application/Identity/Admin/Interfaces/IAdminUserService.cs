using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
}
