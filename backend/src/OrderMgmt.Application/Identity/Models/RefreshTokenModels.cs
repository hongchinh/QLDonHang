namespace OrderMgmt.Application.Identity.Models;

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = default!;
}

public class TokenPairResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}
