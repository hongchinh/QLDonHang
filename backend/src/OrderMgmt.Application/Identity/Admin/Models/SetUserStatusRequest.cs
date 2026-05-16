using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.Admin.Models;

public class SetUserStatusRequest
{
    public UserStatus Status { get; set; }
}
