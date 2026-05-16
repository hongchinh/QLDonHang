using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Interfaces;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(AdminUserListQuery query, CancellationToken ct = default);
    Task<AdminUserDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<AdminUserDetailDto> CreateAsync(CreateUserRequest req, CancellationToken ct = default);
    Task<AdminUserDetailDto> UpdateAsync(Guid id, UpdateUserRequest req, CancellationToken ct = default);
    Task ResetPasswordAsync(Guid id, ResetPasswordRequest req, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, SetUserStatusRequest req, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
