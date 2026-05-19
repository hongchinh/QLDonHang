using FluentValidation;
using OrderMgmt.Application.Reports.VehicleRevenue.Models;

namespace OrderMgmt.Application.Reports.VehicleRevenue.Validators;

public class VehicleRevenueReportRequestValidator : AbstractValidator<VehicleRevenueReportRequest>
{
    public VehicleRevenueReportRequestValidator()
    {
        RuleFor(x => x.From).NotNull().WithMessage("From là bắt buộc.");
        RuleFor(x => x.To).NotNull().WithMessage("To là bắt buộc.");
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("From phải <= To.");
        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.To.Value.DayNumber - x.From.Value.DayNumber <= 366)
            .WithMessage("Khoảng thời gian tối đa 366 ngày.");
        RuleFor(x => x.Months).InclusiveBetween(1, 24);
        RuleFor(x => x.TopVehicles).InclusiveBetween(1, 10);
    }
}
