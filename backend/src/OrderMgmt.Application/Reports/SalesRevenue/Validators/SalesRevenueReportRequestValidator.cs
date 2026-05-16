using FluentValidation;
using OrderMgmt.Application.Reports.SalesRevenue.Models;

namespace OrderMgmt.Application.Reports.SalesRevenue.Validators;

public class SalesRevenueReportRequestValidator : AbstractValidator<SalesRevenueReportRequest>
{
    public SalesRevenueReportRequestValidator()
    {
        RuleFor(x => x.From).LessThanOrEqualTo(x => x.To)
            .WithMessage("From phải <= To.");
        RuleFor(x => x).Must(x => (x.To - x.From).TotalDays <= 366)
            .WithMessage("Khoảng thời gian tối đa 366 ngày.");
    }
}
