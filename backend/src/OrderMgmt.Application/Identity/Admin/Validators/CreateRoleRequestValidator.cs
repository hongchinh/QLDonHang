using FluentValidation;
using OrderMgmt.Application.Identity.Admin.Models;
using OrderMgmt.Domain.Constants;

namespace OrderMgmt.Application.Identity.Admin.Validators;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    private static readonly HashSet<string> ReservedSystemCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleCodes.Admin,
        RoleCodes.Sales,
        RoleCodes.Accountant,
        RoleCodes.Warehouse,
        RoleCodes.Manager,
    };

    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã vai trò là bắt buộc.")
            .Matches("^[A-Z_][A-Z0-9_]{1,29}$")
                .WithMessage("Mã vai trò phải 2–30 ký tự, in hoa hoặc underscore, không bắt đầu bằng số.")
            .Must(code => !ReservedSystemCodes.Contains(code))
                .WithMessage("Mã vai trò trùng với role hệ thống, vui lòng đặt mã khác.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên vai trò là bắt buộc.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.PermissionCodes)
            .NotNull().WithMessage("Danh sách quyền không được null.")
            .Must(codes => codes.Distinct(StringComparer.Ordinal).Count() == codes.Count)
                .WithMessage("Danh sách quyền có giá trị trùng.");
    }
}
