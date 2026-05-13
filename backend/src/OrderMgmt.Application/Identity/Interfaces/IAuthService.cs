using OrderMgmt.Application.Identity.Models;

namespace OrderMgmt.Application.Identity.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
    Task<TokenPairResponse> RefreshAsync(RefreshTokenRequest request, string? ip, string? userAgent, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
    CurrentUserDto GetCurrent();
}
