namespace OrderMgmt.Application.Identity.Admin.Models;

public class PermissionDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string? Description { get; set; }
}
