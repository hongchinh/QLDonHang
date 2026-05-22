using FluentValidation;
using OrderMgmt.Application.Identity.UserSettings.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Identity.UserSettings.Validators;

public class UpdateLockAtRequestValidator : AbstractValidator<UpdateLockAtRequest>
{
    private static readonly HashSet<QuotationStatus> AllowedLockAt = new()
    {
        QuotationStatus.Sent,
        QuotationStatus.Confirmed,
        QuotationStatus.AccountingConfirmed,
    };

    public UpdateLockAtRequestValidator()
    {
        RuleFor(x => x.LockAtStatus)
            .Must(s => !s.HasValue || AllowedLockAt.Contains(s.Value))
            .WithMessage("LockAtStatus chỉ chấp nhận: null | Sent | Confirmed | AccountingConfirmed.");
    }
}
