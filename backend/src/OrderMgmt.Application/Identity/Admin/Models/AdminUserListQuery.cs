namespace OrderMgmt.Application.Identity.Admin.Models;

public class AdminUserListQuery
{
    public string? Search { get; set; }
    public bool ActiveOnly { get; set; } = false;
}
