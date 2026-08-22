using FluentValidation;

namespace SmartTaskManagement.Application.Features.Dashboard.Validators;

public sealed class DashboardSummaryQueryValidator : AbstractValidator<DashboardSummaryQuery>
{
    public DashboardSummaryQueryValidator()
    {
        RuleFor(query => query.UpcomingDays)
            .InclusiveBetween(1, 90)
            .WithMessage("UpcomingDays must be between 1 and 90.");
    }
}
