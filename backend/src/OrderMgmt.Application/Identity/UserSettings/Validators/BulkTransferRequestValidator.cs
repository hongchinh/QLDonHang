using FluentValidation;
using OrderMgmt.Application.Identity.UserSettings.Models;

namespace OrderMgmt.Application.Identity.UserSettings.Validators;

public class BulkTransferRequestValidator : AbstractValidator<BulkTransferRequest>
{
    public BulkTransferRequestValidator()
    {
        RuleFor(x => x.ToUserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
