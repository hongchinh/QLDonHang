using FluentValidation;
using OrderMgmt.Application.Common.Models;

namespace OrderMgmt.Application.Common.Validators;

public class PageRequestValidator<T> : AbstractValidator<T> where T : PageRequest
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page phải lớn hơn hoặc bằng 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PageRequest.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {PageRequest.MaxPageSize}.");

        RuleFor(x => x.SortDirection)
            .Must(d => d is null || d.Equals("asc", StringComparison.OrdinalIgnoreCase) || d.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection chỉ chấp nhận 'asc' hoặc 'desc'.");
    }
}
