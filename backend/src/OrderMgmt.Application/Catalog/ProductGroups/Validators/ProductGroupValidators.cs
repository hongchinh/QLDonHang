using FluentValidation;
using OrderMgmt.Application.Catalog.ProductGroups.Models;
using OrderMgmt.Application.Common.Validators;

namespace OrderMgmt.Application.Catalog.ProductGroups.Validators;

public class CreateProductGroupRequestValidator : AbstractValidator<CreateProductGroupRequest>
{
    public CreateProductGroupRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductGroupRequestValidator : AbstractValidator<UpdateProductGroupRequest>
{
    public UpdateProductGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public class ProductGroupListRequestValidator : PageRequestValidator<ProductGroupListRequest>
{
}
