namespace OrderMgmt.Application.Identity.Models;

public class RefreshTokenRequest
{
    // Nullable so browser clients can POST an empty body and rely on the
    // HttpOnly cookie. [ApiController] would otherwise mark a non-nullable
    // reference type as implicitly [Required] and reject `{}` with 400.
    public string? RefreshToken { get; set; }
}

public class TokenPairResponse
{
    public string AccessToken { get; set; } = default!;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = default!;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}
