namespace OrderMgmt.Application.Identity.Admin.Models;

public class RoleListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int PermissionCount { get; set; }
    public int UserCount { get; set; }
}
