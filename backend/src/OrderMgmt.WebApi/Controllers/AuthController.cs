using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.WebApi.Authorization;

namespace OrderMgmt.WebApi.Controllers;

public class AuthController : ApiControllerBase
{
    // The refresh token is bound to a path-scoped, HttpOnly cookie so it cannot
    // be read by frontend JavaScript (XSS-resistant). The body field is kept
    // populated for legacy/CLI clients but the browser frontend ignores it.
    private const string RefreshCookieName = "qldh.refresh";
    private const string RefreshCookiePath = "/api/auth";

    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(IAuthService authService, IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        await _loginValidator.ValidateAndThrowAsync(request, ct);
        var result = await _authService.LoginAsync(request, GetIp(), GetUserAgent(), ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Success(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<ActionResult<ApiResponse<TokenPairResponse>>> Refresh(
        [FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        var token = ReadRefreshToken(request);
        if (string.IsNullOrWhiteSpace(token))
            throw new AuthenticationException("Thiếu refresh token.");

        var result = await _authService.RefreshAsync(
            new RefreshTokenRequest { RefreshToken = token }, GetIp(), GetUserAgent(), ct);
        SetRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
        return Success(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout([FromBody] RefreshTokenRequest? request, CancellationToken ct)
    {
        var token = ReadRefreshToken(request);
        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                await _authService.LogoutAsync(new RefreshTokenRequest { RefreshToken = token }, ct);
            }
            catch (AuthenticationException)
            {
                // Best-effort: token may already be revoked or expired. Clearing the
                // cookie below is what actually logs this browser out.
            }
        }

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });
        return Success();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<CurrentUserDto>> Me()
    {
        var dto = _authService.GetCurrent();
        return Success(dto);
    }

    private string? ReadRefreshToken(RefreshTokenRequest? body)
    {
        // Prefer the HttpOnly cookie — that's the authoritative store for browser
        // sessions. Fall back to the body so non-browser clients still work.
        var fromCookie = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrWhiteSpace(fromCookie)) return fromCookie;
        return body?.RefreshToken;
    }

    private void SetRefreshCookie(string token, DateTimeOffset expires)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = RefreshCookiePath,
            Expires = expires,
        });
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
}
