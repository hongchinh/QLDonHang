using Microsoft.EntityFrameworkCore;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IDateTime _clock;
    private readonly ICurrentUser _currentUser;

    public AuthService(
        IAppDbContext db,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        IRefreshTokenService refreshTokens,
        IDateTime clock,
        ICurrentUser currentUser)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted, ct);

        if (user is null || user.Status != UserStatus.Active)
            throw new AuthenticationException("Username hoặc mật khẩu không đúng.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new AuthenticationException("Username hoặc mật khẩu không đúng.");

        var roles = user.UserRoles.Select(ur => ur.Role.Code).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct().ToList();

        var access = _jwt.Generate(user, roles, permissions);
        var (refreshRaw, refreshExpiresAt) = await _refreshTokens.IssueAsync(user, ip, userAgent, ct);

        user.LastLoginAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new LoginResponse
        {
            AccessToken = access.AccessToken,
            ExpiresAt = access.ExpiresAt,
            RefreshToken = refreshRaw,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new CurrentUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
                Permissions = permissions,
            },
        };
    }

    public Task<TokenPairResponse> RefreshAsync(
        RefreshTokenRequest request, string? ip, string? userAgent, CancellationToken ct = default)
        => _refreshTokens.RotateAsync(request.RefreshToken!, ip, userAgent, ct);

    public Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
        => _refreshTokens.RevokeAsync(request.RefreshToken!, "LOGOUT", ct);

    public CurrentUserDto GetCurrent()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new AuthenticationException("Bạn cần đăng nhập.");

        return new CurrentUserDto
        {
            Id = _currentUser.UserId.Value,
            Username = _currentUser.Username ?? string.Empty,
            Email = _currentUser.Email ?? string.Empty,
            FullName = _currentUser.FullName ?? string.Empty,
            Roles = _currentUser.Roles.ToList(),
            Permissions = _currentUser.Permissions.ToList(),
        };
    }
}
