using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OrderMgmt.Application.Common.Models;
using OrderMgmt.Application.Identity.Interfaces;
using OrderMgmt.Application.Identity.Models;
using OrderMgmt.Domain.Common;
using OrderMgmt.WebApi.Authorization;
using OrderMgmt.WebApi.Configuration;

namespace OrderMgmt.WebApi.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly AuthCookieOptions _cookieOptions;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        IOptions<AuthCookieOptions> cookieOptions)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _cookieOptions = cookieOptions.Value;
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
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
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

        Response.Cookies.Delete(_cookieOptions.Name, new CookieOptions { Path = _cookieOptions.Path });
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
        var fromCookie = Request.Cookies[_cookieOptions.Name];
        if (!string.IsNullOrWhiteSpace(fromCookie)) return fromCookie;
        return body?.RefreshToken;
    }

    private void SetRefreshCookie(string token, DateTimeOffset expires)
    {
        var sameSite = _cookieOptions.GetSameSiteMode();
        Response.Cookies.Append(_cookieOptions.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieOptions.Secure ?? (Request.IsHttps || sameSite == SameSiteMode.None),
            SameSite = sameSite,
            Path = _cookieOptions.Path,
            Expires = expires,
        });
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
}
