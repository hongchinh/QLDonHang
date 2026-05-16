namespace OrderMgmt.Application.Identity.Admin.Models;

public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = default!;
}
