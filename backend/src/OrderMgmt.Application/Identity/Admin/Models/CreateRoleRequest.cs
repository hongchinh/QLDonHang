namespace OrderMgmt.Application.Identity.Admin.Models;

public class CreateRoleRequest
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
}
