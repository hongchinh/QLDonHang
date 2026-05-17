namespace OrderMgmt.Application.Identity.Admin.Models;

public class UpdateRolePermissionsRequest
{
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
}
