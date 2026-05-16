using FluentValidation;
using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc.")
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc.")
            .MaximumLength(200);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.RoleCode)
            .NotEmpty().WithMessage("Vai trò là bắt buộc.")
            .MaximumLength(50);

        RuleFor(x => x.Status).IsInEnum();
    }
}
