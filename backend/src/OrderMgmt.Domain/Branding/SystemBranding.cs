namespace OrderMgmt.Domain.Branding;

public class SystemBranding
{
    public int Id { get; set; }
    public byte[]? LogoFull { get; set; }
    public string? LogoFullContentType { get; set; }
    public byte[]? LogoMark { get; set; }
    public string? LogoMarkContentType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}
