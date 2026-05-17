using FluentValidation;
using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Validators;

public class UpdateRolePermissionsRequestValidator : AbstractValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionCodes)
            .NotNull().WithMessage("Danh sách quyền không được null.")
            .Must(codes => codes.Distinct(StringComparer.Ordinal).Count() == codes.Count)
                .WithMessage("Danh sách quyền có giá trị trùng.");
    }
}
