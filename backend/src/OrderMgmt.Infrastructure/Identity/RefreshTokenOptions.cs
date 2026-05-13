namespace OrderMgmt.Infrastructure.Identity;

public class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int ExpiresInDays { get; set; } = 14;
    public int TokenByteLength { get; set; } = 64;
}
