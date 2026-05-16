using FluentValidation;
using OrderMgmt.Application.Identity.Admin.Models;

namespace OrderMgmt.Application.Identity.Admin.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username là bắt buộc.")
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9._-]+$")
                .WithMessage("Username chỉ được chứa chữ, số và các ký tự . _ -");

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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc.")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự.")
            .Matches(@"(?=.*[A-Za-z])(?=.*\d)")
                .WithMessage("Mật khẩu phải chứa cả chữ và số.");

        RuleFor(x => x.Status).IsInEnum();
    }
}
