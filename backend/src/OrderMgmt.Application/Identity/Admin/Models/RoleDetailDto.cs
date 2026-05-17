namespace OrderMgmt.Application.Identity.Admin.Models;

public class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
    public int UserCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
