using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Domain.Entities.Identity;

namespace OrderMgmt.Application.Identity.Interfaces;

public interface IRefreshTokenService
{
    Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        User user, string? fromIp, string? userAgent, CancellationToken ct = default);

    Task<TokenPairResponse> RotateAsync(
        string rawToken, string? fromIp, string? userAgent, CancellationToken ct = default);

    Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default);
}
