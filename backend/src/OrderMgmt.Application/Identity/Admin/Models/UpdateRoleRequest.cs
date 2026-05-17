namespace OrderMgmt.Application.Identity.Admin.Models;

public class UpdateRoleRequest
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
