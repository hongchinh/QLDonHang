using FluentValidation;
using OrderMgmt.Application.Common.Validators;
using OrderMgmt.Application.Sales.Quotations.Helpers;
using OrderMgmt.Application.Sales.Quotations.Models;
using OrderMgmt.Domain.Enums;

namespace OrderMgmt.Application.Sales.Quotations.Validators;

public class UpsertQuotationLineRequestValidator : AbstractValidator<UpsertQuotationLineRequest>
{
    public UpsertQuotationLineRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProductCode).MaximumLength(50);
        RuleFor(x => x.Specification).MaximumLength(500);
        RuleFor(x => x.UnitName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PricingMode).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
        RuleFor(x => x.Length).GreaterThanOrEqualTo(0).When(x => x.Length.HasValue);
        RuleFor(x => x.Width).GreaterThanOrEqualTo(0).When(x => x.Width.HasValue);
        RuleFor(x => x.Thickness).GreaterThanOrEqualTo(0).When(x => x.Thickness.HasValue);
        RuleFor(x => x.Density).GreaterThanOrEqualTo(0).When(x => x.Density.HasValue);
        RuleFor(x => x.SheetCount).GreaterThanOrEqualTo(0).When(x => x.SheetCount.HasValue);
        RuleFor(x => x.Note).MaximumLength(1000);

        When(x => x.PricingMode == PricingMode.PerLinearMeter, () =>
        {
            RuleFor(x => x.Length).NotNull().GreaterThan(0);
            RuleFor(x => x.SheetCount).NotNull().GreaterThan(0);
        });

        When(x => x.PricingMode == PricingMode.PerSquareMeter, () =>
        {
            RuleFor(x => x.Length).NotNull().GreaterThan(0);
            RuleFor(x => x.Width).NotNull().GreaterThan(0);
            RuleFor(x => x.SheetCount).NotNull().GreaterThan(0);
        });

        When(x => x.PricingMode == PricingMode.PerCubicMeter, () =>
        {
            RuleFor(x => x.Length).NotNull().GreaterThan(0);
            RuleFor(x => x.Width).NotNull().GreaterThan(0);
            RuleFor(x => x.Thickness).NotNull().GreaterThan(0);
            RuleFor(x => x.SheetCount).NotNull().GreaterThan(0);
        });
    }
}

public class UpsertQuotationRequestValidator : AbstractValidator<UpsertQuotationRequest>
{
    public UpsertQuotationRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerName)
            .MaximumLength(255).WithMessage("Tên khách hàng tối đa 255 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerName));
        RuleFor(x => x.QuotationDate).NotEqual(default(DateOnly));
        RuleFor(x => x.TaxRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Freight).GreaterThanOrEqualTo(0);
        RuleFor(x => x.InternalNote).MaximumLength(2000);
        RuleFor(x => x.DeliveryAddress).MaximumLength(1000);
        RuleFor(x => x.DeliveryRecipient).MaximumLength(255);
        RuleFor(x => x.DeliveryPhone).MaximumLength(30);
        RuleFor(x => x.DeliveryNote).MaximumLength(1000);

        RuleFor(x => x.Lines)
            .Must(l => l != null && l.Count > 0)
            .WithMessage("Báo giá phải có ít nhất 1 dòng.");

        RuleForEach(x => x.Lines).SetValidator(new UpsertQuotationLineRequestValidator());
    }
}

public class QuotationListRequestValidator : PageRequestValidator<QuotationListRequest>
{
    public QuotationListRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(QuotationStatusListParser.IsValid)
            .WithMessage("Trạng thái không hợp lệ. Giá trị cho phép: Draft, Sent, Confirmed, Cancelled.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.OwnerUserIds)
            .Must(OwnerIdListParser.IsValid)
            .WithMessage("Danh sách chủ sở hữu chứa giá trị không phải Guid hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.OwnerUserIds));
    }
}

public class TransitionQuotationRequestValidator : AbstractValidator<TransitionQuotationRequest>
{
    public TransitionQuotationRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
    }
}

public class TransferOwnerRequestValidator : AbstractValidator<TransferOwnerRequest>
{
    public TransferOwnerRequestValidator()
    {
        RuleFor(x => x.NewOwnerUserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
