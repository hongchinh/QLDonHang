namespace OrderMgmt.Application.Branding.Models;

public sealed class BrandingDto
{
    public bool HasLogoFull { get; set; }
    public bool HasLogoMark { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
