namespace OrderMgmt.Infrastructure.Identity;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinSecretLength = 32;

    public string Issuer { get; set; } = "OrderMgmt";
    public string Audience { get; set; } = "OrderMgmtClient";
    public string Secret { get; set; } = default!;
    public int ExpiresInMinutes { get; set; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Audience is required.");
        if (string.IsNullOrWhiteSpace(Secret))
            throw new InvalidOperationException(
                "Jwt:Secret is required. Configure via environment variable Jwt__Secret, " +
                "user-secrets, or appsettings.{Environment}.json. Never commit production secrets.");
        if (Secret.Length < MinSecretLength)
            throw new InvalidOperationException(
                $"Jwt:Secret must be at least {MinSecretLength} characters (got {Secret.Length}).");
        if (ExpiresInMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiresInMinutes must be positive.");
    }
}
