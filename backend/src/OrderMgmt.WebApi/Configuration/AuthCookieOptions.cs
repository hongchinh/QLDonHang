using Microsoft.AspNetCore.Http;

namespace OrderMgmt.WebApi.Configuration;

public class AuthCookieOptions
{
    public const string SectionName = "AuthCookie";

    public string Name { get; set; } = "qldh.refresh";
    public string Path { get; set; } = "/api/auth";
    public string SameSite { get; set; } = "Lax";
    public bool? Secure { get; set; }

    public SameSiteMode GetSameSiteMode()
        => SameSite.Trim().ToLowerInvariant() switch
        {
            "strict" => SameSiteMode.Strict,
            "none" => SameSiteMode.None,
            "unspecified" => SameSiteMode.Unspecified,
            _ => SameSiteMode.Lax,
        };
}
