using System.Security.Claims;
using OrderMgmt.Application.Common.Interfaces;
using OrderMgmt.Infrastructure.Identity;

namespace OrderMgmt.WebApi.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Username => Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => Principal?.FindFirst(ClaimTypes.Email)?.Value;

    public string? FullName => Principal?.FindFirst("full_name")?.Value;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll(JwtTokenGenerator.PermissionClaimType).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(JwtTokenGenerator.PermissionClaimType, permission) ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
