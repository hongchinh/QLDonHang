using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.Domain.Entities.Identity;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Infrastructure.Identity;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IDateTime _clock;
    private readonly RefreshTokenOptions _options;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IAppDbContext db,
        IJwtTokenGenerator jwt,
        IDateTime clock,
        IOptions<RefreshTokenOptions> options,
        ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(string RawToken, DateTimeOffset ExpiresAt)> IssueAsync(
        User user, string? fromIp, string? userAgent, CancellationToken ct = default)
    {
        var raw = GenerateRawToken();
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(raw),
            ExpiresAt = _clock.UtcNow.AddDays(_options.ExpiresInDays),
            CreatedFromIp = fromIp,
            UserAgent = Truncate(userAgent, 500),
        };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);
        return (raw, entity.ExpiresAt);
    }

    public async Task<TokenPairResponse> RotateAsync(
        string rawToken, string? fromIp, string? userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new AuthenticationException("Refresh token không hợp lệ.");

        var hash = Hash(rawToken);
        var now = _clock.UtcNow;

        var token = await _db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
            throw new AuthenticationException("Refresh token không hợp lệ.");

        // Reuse-detection: a revoked token is presented again → an attacker likely stole it.
        // Revoke the entire active family for this user so the legitimate session also has to re-login.
        if (token.RevokedAt is not null)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId} from {Ip}. Revoking active family.",
                token.UserId, fromIp);
            await RevokeFamilyAsync(token.UserId, "REUSE_DETECTED", now, ct);
            await _db.SaveChangesAsync(ct);
            throw new AuthenticationException("Phiên đăng nhập đã bị thu hồi. Vui lòng đăng nhập lại.");
        }

        if (token.ExpiresAt <= now)
            throw new AuthenticationException("Refresh token đã hết hạn.");

        if (token.User.Status != UserStatus.Active || token.User.IsDeleted)
            throw new AuthenticationException("Tài khoản không khả dụng.");

        var newRaw = GenerateRawToken();
        var newEntity = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = Hash(newRaw),
            ExpiresAt = now.AddDays(_options.ExpiresInDays),
            CreatedFromIp = fromIp,
            UserAgent = Truncate(userAgent, 500),
        };
        _db.RefreshTokens.Add(newEntity);

        token.RevokedAt = now;
        token.RevokedReason = "ROTATED";
        token.ReplacedByTokenHash = newEntity.TokenHash;

        var roles = token.User.UserRoles.Select(ur => ur.Role.Code).Distinct().ToList();
        var permissions = token.User.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Code))
            .Distinct().ToList();

        var access = _jwt.Generate(token.User, roles, permissions);

        await _db.SaveChangesAsync(ct);

        return new TokenPairResponse
        {
            AccessToken = access.AccessToken,
            AccessTokenExpiresAt = access.ExpiresAt,
            RefreshToken = newRaw,
            RefreshTokenExpiresAt = newEntity.ExpiresAt,
        };
    }

    public async Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;

        var hash = Hash(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.RevokedAt is not null) return;

        token.RevokedAt = _clock.UtcNow;
        token.RevokedReason = reason;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> RevokeAllActiveForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        await RevokeFamilyAsync(userId, reason, _clock.UtcNow, ct);
        return await _db.SaveChangesAsync(ct);
    }

    private async Task RevokeFamilyAsync(Guid userId, string reason, DateTimeOffset now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active)
        {
            t.RevokedAt = now;
            t.RevokedReason = reason;
        }
    }

    private string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(_options.TokenByteLength);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Hash(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest);
    }

    private static string? Truncate(string? input, int max) =>
        input is null ? null : (input.Length <= max ? input : input[..max]);
}
